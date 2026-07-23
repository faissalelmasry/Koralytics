using Koralytics.Application.DTOs.Academies;
using Koralytics.Application.Interfaces;
using Koralytics.Domain.Entities.Coach;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;

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
            _logger.LogInformation("Building coach performance dashboard for academy {AcademyId}", academyId);

            // Academy must exist
            _ = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                .FindAsNoTrackingAsync(a => a.Id == academyId)
                ?? throw new NotFoundException($"Academy with Id {academyId} not found.");

            var (coaches, teams, playerTeams, cards) = await FetchDashboardDataAsync(academyId);

            var results = MapToCoachPerformanceDtos(coaches, teams, playerTeams, cards);

            _logger.LogInformation(
                "Coach performance dashboard built for academy {AcademyId}: {Count} coaches.",
                academyId, results.Count);

            return results;
        }

        private async Task<(List<CoachData> Coaches, List<CoachTeamData> Teams, List<PlayerTeamData> PlayerTeams, List<PlayerCardData> Cards)> FetchDashboardDataAsync(int academyId)
        {
            var coaches = await _unitOfWork.Repository<CoachAcademy>()
                .GetQueryableAsNoTracking()
                .Where(ca => ca.AcademyId == academyId && ca.LeftAt == null)
                .Select(ca => new CoachData
                {
                    CoachUserId = ca.CoachUserId,
                    BiasScore = ca.BiasScore ?? 0m,
                    FirstName = ca.Coach != null ? ca.Coach.FirstName : string.Empty,
                    LastName = ca.Coach != null ? ca.Coach.LastName : string.Empty
                })
                .ToListAsync();

            var coachIds = coaches.Select(c => c.CoachUserId).ToList();

            var teams = await _unitOfWork.Repository<CoachTeam>()
                .GetQueryableAsNoTracking()
                .Where(ct => coachIds.Contains(ct.CoachUserId) && ct.Team.AcademyId == academyId && ct.RemovedAt == null)
                .Select(ct => new CoachTeamData
                {
                    CoachUserId = ct.CoachUserId,
                    TeamId = ct.TeamId,
                    TeamName = ct.Team.Name
                })
                .Distinct()
                .ToListAsync();

            var teamIds = teams.Select(t => t.TeamId).Distinct().ToList();

            var playerTeams = await _unitOfWork.Repository<PlayerTeam>()
                .GetQueryableAsNoTracking()
                .Where(pt => teamIds.Contains(pt.TeamId) && pt.LeftAt == null)
                .Select(pt => new PlayerTeamData
                {
                    TeamId = pt.TeamId,
                    PlayerId = pt.PlayerId
                })
                .ToListAsync();

            var playerIds = playerTeams.Select(pt => pt.PlayerId).Distinct().ToList();

            var cards = await _unitOfWork.Repository<PlayerCard>()
                .GetQueryableAsNoTracking()
                .Where(pc => playerIds.Contains(pc.PlayerId))
                .Select(pc => new PlayerCardData
                {
                    PlayerId = pc.PlayerId,
                    OverallRating = pc.OverallRating,
                    OverallTrainingAvg = pc.OverallTrainingAvg
                })
                .ToListAsync();

            return (coaches, teams, playerTeams, cards);
        }

        private List<CoachPerformanceDto> MapToCoachPerformanceDtos(
            List<CoachData> coaches,
            List<CoachTeamData> teams,
            List<PlayerTeamData> playerTeams,
            List<PlayerCardData> cards)
        {
            var results = new List<CoachPerformanceDto>();

            // Convert to lookups/dictionaries for faster processing
            var teamsByCoach = teams.ToLookup(t => t.CoachUserId);
            var playersByTeam = playerTeams.ToLookup(pt => pt.TeamId);
            var cardByPlayer = cards.ToDictionary(c => c.PlayerId);

            foreach (var coach in coaches)
            {
                var coachActiveTeams = teamsByCoach[coach.CoachUserId];
                var teamSummaries = new List<CoachTeamSummaryDto>();
                var improvementRates = new List<decimal>();

                foreach (var team in coachActiveTeams)
                {
                    var teamPlayers = playersByTeam[team.TeamId];

                    foreach (var player in teamPlayers)
                    {
                        if (cardByPlayer.TryGetValue(player.PlayerId, out var card) && card.OverallTrainingAvg > 0)
                        {
                            var improvement = card.OverallRating - card.OverallTrainingAvg;
                            improvementRates.Add(improvement);
                        }
                    }

                    teamSummaries.Add(new CoachTeamSummaryDto
                    {
                        TeamId = team.TeamId,
                        TeamName = team.TeamName,
                        PlayerCount = teamPlayers.Count()
                    });
                }

                var avgImprovement = improvementRates.Count > 0 ? improvementRates.Average() : 0m;

                results.Add(new CoachPerformanceDto
                {
                    CoachUserId = coach.CoachUserId,
                    CoachFullName = $"{coach.FirstName} {coach.LastName}".Trim(),
                    AveragePlayerImprovementRate = Math.Round(avgImprovement, 2),
                    BiasScore = coach.BiasScore,
                    Teams = teamSummaries
                });
            }

            int rank = 1;
            foreach (var dto in results.OrderByDescending(r => r.AveragePlayerImprovementRate))
            {
                dto.Rank = rank++;
            }

            return results.OrderBy(r => r.Rank).ToList();
        }

        // ──────────────────────────────────────────────────────────────────────
        // GetSubscriptionStatusAsync
        // ──────────────────────────────────────────────────────────────────────
        public async Task<SubscriptionStatusSummaryDto> GetSubscriptionStatusAsync(int academyId)
        {
            _logger.LogInformation("Fetching subscription status for academy {AcademyId}", academyId);

            _ = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                .FindAsNoTrackingAsync(a => a.Id == academyId)
                ?? throw new NotFoundException($"Academy with Id {academyId} not found.");

            // Fetch the ID of the latest subscription record per player in this academy
            var latestSubscriptionIds = await _unitOfWork.Repository<PlayerSubscription>()
                .GetQueryableAsNoTracking()
                .Where(ps => ps.AcademyId == academyId)
                .GroupBy(ps => ps.PlayerId)
                .Select(g => g.Max(ps => ps.Id))
                .ToListAsync();

            // Retrieve only the necessary fields for those latest subscriptions
            var latestPerPlayer = await _unitOfWork.Repository<PlayerSubscription>()
                .GetQueryableAsNoTracking()
                .Where(ps => latestSubscriptionIds.Contains(ps.Id))
                .Select(ps => new
                {
                    ps.PlayerId,
                    ps.Status,
                    ps.GraceUntil,
                    FirstName = ps.Player != null ? ps.Player.FirstName : string.Empty,
                    LastName = ps.Player != null ? ps.Player.LastName : string.Empty
                })
                .ToListAsync();

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
                            PlayerFullName = $"{sub.FirstName} {sub.LastName}".Trim(),
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
                            PlayerFullName = $"{sub.FirstName} {sub.LastName}".Trim(),
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
