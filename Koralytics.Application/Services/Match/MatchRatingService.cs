using AutoMapper;
using Koralytics.Application.DTOs.Match;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Match;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Coach;
using Koralytics.Domain.Exceptions;
using PlayerEntity = Koralytics.Domain.Entities.Player.Player;
using DomainEnums = Koralytics.Domain.Enums;
using MatchEntity = Koralytics.Domain.Entities.Match.Match;
using MatchLineupEntity = Koralytics.Domain.Entities.Match.MatchLineup;
using MatchPlayerRatingEntity = Koralytics.Domain.Entities.Match.MatchPlayerRating;
using MatchPlayerCategoryRatingEntity = Koralytics.Domain.Entities.Match.MatchPlayerCategoryRating;
using Koralytics.Application.Services.Player.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koralytics.Application.Services.Match
{
    public class MatchRatingService : IMatchRatingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<MatchRatingService> _logger;
        private readonly ICardInvalidationList _invalidationList;

        public MatchRatingService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<MatchRatingService> logger,
            ICardInvalidationList invalidationList)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _invalidationList = invalidationList;
        }

        public async Task<List<LineupResponseDto>> SubmitLineupAsync(int matchId, int coachId, SubmitLineupDto dto)
        {
            _logger.LogInformation("Coach {CoachId} submitting lineup for match {MatchId} with {Count} players",
                coachId, matchId, dto.Players.Count);

            var match = await _unitOfWork.Repository<MatchEntity>()
                .FindAsync(m => m.Id == matchId);

            if (match is null)
                throw new NotFoundException($"Match with Id {matchId} not found");

            if (match.Type == DomainEnums.MatchType.Session)
                throw new BadRequestException(
                    "Session matches have their lineup embedded at creation time.");

            var coachTeam = await _unitOfWork.Repository<CoachTeam>()
                .GetQueryableAsNoTracking()
                .FirstOrDefaultAsync(ct => ct.CoachUserId == coachId
                    && ct.RemovedAt == null
                    && (ct.TeamId == match.HomeTeamId || ct.TeamId == match.AwayTeamId));

            if (coachTeam is null)
                throw new ForbiddenException($"Coach {coachId} is not assigned to either home or away team of this match.");

            var coachTeamId = coachTeam.TeamId;

            var formatStartingCount = match.Format switch
            {
                DomainEnums.MatchFormat.FiveSide => 5,
                DomainEnums.MatchFormat.SevenSide => 7,
                DomainEnums.MatchFormat.ElevenSide => 11,
                _ => throw new BadRequestException("Invalid match format.")
            };

            var startingCount = dto.Players.Count(p => p.IsStarting);
            if (startingCount != formatStartingCount)
                throw new BadRequestException(
                    $"Each team must have exactly {formatStartingCount} starting players ({startingCount} provided).");

            if (dto.Players.Any(p => p.TeamId != coachTeamId))
                throw new BadRequestException(
                    $"Coach can only submit lineup for team {coachTeamId}.");

            var existingEntries = await _unitOfWork.Repository<MatchLineupEntity>()
                .GetQueryable()
                .Where(ml => ml.MatchId == matchId && ml.TeamId == coachTeamId)
                .ToListAsync();

            if (existingEntries.Count != 0)
                _unitOfWork.Repository<MatchLineupEntity>().SoftDeleteRange(existingEntries);

            foreach (var player in dto.Players)
            {
                var playerExists = await _unitOfWork.Repository<PlayerEntity>()
                    .ExistsAsync(p => p.Id == player.PlayerId);

                if (!playerExists)
                    throw new NotFoundException($"Player with Id {player.PlayerId} not found");

                var lineup = _mapper.Map<MatchLineupEntity>(player);
                lineup.MatchId = matchId;
                await _unitOfWork.Repository<MatchLineupEntity>().AddAsync(lineup);
            }

            await _unitOfWork.SaveChangesAsync();

            var created = await _unitOfWork.Repository<MatchLineupEntity>()
                .GetQueryableAsNoTracking()
                .Include(ml => ml.Player)
                .Include(ml => ml.Team)
                .Where(ml => ml.MatchId == matchId)
                .ToListAsync();

            _logger.LogInformation("Lineup submitted for match {MatchId} by coach {CoachId}: {Count} players",
                matchId, coachId, created.Count);

            return _mapper.Map<List<LineupResponseDto>>(created);
        }

        public async Task<List<LineupResponseDto>> GetLineupAsync(int matchId)
        {
            var matchExists = await _unitOfWork.Repository<MatchEntity>()
                .ExistsAsync(m => m.Id == matchId);

            if (!matchExists)
                throw new NotFoundException($"Match with Id {matchId} not found");

            var lineup = await _unitOfWork.Repository<MatchLineupEntity>()
                .GetQueryableAsNoTracking()
                .Include(ml => ml.Player)
                .Include(ml => ml.Team)
                .Where(ml => ml.MatchId == matchId)
                .ToListAsync();

            return _mapper.Map<List<LineupResponseDto>>(lineup);
        }

        public async Task<MatchResponseDto> SubmitMatchRatingsAsync(int matchId, int coachId, SubmitMatchRatingsDto dto)
        {
            _logger.LogInformation("Coach {CoachId} submitting ratings for match {MatchId} with {Count} players",
                coachId, matchId, dto.Ratings.Count);

            var match = await _unitOfWork.Repository<MatchEntity>()
                .GetQueryable()
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Include(m => m.MatchEvents)
                .Include(m => m.MatchLineups)
                .Include(m => m.MatchPlayerRatings)
                .FirstOrDefaultAsync(m => m.Id == matchId);

            if (match is null)
                throw new NotFoundException($"Match with Id {matchId} not found");

            if (match.Status != DomainEnums.MatchStatus.Completed)
                throw new BadRequestException("Ratings can only be submitted after the match has ended.");

            var eligiblePlayerIds = await GetEligiblePlayerIdsAsync(match, coachId);

            if (dto.Ratings.Any(r => !eligiblePlayerIds.Contains(r.PlayerId)))
                throw new BadRequestException("Coach can only rate players from their own team.");

            var motmCount = dto.Ratings.Count(r => r.IsMOTM);
            if (motmCount != 1)
                throw new BadRequestException("Exactly one MOTM must be selected.");

            var playerGoals = match.MatchEvents
                .Where(e => IsGoalEvent(e.EventType))
                .GroupBy(e => e.PlayerId)
                .ToDictionary(g => g.Key, g => g.Count());

            var playerAssists = match.MatchEvents
                .Where(e => e.AssistPlayerId.HasValue)
                .GroupBy(e => e.AssistPlayerId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var rating in dto.Ratings)
            {
                var isInLineup = match.MatchLineups
                    .Any(ml => ml.PlayerId == rating.PlayerId);

                if (!isInLineup)
                    throw new BadRequestException($"Player {rating.PlayerId} is not in the lineup.");

                var matchPlayerRating = new MatchPlayerRatingEntity
                {
                    MatchId = matchId,
                    PlayerId = rating.PlayerId,
                    CoachId = coachId,
                    Goals = playerGoals.GetValueOrDefault(rating.PlayerId, 0),
                    Assists = playerAssists.GetValueOrDefault(rating.PlayerId, 0),
                    MinutesPlayed = rating.MinutesPlayed,
                    IsMOTM = rating.IsMOTM,
                    CoachNote = rating.CoachNote
                };

                await _unitOfWork.Repository<MatchPlayerRatingEntity>().AddAsync(matchPlayerRating);
                await _unitOfWork.SaveChangesAsync();

                foreach (var category in rating.CategoryRatings)
                {
                    var categoryRating = new MatchPlayerCategoryRatingEntity
                    {
                        MatchPlayerRatingId = matchPlayerRating.Id,
                        DrillCategoryId = category.DrillCategoryId,
                        Rating = category.Rating
                    };

                    await _unitOfWork.Repository<MatchPlayerCategoryRatingEntity>().AddAsync(categoryRating);
                }

                _invalidationList.Invalidate(rating.PlayerId);
            }

            await _unitOfWork.SaveChangesAsync();

            var updated = await _unitOfWork.Repository<MatchEntity>()
                .GetQueryableAsNoTracking()
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Include(m => m.WinningTeam)
                .FirstOrDefaultAsync(m => m.Id == matchId);

            _logger.LogInformation(
                "Ratings submitted for match {MatchId} by coach {CoachId}",
                matchId, coachId);

            return _mapper.Map<MatchResponseDto>(updated!);
        }

        private async Task<HashSet<int>> GetEligiblePlayerIdsAsync(MatchEntity match, int coachId)
        {
            if (match.Type == DomainEnums.MatchType.Tournament)
                return match.MatchLineups.Select(ml => ml.PlayerId).ToHashSet();

            var coachTeam = await _unitOfWork.Repository<CoachTeam>()
                .GetQueryableAsNoTracking()
                .FirstOrDefaultAsync(ct => ct.CoachUserId == coachId
                    && ct.RemovedAt == null
                    && (ct.TeamId == match.HomeTeamId || ct.TeamId == match.AwayTeamId));

            if (coachTeam is null)
                throw new ForbiddenException(
                    $"Coach {coachId} is not assigned to either team of this match.");

            if (match.Type == DomainEnums.MatchType.Session)
                return match.MatchLineups.Select(ml => ml.PlayerId).ToHashSet();

            return match.MatchLineups
                .Where(ml => ml.TeamId == coachTeam.TeamId)
                .Select(ml => ml.PlayerId)
                .ToHashSet();
        }

        private static bool IsGoalEvent(DomainEnums.MatchEventType eventType)
        {
            return eventType == DomainEnums.MatchEventType.Goal;
        }
    }
}
