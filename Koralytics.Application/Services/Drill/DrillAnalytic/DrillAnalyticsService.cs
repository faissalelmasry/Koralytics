using Koralytics.Application.DTOs.Drill;
using Koralytics.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Koralytics.Application.Services.Drill.DrillAnalytic
{
    public class DrillAnalyticsService : IDrillAnalyticsService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DrillAnalyticsService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<CategoryPerformanceDto>> GetSquadWeakCategoriesAsync(int teamId)
        {
            var rawResults = await _unitOfWork.Repository<Domain.Entities.Drill.DrillResult>()
                .GetQueryableAsNoTracking()
                .Where(r => r.Drill.DrillSession.TeamId == teamId)
                .Select(r => new
                {
                    CategoryName = r.Drill.DrillTemplate.DrillCategory.Name ?? "Uncategorized",
                    PlayerId = r.PlayerId,
                    PlayerName = r.Player.FirstName + " " + r.Player.LastName,
                    Score = r.FinalScore
                })
                .ToListAsync();

            var squadPerformance = rawResults
                .GroupBy(r => r.CategoryName)
                .Select(g => new CategoryPerformanceDto
                {
                    CategoryName = g.Key,
                    AverageScore = Math.Round(g.Average(r => r.Score), 2),
                    LowestPerformers = g.GroupBy(p => new { p.PlayerId, p.PlayerName })
                                        .Select(pg => new PlayerPerformanceInsightDto
                                        {
                                            Name = string.IsNullOrWhiteSpace(pg.Key.PlayerName) ? $"Player #{pg.Key.PlayerId}" : pg.Key.PlayerName.Trim(),
                                            Score = Math.Round(pg.Average(p => p.Score), 2)
                                        })
                                        .OrderBy(p => p.Score)
                                        .Take(3)
                                        .ToList()
                })
                .OrderBy(c => c.AverageScore)
                .ToList();

            return squadPerformance;
        }

        public async Task<CoachBiasReportDto> DetectCoachBiasAsync(int targetCoachId, int academyId, int currentUserId, string currentUserRole)
        {
            if (string.Equals(currentUserRole, "Coach", StringComparison.OrdinalIgnoreCase) && currentUserId != targetCoachId)
            {
                throw new UnauthorizedAccessException("Coaches can only view their own bias reports. Academy Admins can view any coach.");
            }

            var coachUser = await _unitOfWork.Repository<Domain.Entities.Coach.Coach>()
                .GetQueryableAsNoTracking()
                .Select(u => new { u.Id, u.FirstName, u.LastName, u.UserName })
                .FirstOrDefaultAsync(u => u.Id == targetCoachId);

            string coachName = coachUser != null
                ? $"{coachUser.FirstName} {coachUser.LastName}".Trim()
                : $"Coach #{targetCoachId}";

            if (string.IsNullOrWhiteSpace(coachName))
            {
                coachName = coachUser?.UserName ?? $"Coach #{targetCoachId}";
            }

            var cutoffDate = DateTime.UtcNow.AddDays(-30);

            // ====================================================================
            // 1. FETCH PRACTICE SCORES CONCURRENTLY (🟢 OPTIMIZATION)
            // ====================================================================

            var drillScoresTask = _unitOfWork.Repository<Domain.Entities.Drill.DrillResult>()
                .GetQueryableAsNoTracking()
                .Where(dr => (dr.CreatedById == targetCoachId || dr.Drill.DrillSession.CoachId == targetCoachId)
                          && (dr.Drill.Mode == Koralytics.Domain.Enums.DrillMode.Manual || dr.Drill.DrillTemplate.DrillMode == Koralytics.Domain.Enums.DrillMode.Manual)
                          && dr.CreatedAt >= cutoffDate)
                .GroupBy(dr => dr.PlayerId)
                .Select(g => new
                {
                    PlayerId = g.Key,
                    AvgDrillScore = g.Average(x => x.FinalScore),
                    DrillCount = g.Count()
                })
                .ToListAsync();

            var practiceMatchScoresTask = _unitOfWork.Repository<Domain.Entities.Match.MatchPlayerCategoryRating>()
                .GetQueryableAsNoTracking()
                .Where(cr => (cr.MatchPlayerRating.Match.Type == Koralytics.Domain.Enums.MatchType.Friendly
                           || cr.MatchPlayerRating.Match.Type == Koralytics.Domain.Enums.MatchType.Session)
                          && cr.MatchPlayerRating.CoachId == targetCoachId
                          && cr.MatchPlayerRating.CreatedAt >= cutoffDate)
                .GroupBy(cr => cr.MatchPlayerRating.PlayerId)
                .Select(g => new
                {
                    PlayerId = g.Key,
                    AvgPracticeMatchScore = g.Average(x => x.Rating),
                    RatingCount = g.Count()
                })
                .ToListAsync();

            // Fire both database round-trips simultaneously
            await Task.WhenAll(drillScoresTask, practiceMatchScoresTask);

            var drillScoresList = drillScoresTask.Result;
            var practiceMatchScoresList = practiceMatchScoresTask.Result;

            var allPracticePlayerIds = drillScoresList.Select(d => d.PlayerId)
                .Union(practiceMatchScoresList.Select(p => p.PlayerId))
                .Distinct()
                .ToList();

            var playerNamesList = await _unitOfWork.Repository<Domain.Entities.Player.Player>()
                .GetQueryableAsNoTracking()
                .Where(p => allPracticePlayerIds.Contains(p.Id))
                .Select(p => new { p.Id, p.FirstName, p.LastName })
                .ToListAsync();

            var drillScores = drillScoresList.ToDictionary(d => d.PlayerId);
            var practiceMatchScores = practiceMatchScoresList.ToDictionary(m => m.PlayerId);
            var playerNames = playerNamesList.ToDictionary(p => p.Id);

            var practiceScores = allPracticePlayerIds.Select(playerId =>
            {
                drillScores.TryGetValue(playerId, out var drill);
                practiceMatchScores.TryGetValue(playerId, out var pracMatch);

                decimal totalScore = 0;
                int totalCount = 0;

                if (drill != null)
                {
                    totalScore += drill.AvgDrillScore * drill.DrillCount;
                    totalCount += drill.DrillCount;
                }
                if (pracMatch != null)
                {
                    totalScore += pracMatch.AvgPracticeMatchScore * pracMatch.RatingCount;
                    totalCount += pracMatch.RatingCount;
                }

                decimal blendedAvg = totalCount > 0 ? totalScore / totalCount : 0;

                playerNames.TryGetValue(playerId, out var nameEntry);
                string name = nameEntry != null
                    ? $"{nameEntry.FirstName} {nameEntry.LastName}".Trim()
                    : $"Player #{playerId}";

                return new { PlayerId = playerId, PlayerName = name, AvgPracticeScore = blendedAvg };
            }).ToList();

            var playerIdsToAnalyze = practiceScores.Select(p => p.PlayerId).ToList();

            if (!playerIdsToAnalyze.Any())
            {
                return new CoachBiasReportDto
                {
                    CoachId = targetCoachId,
                    CoachName = coachName,
                    TrustPercentage = 100,
                    PlayersAnalyzedCount = 0,
                    Remarks = "Insufficient practice data (drills, friendlies, session matches) in the last 30 days."
                };
            }

            // ====================================================================
            // 2. FETCH TOURNAMENT MATCH SCORES 
            // ====================================================================
            var matchScoresList = await _unitOfWork.Repository<Domain.Entities.Match.MatchPlayerCategoryRating>()
                .GetQueryableAsNoTracking()
                .Where(cr => playerIdsToAnalyze.Contains(cr.MatchPlayerRating.PlayerId)
                          && cr.MatchPlayerRating.Match.Type == Koralytics.Domain.Enums.MatchType.Tournament
                          && cr.MatchPlayerRating.CreatedAt >= cutoffDate)
                .GroupBy(cr => cr.MatchPlayerRating.PlayerId)
                .Select(g => new
                {
                    PlayerId = g.Key,
                    AvgMatchScore = g.Average(x => x.Rating)
                })
                .ToListAsync();

            var matchScores = matchScoresList.ToDictionary(m => m.PlayerId);

            // ====================================================================
            // 3. THE TRUST INDEX CALCULATION
            // ====================================================================
            decimal totalDelta = 0;
            int validPlayerComparisons = 0;
            var playerComparisons = new List<PlayerBiasComparisonDto>();

            foreach (var practice in practiceScores)
            {
                if (matchScores.TryGetValue(practice.PlayerId, out var match))
                {
                    var practiceAvg = Math.Round(practice.AvgPracticeScore, 2);
                    var matchAvg = Math.Round(match.AvgMatchScore, 2);
                    var delta = Math.Round(Math.Abs(practiceAvg - matchAvg), 2);

                    totalDelta += delta;
                    validPlayerComparisons++;

                    string status = delta <= 1.0m
                        ? "Accurate"
                        : (practiceAvg > matchAvg ? "Over-rated" : "Under-rated");

                    playerComparisons.Add(new PlayerBiasComparisonDto
                    {
                        PlayerId = practice.PlayerId,
                        PlayerName = string.IsNullOrWhiteSpace(practice.PlayerName) ? $"Player #{practice.PlayerId}" : practice.PlayerName,
                        AvgPracticeScore = practiceAvg,
                        AvgMatchScore = matchAvg,
                        Delta = delta,
                        Status = status
                    });
                }
            }

            if (validPlayerComparisons == 0)
            {
                return new CoachBiasReportDto
                {
                    CoachId = targetCoachId,
                    CoachName = coachName,
                    TrustPercentage = 100,
                    PlayersAnalyzedCount = 0,
                    Remarks = "Players have practice data but no tournament matches in the last 30 days to compare against."
                };
            }

            decimal averageDelta = totalDelta / validPlayerComparisons;
            decimal rawTrustPercentage = 100 - (averageDelta * 10);
            decimal finalTrustPercentage = Math.Max(0, Math.Round(rawTrustPercentage, 2));

            // ====================================================================
            // 4. SAVE THE AUDIT TO THE DATABASE
            // ====================================================================
            await _unitOfWork.Repository<Domain.Entities.Coach.CoachAcademy>()
                .GetQueryable()
                .Where(ca => ca.CoachUserId == targetCoachId && (academyId == 0 || ca.AcademyId == academyId) && ca.LeftAt == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(c => c.BiasScore, finalTrustPercentage)
                    .SetProperty(c => c.BiasLastCalculatedAt, DateTime.UtcNow));

            return new CoachBiasReportDto
            {
                CoachId = targetCoachId,
                CoachName = coachName,
                TrustPercentage = finalTrustPercentage,
                PlayersAnalyzedCount = validPlayerComparisons,
                Remarks = "Trust Index calculated: practice scores (drills + friendlies + session matches) vs. tournament performance.",
                PlayerComparisons = playerComparisons
            };
        }
    }
}