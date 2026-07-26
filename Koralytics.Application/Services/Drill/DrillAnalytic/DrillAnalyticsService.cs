using Koralytics.Application.DTOs.Drill;
using Koralytics.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

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
            var squadPerformance = await _unitOfWork.Repository<Domain.Entities.Drill.DrillResult>()
                .GetQueryableAsNoTracking()
                .Where(r => r.Drill.DrillSession.TeamId == teamId)
                .GroupBy(r => r.Drill.DrillTemplate.DrillCategory.Name)
                .Select(g => new CategoryPerformanceDto
                {
                    CategoryName = g.Key ?? "Uncategorized",
                    AverageScore = Math.Round(g.Average(r => r.FinalScore), 2)
                })
                .OrderBy(c => c.AverageScore)
                .ToListAsync();

            return squadPerformance;
        }

        public async Task<CoachBiasReportDto> DetectCoachBiasAsync(int targetCoachId, int academyId, int currentUserId, string currentUserRole)
        {
            // 🛑 Security Check: Coaches can only view their own bias reports. Academy Admins can view any coach.
            if (string.Equals(currentUserRole, "Coach", StringComparison.OrdinalIgnoreCase) && currentUserId != targetCoachId)
            {
                throw new UnauthorizedAccessException("Coaches can only view their own bias reports. Academy Admins can view any coach.");
            }

            var coachUser = await _unitOfWork.Repository<Domain.Entities.Coach.Coach>()
                .GetQueryableAsNoTracking()
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
            // 1. FETCH PRACTICE SCORES (The Subjective Data)
            // ====================================================================
            var practiceScores = await _unitOfWork.Repository<Domain.Entities.Drill.DrillResult>()
                .GetQueryableAsNoTracking()
                .Where(dr => (dr.CreatedById == targetCoachId || dr.Drill.DrillSession.CoachId == targetCoachId)
                          && (dr.Drill.Mode == Koralytics.Domain.Enums.DrillMode.Manual || dr.Drill.DrillTemplate.DrillMode == Koralytics.Domain.Enums.DrillMode.Manual)
                          && dr.CreatedAt >= cutoffDate)
                .GroupBy(dr => new { dr.PlayerId, dr.Player.FirstName, dr.Player.LastName })
                .Select(g => new
                {
                    g.Key.PlayerId,
                    PlayerName = (g.Key.FirstName + " " + g.Key.LastName).Trim(),
                    AvgPracticeScore = g.Average(x => x.FinalScore)
                })
                .ToListAsync();

            var playerIdsToAnalyze = practiceScores.Select(p => p.PlayerId).ToList();

            if (!playerIdsToAnalyze.Any())
            {
                return new CoachBiasReportDto
                {
                    CoachId = targetCoachId,
                    CoachName = coachName,
                    TrustPercentage = 100,
                    PlayersAnalyzedCount = 0,
                    Remarks = "Insufficient practice data in the last 30 days."
                };
            }

            // ====================================================================
            // 2. FETCH MATCH SCORES (The Objective Reality)
            // ====================================================================
            var matchScores = await _unitOfWork.Repository<Domain.Entities.Match.MatchPlayerCategoryRating>()
                .GetQueryableAsNoTracking()
                .Where(cr => playerIdsToAnalyze.Contains(cr.MatchPlayerRating.PlayerId)
                          && cr.MatchPlayerRating.CreatedAt >= cutoffDate)
                .GroupBy(cr => cr.MatchPlayerRating.PlayerId)
                .Select(g => new
                {
                    PlayerId = g.Key,
                    AvgMatchScore = g.Average(x => x.Rating)
                })
                .ToListAsync();

            // ====================================================================
            // 3. THE TRUST INDEX CALCULATION
            // ====================================================================
            decimal totalDelta = 0;
            int validPlayerComparisons = 0;
            var playerComparisons = new List<PlayerBiasComparisonDto>();

            foreach (var practice in practiceScores)
            {
                var match = matchScores.FirstOrDefault(m => m.PlayerId == practice.PlayerId);

                if (match != null)
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
                    Remarks = "Players practiced but played no matches in the last 30 days to compare against."
                };
            }

            decimal averageDelta = totalDelta / validPlayerComparisons;
            decimal rawTrustPercentage = 100 - (averageDelta * 10);

            decimal finalTrustPercentage = Math.Max(0, Math.Round(rawTrustPercentage, 2));

            // ====================================================================
            // 4. SAVE THE AUDIT TO THE DATABASE
            // ====================================================================
            var coachAcademyRecord = await _unitOfWork.Repository<Domain.Entities.Coach.CoachAcademy>()
                .GetQueryable()
                .FirstOrDefaultAsync(ca => ca.CoachUserId == targetCoachId && (academyId == 0 || ca.AcademyId == academyId) && ca.LeftAt == null);

            if (coachAcademyRecord != null)
            {
                coachAcademyRecord.BiasScore = finalTrustPercentage;
                coachAcademyRecord.BiasLastCalculatedAt = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync();
            }

            return new CoachBiasReportDto
            {
                CoachId = targetCoachId,
                CoachName = coachName,
                TrustPercentage = finalTrustPercentage,
                PlayersAnalyzedCount = validPlayerComparisons,
                Remarks = "Trust Index calculated successfully.",
                PlayerComparisons = playerComparisons
            };
        }
    }
}