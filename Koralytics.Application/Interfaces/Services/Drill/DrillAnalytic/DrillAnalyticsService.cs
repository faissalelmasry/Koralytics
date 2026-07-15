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

        public async Task<CoachBiasReportDto> DetectCoachBiasAsync(int coachUserId, int academyId)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-30);

            var practiceScores = await _unitOfWork.Repository<Domain.Entities.Drill.DrillResult>()
                .GetQueryableAsNoTracking()
                .Where(dr => dr.CreatedById == coachUserId
                          && dr.Drill.Mode == Koralytics.Domain.Enums.DrillMode.Manual
                          && dr.CreatedAt >= cutoffDate)
                .GroupBy(dr => dr.PlayerId)
                .Select(g => new
                {
                    PlayerId = g.Key,
                    AvgPracticeScore = g.Average(x => x.FinalScore)
                })
                .ToListAsync();

            var playerIdsToAnalyze = practiceScores.Select(p => p.PlayerId).ToList();

            if (!playerIdsToAnalyze.Any())
            {
                return new CoachBiasReportDto
                {
                    CoachId = coachUserId,
                    TrustPercentage = 100,
                    PlayersAnalyzedCount = 0,
                    Remarks = "Insufficient practice data in the last 30 days."
                };
            }

            var matchScores = await _unitOfWork.Repository<Domain.Entities.Match.MatchPlayerRating>()
                .GetQueryableAsNoTracking()
                .Where(mr => playerIdsToAnalyze.Contains(mr.PlayerId)
                          && mr.CreatedAt >= cutoffDate)
                .GroupBy(mr => mr.PlayerId)
                .Select(g => new
                {
                    PlayerId = g.Key,
                    AvgMatchScore = g.Average(x => x.Rating)
                })
                .ToListAsync();

            decimal totalDelta = 0;
            int validPlayerComparisons = 0;

            foreach (var practice in practiceScores)
            {
                var match = matchScores.FirstOrDefault(m => m.PlayerId == practice.PlayerId);

                if (match != null)
                {
                    var delta = Math.Abs(practice.AvgPracticeScore - match.AvgMatchScore);
                    totalDelta += delta;
                    validPlayerComparisons++;
                }
            }

            if (validPlayerComparisons == 0)
            {
                return new CoachBiasReportDto
                {
                    CoachId = coachUserId,
                    TrustPercentage = 100,
                    PlayersAnalyzedCount = 0,
                    Remarks = "Players practiced but played no matches in the last 30 days to compare against."
                };
            }

            decimal averageDelta = totalDelta / validPlayerComparisons;
            decimal rawTrustPercentage = 100 - (averageDelta * 10);

            decimal finalTrustPercentage = Math.Max(0, Math.Round(rawTrustPercentage, 2));

            var coachAcademyRecord = await _unitOfWork.Repository<Domain.Entities.Coach.CoachAcademy>()
                .GetQueryable()
                .FirstOrDefaultAsync(ca => ca.CoachUserId == coachUserId && ca.AcademyId == academyId);

            if (coachAcademyRecord != null)
            {
                coachAcademyRecord.BiasScore = finalTrustPercentage;
                coachAcademyRecord.BiasLastCalculatedAt = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync();
            }

            return new CoachBiasReportDto
            {
                CoachId = coachUserId,
                TrustPercentage = finalTrustPercentage,
                PlayersAnalyzedCount = validPlayerComparisons,
                Remarks = "Trust Index calculated successfully."
            };
        }
    }
}