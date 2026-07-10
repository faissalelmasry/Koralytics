using Koralytics.Application.DTOs.Academies;
using Koralytics.Application.Interfaces;
using Koralytics.Domain.Entities.Coach;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koralytics.Application.Services.Academy.AcademyAnalyticsService
{
    public class AcademyAnalyticsService : IAcademyAnalyticsService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AcademyAnalyticsService> _logger;

        public AcademyAnalyticsService(IUnitOfWork unitOfWork, ILogger<AcademyAnalyticsService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // ──────────────────────────────────────────────────────────────────────
        // GetCoachPerformanceDashboardAsync
        // ──────────────────────────────────────────────────────────────────────
        public async Task<IEnumerable<CoachPerformanceDto>> GetCoachPerformanceDashboardAsync(int academyId)
        {
            _logger.LogInformation(
                "Building coach performance dashboard for academy {AcademyId}", academyId);

            // Academy must exist
            _ = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                .FindAsNoTrackingAsync(a => a.Id == academyId)
                ?? throw new NotFoundException($"Academy with Id {academyId} not found.");

            // Fetch all active coaches in the academy with their team assignments
            var coachAcademies = await _unitOfWork.Repository<CoachAcademy>()
                .GetQueryableAsNoTracking()
                .Include(ca => ca.Coach)
                .Where(ca => ca.AcademyId == academyId && ca.LeftAt == null )        
                .ToListAsync();

            var results = new List<CoachPerformanceDto>();

            foreach (var ca in coachAcademies)
            {
                // Fetch all teams this coach is actively assigned to within this academy
                var activeTeamIds = await _unitOfWork.Repository<CoachTeam>()
                    .GetQueryableAsNoTracking()
                    .Include(ct => ct.Team)
                    .Where(ct => ct.CoachUserId == ca.CoachUserId && ct.Team.AcademyId == academyId && ct.RemovedAt == null)
                    .Select(ct => new { ct.TeamId, ct.Team.Name })
                    .Distinct()
                    .ToListAsync();

                var teamSummaries = new List<CoachTeamSummaryDto>();
                var improvementRates = new List<decimal>();

                foreach (var t in activeTeamIds)
                {
                    // Fetch active players in this team {id,Full Names}
                    var playerIds = await _unitOfWork.Repository<PlayerTeam>()
                        .GetQueryableAsNoTracking()
                        .Where(pt => pt.TeamId == t.TeamId && pt.LeftAt == null )            
                        .Select(pt => pt.PlayerId)
                        .ToListAsync();

                    // For each player, compute improvement rate from PlayerCard
                    // Improvement = OverallRating - OverallTrainingAvg (baseline proxy)
                    // TODO: Refine improvement formula with the AI/Analytics teammate
                    foreach (var playerId in playerIds)
                    {
                        var card = await _unitOfWork.Repository<PlayerCard>()
                            .FindAsNoTrackingAsync(pc => pc.PlayerId == playerId );

                        if (card is not null && card.OverallTrainingAvg > 0)
                        {
                            var improvement = card.OverallRating - card.OverallTrainingAvg;
                            improvementRates.Add(improvement);
                        }
                    }

                    teamSummaries.Add(new CoachTeamSummaryDto
                    {
                        TeamId = t.TeamId,
                        TeamName = t.Name,
                        PlayerCount = playerIds.Count
                    });
                }

                var avgImprovement = improvementRates.Count > 0 ? improvementRates.Average() : 0m;

                // TODO: BiasScore is calculated by the AI/Analytics module background job
                // and stored in CoachAcademy.BiasScore. We read it directly from the record.
                var biasScore = ca.BiasScore;

                results.Add(new CoachPerformanceDto
                {
                    CoachUserId = ca.CoachUserId,
                    CoachFullName = ca.Coach is not null
                        ? $"{ca.Coach.FirstName} {ca.Coach.LastName}"
                        : string.Empty,
                    AveragePlayerImprovementRate = Math.Round(avgImprovement, 2),
                    BiasScore = biasScore,
                    Teams = teamSummaries
                });
            }

            // Rank by improvement rate (descending) — best coach = rank 1
            int rank = 1;
            foreach (var dto in results.OrderByDescending(r => r.AveragePlayerImprovementRate))
            {
                dto.Rank = rank++;
            }

            _logger.LogInformation(
                "Coach performance dashboard built for academy {AcademyId}: {Count} coaches.",
                academyId, results.Count);

            return results.OrderBy(r => r.Rank);
        }

        // ──────────────────────────────────────────────────────────────────────
        // GetSubscriptionStatusAsync
        // ──────────────────────────────────────────────────────────────────────
        public async Task<SubscriptionStatusSummaryDto> GetSubscriptionStatusAsync(int academyId)
        {
            _logger.LogInformation(
                "Fetching subscription status for academy {AcademyId}", academyId);

            _ = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                .FindAsNoTrackingAsync(a => a.Id == academyId )
                ?? throw new NotFoundException($"Academy with Id {academyId} not found.");

            // Fetch the latest subscription record per player in this academy
            // We use a per-player grouping: latest subscription decides the player's current status.
            var allSubscriptions = await _unitOfWork.Repository<PlayerSubscription>()
                .GetQueryableAsNoTracking()
                .Include(ps => ps.Player)
                .Where(ps => ps.AcademyId == academyId)
                .ToListAsync();

            // Group by player → take the most recent subscription per player
            var latestPerPlayer = allSubscriptions
                .GroupBy(ps => ps.PlayerId)
                .Select(g => g.OrderByDescending(ps => ps.Id).First())
                .ToList();

            var now = DateTime.UtcNow;
            var unpaidPlayers = new List<UnpaidPlayerDto>();

            int paid = 0, unpaid = 0, grace = 0;

            foreach (var sub in latestPerPlayer)
            {
                switch (sub.Status)
                {
                    case SubscriptionStatus.Paid:
                        paid++;
                        break;

                    case SubscriptionStatus.Unpaid:
                        unpaid++;
                        unpaidPlayers.Add(new UnpaidPlayerDto
                        {
                            PlayerId = sub.PlayerId,
                            PlayerFullName = sub.Player is not null
                                ? $"{sub.Player.FirstName} {sub.Player.LastName}"
                                : string.Empty,
                            Status = SubscriptionStatus.Unpaid,
                            GraceUntil = sub.GraceUntil,
                            IsGraceExpired = sub.GraceUntil.HasValue && sub.GraceUntil < now
                        });
                        break;

                    case SubscriptionStatus.Grace:
                        grace++;
                        unpaidPlayers.Add(new UnpaidPlayerDto
                        {
                            PlayerId = sub.PlayerId,
                            PlayerFullName = sub.Player is not null
                                ? $"{sub.Player.FirstName} {sub.Player.LastName}"
                                : string.Empty,
                            Status = SubscriptionStatus.Grace,
                            GraceUntil = sub.GraceUntil,
                            IsGraceExpired = sub.GraceUntil.HasValue && sub.GraceUntil < now
                        });
                        break;
                }
            }

            _logger.LogInformation(
                "Subscription status for academy {AcademyId}: Paid={Paid}, Unpaid={Unpaid}, Grace={Grace}",
                academyId, paid, unpaid, grace);

            return new SubscriptionStatusSummaryDto
            {
                TotalPaid = paid,
                TotalUnpaid = unpaid,
                TotalGrace = grace,
                TotalPlayers = latestPerPlayer.Count,
                UnpaidPlayers = unpaidPlayers
            };
        }
    }
}
