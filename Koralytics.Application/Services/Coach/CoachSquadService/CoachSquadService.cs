using Koralytics.Application.DTOs.Coach;
using Koralytics.Application.Interfaces;
using Koralytics.Domain.Entities.Drill;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using CoachEntity = Koralytics.Domain.Entities.Coach.Coach;
using CoachTeamEntity = Koralytics.Domain.Entities.Coach.CoachTeam;
using PlayerEntity = Koralytics.Domain.Entities.Player.Player;
using PlayerCardEntity = Koralytics.Domain.Entities.Player.PlayerCard;
using DrillSessionEntity = Koralytics.Domain.Entities.Drill.DrillSession;
using TeamEntity = Koralytics.Domain.Entities.Academy.Team;

namespace Koralytics.Application.Services.Coach.CoachSquadService
{
    public class CoachSquadService : ICoachSquadService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CoachSquadService> _logger;

        public CoachSquadService(IUnitOfWork unitOfWork, ILogger<CoachSquadService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GetSquadAsync
        // ─────────────────────────────────────────────────────────────────────
        public async Task<SquadOverviewDto> GetSquadAsync(int coachId, int teamId)
        {
            _logger.LogInformation(
                "Coach {CoachId} requesting squad overview for team {TeamId}", coachId, teamId);

            // 1. Verify the coach is assigned to this team
            var coachTeam = await _unitOfWork.Repository<CoachTeamEntity>()
                .FindAsync(ct => ct.CoachUserId == coachId && ct.TeamId == teamId && ct.RemovedAt == null);

            if (coachTeam is null)
                throw new ForbiddenException(
                    $"Coach {coachId} is not assigned to team {teamId} or has been removed.");

            // 2. Load team info
            var team = await _unitOfWork.Repository<TeamEntity>()
                .GetByIdAsNoTrackingAsync(teamId)
                ?? throw new NotFoundException($"Team with Id {teamId} not found.");

            // 3. Load all active PlayerTeam records for the team, eagerly include player data
            var playerTeams = await _unitOfWork.Repository<PlayerTeam>()
                .GetQueryableAsNoTracking()
                .Where(pt => pt.TeamId == teamId && pt.LeftAt == null)
                .Include(pt => pt.Player)
                    .ThenInclude(p => p.PlayerPositions)
                .Include(pt => pt.Player)
                    .ThenInclude(p => p.PlayerSubscriptions)
                .ToListAsync();

            // 4. Load player cards separately (not all players will have a card yet)
            var playerIds = playerTeams.Select(pt => pt.PlayerId).ToList();

            var playerCards = await _unitOfWork.Repository<PlayerCardEntity>()
                .GetQueryableAsNoTracking()
                .Where(pc => playerIds.Contains(pc.PlayerId))
                .Include(pc => pc.CategoryRatings)
                    .ThenInclude(cr => cr.DrillCategory)
                .ToListAsync();

            var cardByPlayerId = playerCards.ToDictionary(pc => pc.PlayerId);

            // 5. Map to DTOs
            var squadPlayers = playerTeams
                .Select(pt => MapToSquadPlayerDto(pt.Player, cardByPlayerId))
                .ToList();

            return new SquadOverviewDto
            {
                TeamId = teamId,
                TeamName = team.Name,
                Players = squadPlayers
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // SplitTrainingTeamsAsync
        // ─────────────────────────────────────────────────────────────────────
        public async Task<TrainingTeamSplitDto> SplitTrainingTeamsAsync(int coachId, int sessionId)
        {
            _logger.LogInformation(
                "Coach {CoachId} requesting training split for session {SessionId}", coachId, sessionId);

            // 1. Validate session exists and belongs to this coach
            var session = await _unitOfWork.Repository<DrillSessionEntity>()
                .FindAsync(s => s.Id == sessionId);

            if (session is null)
                throw new NotFoundException($"DrillSession with Id {sessionId} not found.");

            if (session.CoachId != coachId)
                throw new ForbiddenException(
                    $"Coach {coachId} does not own session {sessionId}.");

            // 2. Fetch attending players (IsPresent = true)
            var attendances = await _unitOfWork.Repository<SessionAttendance>()
                .GetQueryableAsNoTracking()
                .Where(sa => sa.SessionId == sessionId && sa.IsPresent)
                .ToListAsync();

            if (!attendances.Any())
                throw new BadRequestException(
                    $"No present players found for session {sessionId}. Mark attendance before splitting.");

            var attendingPlayerIds = attendances.Select(a => a.playerId).ToList();

            // 3. Load player entities with their card and positions
            var players = await _unitOfWork.Repository<PlayerEntity>()
                .GetQueryableAsNoTracking()
                .Where(p => attendingPlayerIds.Contains(p.Id))
                .Include(p => p.PlayerPositions)
                .ToListAsync();

            var playerCards = await _unitOfWork.Repository<PlayerCardEntity>()
                .GetQueryableAsNoTracking()
                .Where(pc => attendingPlayerIds.Contains(pc.PlayerId))
                .Include(pc => pc.CategoryRatings)
                    .ThenInclude(cr => cr.DrillCategory)
                .ToListAsync();

            var cardByPlayerId = playerCards.ToDictionary(pc => pc.PlayerId);

            // 4. Sort players by OverallRating descending (players without a card get 0)
            var sortedPlayers = players
                .OrderByDescending(p =>
                    cardByPlayerId.TryGetValue(p.Id, out var card) ? card.OverallRating : 0m)
                .ToList();

            // 5. Snake-draft alternation for balanced teams
            var teamA = new List<SquadPlayerDto>();
            var teamB = new List<SquadPlayerDto>();

            for (int i = 0; i < sortedPlayers.Count; i++)
            {
                var dto = MapToSquadPlayerDto(sortedPlayers[i], cardByPlayerId);

                // Even index → Team A, Odd index → Team B
                if (i % 2 == 0)
                    teamA.Add(dto);
                else
                    teamB.Add(dto);
            }

            _logger.LogInformation(
                "Session {SessionId} split: TeamA={CountA} players, TeamB={CountB} players",
                sessionId, teamA.Count, teamB.Count);

            return new TrainingTeamSplitDto
            {
                SessionId = sessionId,
                TeamA = teamA,
                TeamB = teamB
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // GetSquadComparisonAsync
        // ─────────────────────────────────────────────────────────────────────
        public async Task<SquadComparisonDto> GetSquadComparisonAsync(int playerAId, int playerBId)
        {
            _logger.LogInformation(
                "Squad comparison requested for players {PlayerAId} and {PlayerBId}",
                playerAId, playerBId);

            // 1. Load both players with positions
            var players = await _unitOfWork.Repository<PlayerEntity>()
                .GetQueryableAsNoTracking()
                .Where(p => p.Id == playerAId || p.Id == playerBId)
                .Include(p => p.PlayerPositions)
                .ToListAsync();

            var playerA = players.FirstOrDefault(p => p.Id == playerAId)
                ?? throw new NotFoundException($"Player with Id {playerAId} not found.");

            var playerB = players.FirstOrDefault(p => p.Id == playerBId)
                ?? throw new NotFoundException($"Player with Id {playerBId} not found.");

            // 2. Load their player cards
            var cards = await _unitOfWork.Repository<PlayerCardEntity>()
                .GetQueryableAsNoTracking()
                .Where(pc => pc.PlayerId == playerAId || pc.PlayerId == playerBId)
                .Include(pc => pc.CategoryRatings)
                    .ThenInclude(cr => cr.DrillCategory)
                .ToListAsync();

            var cardByPlayerId = cards.ToDictionary(pc => pc.PlayerId);

            return new SquadComparisonDto
            {
                PlayerA = MapToSquadPlayerDto(playerA, cardByPlayerId),
                PlayerB = MapToSquadPlayerDto(playerB, cardByPlayerId)
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private helper — maps a Player + optional PlayerCard → SquadPlayerDto
        // ─────────────────────────────────────────────────────────────────────
        private static SquadPlayerDto MapToSquadPlayerDto(
            PlayerEntity player,
            Dictionary<int, PlayerCardEntity> cardByPlayerId)
        {
            cardByPlayerId.TryGetValue(player.Id, out var card);

            var primaryPosition = player.PlayerPositions
                .FirstOrDefault(pp => pp.IsPrimary)?.Position
                ?? player.PlayerPositions.FirstOrDefault()?.Position
                ?? "Unknown";

            // Helper: find category rating by name (case-insensitive)
            decimal GetCategoryScore(string categoryName)
            {
                if (card is null) return 0m;
                var rating = card.CategoryRatings
                    .FirstOrDefault(cr =>
                        cr.DrillCategory.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
                return rating?.Score ?? 0m;
            }

            return new SquadPlayerDto
            {
                PlayerId = player.Id,
                FullName = $"{player.FirstName} {player.LastName}",
                ProfileImageUrl = player.ProfileImageUrl,
                PrimaryPosition = primaryPosition,
                AvailabilityStatus = player.AvailabilityStatus,
                OverallRating = card?.OverallRating ?? 0m,
                PaceRating = GetCategoryScore("Pace"),
                DribblingRating = GetCategoryScore("Dribbling"),
                ShootingRating = GetCategoryScore("Shooting"),
                DefendingRating = GetCategoryScore("Defending"),
                PassingRating = GetCategoryScore("Passing"),
                PhysicalRating = GetCategoryScore("Physical"),
                GoalkeepingRating = primaryPosition.Equals("GK", StringComparison.OrdinalIgnoreCase)
                    ? GetCategoryScore("Goalkeeping")
                    : null,
                PreferredFoot = player.PreferredFoot,
                WeakFootRating = player.WeakFootRating,
                ArchetypePlayerName = player.ArchetypePlayerName,
                PlayStyleTag = player.PlayStyleTag
            };
        }
    }
}
