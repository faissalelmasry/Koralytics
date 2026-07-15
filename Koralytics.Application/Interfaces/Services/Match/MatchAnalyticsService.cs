using Koralytics.Application.DTOs.Match;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Match;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Exceptions;
using DomainEnums = Koralytics.Domain.Enums;
using MatchEntity = Koralytics.Domain.Entities.Match.Match;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koralytics.Application.Services.Match
{
    public class MatchAnalyticsService : IMatchAnalyticsService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MatchAnalyticsService> _logger;

        public MatchAnalyticsService(
            IUnitOfWork unitOfWork,
            ILogger<MatchAnalyticsService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<HeadToHeadResponseDto> GetHeadToHeadAsync(int teamAId, int teamBId)
        {
            _logger.LogInformation("Getting head-to-head: Team {TeamA} vs Team {TeamB}",
                teamAId, teamBId);

            var teamA = await _unitOfWork.Repository<Team>()
                .FindAsNoTrackingAsync(t => t.Id == teamAId);

            if (teamA is null)
                throw new NotFoundException($"Team with Id {teamAId} not found");

            var teamB = await _unitOfWork.Repository<Team>()
                .FindAsNoTrackingAsync(t => t.Id == teamBId);

            if (teamB is null)
                throw new NotFoundException($"Team with Id {teamBId} not found");

            var matches = await _unitOfWork.Repository<MatchEntity>()
                .GetQueryableAsNoTracking()
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Where(m => m.Status == DomainEnums.MatchStatus.Completed
                    && m.Type != DomainEnums.MatchType.Session
                    && ((m.HomeTeamId == teamAId && m.AwayTeamId == teamBId)
                        || (m.HomeTeamId == teamBId && m.AwayTeamId == teamAId)))
                .OrderByDescending(m => m.MatchDate)
                .ToListAsync();

            int teamAWins = 0, teamBWins = 0, draws = 0;

            foreach (var m in matches)
            {
                if (IsDraw(m))
                {
                    draws++;
                }
                else if (m.HomeTeamId == teamAId && IsHomeWin(m)
                    || m.AwayTeamId == teamAId && IsAwayWin(m))
                {
                    teamAWins++;
                }
                else
                {
                    teamBWins++;
                }
            }

            return new HeadToHeadResponseDto
            {
                TeamAId = teamAId,
                TeamAName = teamA.Name,
                TeamBId = teamBId,
                TeamBName = teamB.Name,
                TotalMatches = matches.Count,
                TeamAWins = teamAWins,
                TeamBWins = teamBWins,
                Draws = draws,
                Matches = matches.Select(m => new HeadToHeadMatchDto
                {
                    MatchId = m.Id,
                    MatchDate = m.MatchDate,
                    HomeTeamName = m.HomeTeam.Name,
                    AwayTeamName = m.AwayTeam.Name,
                    HomeScore = m.HomeScore,
                    AwayScore = m.AwayScore
                }).ToList()
            };
        }

        public async Task<PostMatchAnalysisResponseDto> GetPostMatchAnalysisAsync(int teamId)
        {
            _logger.LogInformation("Getting post-match analysis for team {TeamId}", teamId);

            var team = await _unitOfWork.Repository<Team>()
                .FindAsNoTrackingAsync(t => t.Id == teamId);

            if (team is null)
                throw new NotFoundException($"Team with Id {teamId} not found");

            var matches = await _unitOfWork.Repository<MatchEntity>()
                .GetQueryableAsNoTracking()
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Where(m => m.Status == DomainEnums.MatchStatus.Completed
                    && m.Type != DomainEnums.MatchType.Session
                    && (m.HomeTeamId == teamId || m.AwayTeamId == teamId))
                .OrderByDescending(m => m.MatchDate)
                .Take(10)
                .ToListAsync();

            int wins = 0, losses = 0, draws = 0;
            int goalsFor = 0, goalsAgainst = 0;
            var recentMatches = new List<PostMatchAnalysisMatchDto>();

            foreach (var m in matches)
            {
                bool isHome = m.HomeTeamId == teamId;
                int gf = isHome ? m.HomeScore : m.AwayScore;
                int ga = isHome ? m.AwayScore : m.HomeScore;
                string result;

                if (IsDraw(m))
                {
                    result = "D";
                    draws++;
                }
                else if (isHome && IsHomeWin(m) || !isHome && IsAwayWin(m))
                {
                    result = "W";
                    wins++;
                }
                else
                {
                    result = "L";
                    losses++;
                }

                goalsFor += gf;
                goalsAgainst += ga;

                recentMatches.Add(new PostMatchAnalysisMatchDto
                {
                    MatchId = m.Id,
                    MatchDate = m.MatchDate,
                    OpponentName = isHome ? m.AwayTeam.Name : m.HomeTeam.Name,
                    Result = result,
                    GoalsFor = gf,
                    GoalsAgainst = ga
                });
            }

            return new PostMatchAnalysisResponseDto
            {
                TeamId = teamId,
                TeamName = team.Name,
                Wins = wins,
                Losses = losses,
                Draws = draws,
                GoalsFor = goalsFor,
                GoalsAgainst = goalsAgainst,
                RecentMatches = recentMatches
            };
        }

        public async Task<PlayerReadinessDto> GetPlayerReadinessAsync(int playerId)
        {
            _logger.LogInformation("Getting player readiness for player {PlayerId}", playerId);

            var player = await _unitOfWork.Repository<Koralytics.Domain.Entities.Player.Player>()
                .FindAsNoTrackingAsync(p => p.Id == playerId);

            if (player == null)
                throw new NotFoundException($"Player with Id {playerId} not found");

            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

            var recentMatchesCount = await _unitOfWork.Repository<Koralytics.Domain.Entities.Match.MatchLineup>()
                .GetQueryableAsNoTracking()
                .Include(ml => ml.Match)
                .Where(ml => ml.PlayerId == playerId && ml.Match.MatchDate >= sevenDaysAgo && ml.Match.Status == DomainEnums.MatchStatus.Completed)
                .CountAsync();

            var dto = new PlayerReadinessDto
            {
                PlayerId = player.Id,
                PlayerName = $"{player.FirstName} {player.LastName}",
                MatchesPlayedLast7Days = recentMatchesCount
            };

            switch (player.AvailabilityStatus)
            {
                case DomainEnums.AvailabilityStatus.Injured:
                    dto.ReadinessScore = 0;
                    dto.Status = "Injured";
                    dto.Recommendation = "Player needs medical clearance.";
                    break;
                case DomainEnums.AvailabilityStatus.Suspended:
                    dto.ReadinessScore = 0;
                    dto.Status = "Suspended";
                    dto.Recommendation = "Player is suspended and cannot play.";
                    break;
                case DomainEnums.AvailabilityStatus.Resting:
                    dto.ReadinessScore = 0;
                    dto.Status = "Resting";
                    dto.Recommendation = "Player is currently on a rest period.";
                    break;
                default:
                    // Available
                    if (recentMatchesCount >= 3)
                    {
                        dto.ReadinessScore = 30;
                        dto.Status = "Highly Fatigued";
                        dto.Recommendation = "Player has played heavily in the last 7 days. High risk of injury. Needs rest.";
                    }
                    else if (recentMatchesCount == 2)
                    {
                        dto.ReadinessScore = 60;
                        dto.Status = "Fatigued";
                        dto.Recommendation = "Player has played 2 matches recently. Monitor minutes carefully.";
                    }
                    else if (recentMatchesCount == 1)
                    {
                        dto.ReadinessScore = 85;
                        dto.Status = "Match Fit";
                        dto.Recommendation = "Player is in good rhythm and match fit.";
                    }
                    else
                    {
                        dto.ReadinessScore = 100;
                        dto.Status = "Fully Rested";
                        dto.Recommendation = "Player is fully rested and ready to play.";
                    }
                    break;
            }

            return dto;
        }

        private static bool IsDraw(MatchEntity m)
        {
            if (m.HomeScore != m.AwayScore) return false;

            if (!m.HomePenaltyScore.HasValue && !m.AwayPenaltyScore.HasValue)
                return true;

            return m.HomePenaltyScore == m.AwayPenaltyScore;
        }

        private static bool IsHomeWin(MatchEntity m)
        {
            if (m.HomeScore != m.AwayScore)
                return m.HomeScore > m.AwayScore;

            if (m.HomePenaltyScore.HasValue && m.AwayPenaltyScore.HasValue)
                return m.HomePenaltyScore > m.AwayPenaltyScore;

            return false;
        }

        private static bool IsAwayWin(MatchEntity m)
        {
            if (m.HomeScore != m.AwayScore)
                return m.AwayScore > m.HomeScore;

            if (m.HomePenaltyScore.HasValue && m.AwayPenaltyScore.HasValue)
                return m.AwayPenaltyScore > m.HomePenaltyScore;

            return false;
        }
    }
}
