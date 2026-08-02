using AutoMapper;
using Koralytics.Application.DTOs.Match;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Match;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Exceptions;
using MatchLineupEntity = Koralytics.Domain.Entities.Match.MatchLineup;
using DomainEnums = Koralytics.Domain.Enums;
using MatchEntity = Koralytics.Domain.Entities.Match.Match;
using MatchEventEntity = Koralytics.Domain.Entities.Match.MatchEvent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koralytics.Application.Services.Match
{
    public class MatchEventService : IMatchEventService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<MatchEventService> _logger;
        private readonly IMatchLiveUpdateService _liveUpdateService;

        public MatchEventService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<MatchEventService> logger,
            IMatchLiveUpdateService liveUpdateService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _liveUpdateService = liveUpdateService;
        }

        public async Task<MatchEventResponseDto> LogMatchEventAsync(int matchId, LogMatchEventDto dto)
        {
            _logger.LogInformation(
                "Logging event for match {MatchId}: {EventType} by Player {PlayerId}",
                matchId, dto.EventType, dto.PlayerId);

            var match = await _unitOfWork.Repository<MatchEntity>()
                .GetQueryable()
                .FirstOrDefaultAsync(m => m.Id == matchId);

            if (match is null)
                throw new NotFoundException($"Match with Id {matchId} not found");

            if (match.Status != DomainEnums.MatchStatus.Live)
                throw new BadRequestException("Match events can only be logged while the match is live.");

            if (dto.TeamId != match.HomeTeamId && dto.TeamId != match.AwayTeamId)
                throw new BadRequestException("TeamId must be either the home team or the away team.");

            var isInLineup = await _unitOfWork.Repository<MatchLineupEntity>()
                .ExistsAsync(ml => ml.MatchId == matchId
                    && ml.PlayerId == dto.PlayerId
                    && ml.TeamId == dto.TeamId);

            if (!isInLineup)
                throw new BadRequestException($"Player {dto.PlayerId} is not in the lineup for team {dto.TeamId}.");

            var matchEvent = _mapper.Map<MatchEventEntity>(dto);
            matchEvent.MatchId = matchId;

            await _unitOfWork.Repository<MatchEventEntity>().AddAsync(matchEvent);

            if (IsGoalEvent(dto.EventType))
            {
                if (dto.TeamId == match.HomeTeamId)
                    match.HomeScore++;
                else
                    match.AwayScore++;
            }

            if (IsOwnGoalEvent(dto.EventType))
            {
                if (dto.TeamId == match.HomeTeamId)
                    match.AwayScore++;
                else
                    match.HomeScore++;
            }

            if (IsPenaltyShootoutEvent(dto.EventType))
            {
                if (!match.HomePenaltyScore.HasValue) match.HomePenaltyScore = 0;
                if (!match.AwayPenaltyScore.HasValue) match.AwayPenaltyScore = 0;

                if (dto.TeamId == match.HomeTeamId)
                    match.HomePenaltyScore++;
                else
                    match.AwayPenaltyScore++;
            }

            if (dto.EventType == DomainEnums.MatchEventType.Substitution)
            {
                if (!dto.AssistPlayerId.HasValue)
                    throw new BadRequestException("Substitution event requires an AssistPlayerId representing the incoming player.");

                var subOutLineup = await _unitOfWork.Repository<MatchLineupEntity>()
                    .GetQueryable()
                    .FirstOrDefaultAsync(ml => ml.MatchId == matchId && ml.PlayerId == dto.PlayerId && ml.TeamId == dto.TeamId);

                var subInLineup = await _unitOfWork.Repository<MatchLineupEntity>()
                    .GetQueryable()
                    .FirstOrDefaultAsync(ml => ml.MatchId == matchId && ml.PlayerId == dto.AssistPlayerId.Value && ml.TeamId == dto.TeamId);

                if (subOutLineup != null && subInLineup != null)
                {
                    var outPosition = subOutLineup.PositionInMatch;
                    subOutLineup.IsStarting = false;
                    subOutLineup.PositionInMatch = "SUB";

                    subInLineup.IsStarting = true;
                    if (!string.IsNullOrEmpty(outPosition))
                        subInLineup.PositionInMatch = outPosition;
                }
            }

            await _unitOfWork.SaveChangesAsync();

            var created = await _unitOfWork.Repository<MatchEventEntity>()
                .GetQueryableAsNoTracking()
                .Include(e => e.Team)
                .Include(e => e.Player)
                .Include(e => e.AssistPlayer)
                .FirstOrDefaultAsync(e => e.Id == matchEvent.Id);

            _logger.LogInformation(
                "Match event logged: {EventType} at minute {Minute} for match {MatchId}",
                dto.EventType, dto.Minute, matchId);

            var responseDto = _mapper.Map<MatchEventResponseDto>(created!);

            await _liveUpdateService.BroadcastMatchEventAsync(new LiveMatchEventUpdateDto
            {
                MatchId = matchId,
                Event = responseDto
            });

            await _liveUpdateService.BroadcastMatchScoreUpdateAsync(new LiveMatchScoreUpdateDto
            {
                MatchId = matchId,
                HomeScore = match.HomeScore,
                AwayScore = match.AwayScore,
                HomePenaltyScore = match.HomePenaltyScore,
                AwayPenaltyScore = match.AwayPenaltyScore,
                Status = match.Status.ToString()
            });

            return responseDto;
        }

        public async Task<MatchEventResponseDto> LogSessionMatchEventAsync(int matchId, LogSessionMatchEventDto dto)
        {
            _logger.LogInformation(
                "Logging session match event for match {MatchId}: {EventType} by Player {PlayerId}, IsHomeSide {IsHomeSide}",
                matchId, dto.EventType, dto.PlayerId, dto.IsHomeSide);

            var match = await _unitOfWork.Repository<MatchEntity>()
                .GetQueryable()
                .FirstOrDefaultAsync(m => m.Id == matchId);

            if (match is null)
                throw new NotFoundException($"Match with Id {matchId} not found");

            if (match.Type != DomainEnums.MatchType.Session)
                throw new BadRequestException("This endpoint is for session matches only.");

            if (match.Status != DomainEnums.MatchStatus.Live)
                throw new BadRequestException("Match events can only be logged while the match is live.");

            var isInLineup = await _unitOfWork.Repository<MatchLineupEntity>()
                .ExistsAsync(ml => ml.MatchId == matchId
                    && ml.PlayerId == dto.PlayerId
                    && ml.IsHomeSide == dto.IsHomeSide);

            if (!isInLineup)
                throw new BadRequestException(
                    $"Player {dto.PlayerId} is not in the {(dto.IsHomeSide ? "home" : "away")} side lineup.");

            var matchEvent = new MatchEventEntity
            {
                MatchId = matchId,
                PlayerId = dto.PlayerId,
                TeamId = match.HomeTeamId,
                AssistPlayerId = dto.AssistPlayerId,
                EventType = dto.EventType,
                Minute = dto.Minute,
                IsHomeSide = dto.IsHomeSide
            };

            await _unitOfWork.Repository<MatchEventEntity>().AddAsync(matchEvent);

            if (IsGoalEvent(dto.EventType))
            {
                if (dto.IsHomeSide)
                    match.HomeScore++;
                else
                    match.AwayScore++;
            }

            if (IsOwnGoalEvent(dto.EventType))
            {
                if (dto.IsHomeSide)
                    match.AwayScore++;
                else
                    match.HomeScore++;
            }

            if (IsPenaltyShootoutEvent(dto.EventType))
            {
                if (!match.HomePenaltyScore.HasValue) match.HomePenaltyScore = 0;
                if (!match.AwayPenaltyScore.HasValue) match.AwayPenaltyScore = 0;

                if (dto.IsHomeSide)
                    match.HomePenaltyScore++;
                else
                    match.AwayPenaltyScore++;
            }

            if (dto.EventType == DomainEnums.MatchEventType.Substitution)
            {
                if (!dto.AssistPlayerId.HasValue)
                    throw new BadRequestException("Substitution event requires an AssistPlayerId representing the incoming player.");

                var subOutLineup = await _unitOfWork.Repository<MatchLineupEntity>()
                    .GetQueryable()
                    .FirstOrDefaultAsync(ml => ml.MatchId == matchId && ml.PlayerId == dto.PlayerId && ml.IsHomeSide == dto.IsHomeSide);

                var subInLineup = await _unitOfWork.Repository<MatchLineupEntity>()
                    .GetQueryable()
                    .FirstOrDefaultAsync(ml => ml.MatchId == matchId && ml.PlayerId == dto.AssistPlayerId.Value && ml.IsHomeSide == dto.IsHomeSide);

                if (subOutLineup != null && subInLineup != null)
                {
                    var outPosition = subOutLineup.PositionInMatch;
                    subOutLineup.IsStarting = false;
                    subOutLineup.PositionInMatch = "SUB";

                    subInLineup.IsStarting = true;
                    if (!string.IsNullOrEmpty(outPosition))
                        subInLineup.PositionInMatch = outPosition;
                }
            }

            await _unitOfWork.SaveChangesAsync();

            var created = await _unitOfWork.Repository<MatchEventEntity>()
                .GetQueryableAsNoTracking()
                .Include(e => e.Team)
                .Include(e => e.Player)
                .Include(e => e.AssistPlayer)
                .FirstOrDefaultAsync(e => e.Id == matchEvent.Id);

            _logger.LogInformation(
                "Session match event logged: {EventType} at minute {Minute} for match {MatchId}",
                dto.EventType, dto.Minute, matchId);

            var responseDto = _mapper.Map<MatchEventResponseDto>(created!);

            await _liveUpdateService.BroadcastMatchEventAsync(new LiveMatchEventUpdateDto
            {
                MatchId = matchId,
                Event = responseDto
            });

            await _liveUpdateService.BroadcastMatchScoreUpdateAsync(new LiveMatchScoreUpdateDto
            {
                MatchId = matchId,
                HomeScore = match.HomeScore,
                AwayScore = match.AwayScore,
                HomePenaltyScore = match.HomePenaltyScore,
                AwayPenaltyScore = match.AwayPenaltyScore,
                Status = match.Status.ToString()
            });

            return responseDto;
        }

        public async Task<MatchTimelineResponseDto> GetMatchTimelineAsync(int matchId)
        {
            var matchExists = await _unitOfWork.Repository<MatchEntity>()
                .ExistsAsync(m => m.Id == matchId);

            if (!matchExists)
                throw new NotFoundException($"Match with Id {matchId} not found");

            var events = await _unitOfWork.Repository<MatchEventEntity>()
                .GetQueryableAsNoTracking()
                .Include(e => e.Team)
                .Include(e => e.Player)
                .Include(e => e.AssistPlayer)
                .Where(e => e.MatchId == matchId)
                .OrderBy(e => e.Minute)
                .ToListAsync();

            return new MatchTimelineResponseDto
            {
                MatchId = matchId,
                Events = _mapper.Map<List<MatchEventResponseDto>>(events)
            };
        }

        public async Task DeleteMatchEventAsync(int matchId, int eventId)
        {
            _logger.LogInformation("Disallowing/Deleting event {EventId} for match {MatchId}", eventId, matchId);

            var match = await _unitOfWork.Repository<MatchEntity>()
                .GetQueryable()
                .FirstOrDefaultAsync(m => m.Id == matchId);

            if (match is null)
                throw new NotFoundException($"Match with Id {matchId} not found");

            if (match.Status != DomainEnums.MatchStatus.Live)
                throw new BadRequestException("Match events can only be disallowed while the match is live.");

            var matchEvent = await _unitOfWork.Repository<MatchEventEntity>()
                .GetQueryable()
                .FirstOrDefaultAsync(e => e.Id == eventId && e.MatchId == matchId);

            if (matchEvent is null)
                throw new NotFoundException($"Event with Id {eventId} for match {matchId} not found");

            bool isHomeSide = matchEvent.IsHomeSide ?? (matchEvent.TeamId == match.HomeTeamId);

            if (IsGoalEvent(matchEvent.EventType))
            {
                if (isHomeSide)
                    match.HomeScore = Math.Max(0, match.HomeScore - 1);
                else
                    match.AwayScore = Math.Max(0, match.AwayScore - 1);
            }
            else if (IsOwnGoalEvent(matchEvent.EventType))
            {
                if (isHomeSide)
                    match.AwayScore = Math.Max(0, match.AwayScore - 1);
                else
                    match.HomeScore = Math.Max(0, match.HomeScore - 1);
            }
            else if (IsPenaltyShootoutEvent(matchEvent.EventType))
            {
                if (isHomeSide)
                {
                    if (match.HomePenaltyScore.HasValue && match.HomePenaltyScore > 0)
                        match.HomePenaltyScore--;
                }
                else
                {
                    if (match.AwayPenaltyScore.HasValue && match.AwayPenaltyScore > 0)
                        match.AwayPenaltyScore--;
                }
            }
            else if (matchEvent.EventType == DomainEnums.MatchEventType.Substitution)
            {
                if (matchEvent.AssistPlayerId.HasValue)
                {
                    var subOutLineup = await _unitOfWork.Repository<MatchLineupEntity>()
                        .GetQueryable()
                        .FirstOrDefaultAsync(ml => ml.MatchId == matchId && ml.PlayerId == matchEvent.PlayerId);

                    var subInLineup = await _unitOfWork.Repository<MatchLineupEntity>()
                        .GetQueryable()
                        .FirstOrDefaultAsync(ml => ml.MatchId == matchId && ml.PlayerId == matchEvent.AssistPlayerId.Value);

                    if (subOutLineup != null && subInLineup != null)
                    {
                        var inPosition = subInLineup.PositionInMatch;
                        subOutLineup.IsStarting = true;
                        if (!string.IsNullOrEmpty(inPosition) && inPosition != "SUB")
                            subOutLineup.PositionInMatch = inPosition;

                        subInLineup.IsStarting = false;
                        subInLineup.PositionInMatch = "SUB";
                    }
                }
            }

            _unitOfWork.Repository<MatchEventEntity>().SoftDelete(matchEvent);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Match event {EventId} deleted successfully for match {MatchId}", eventId, matchId);

            await _liveUpdateService.BroadcastMatchScoreUpdateAsync(new LiveMatchScoreUpdateDto
            {
                MatchId = matchId,
                HomeScore = match.HomeScore,
                AwayScore = match.AwayScore,
                HomePenaltyScore = match.HomePenaltyScore,
                AwayPenaltyScore = match.AwayPenaltyScore,
                Status = match.Status.ToString()
            });

            // Notify all viewers to remove this event from their timeline in real-time
            await _liveUpdateService.BroadcastMatchEventDeletedAsync(new LiveMatchEventDeletedDto
            {
                MatchId = matchId,
                EventId = eventId
            });
        }

        private static bool IsGoalEvent(DomainEnums.MatchEventType eventType)
        {
            return eventType == DomainEnums.MatchEventType.Goal;
        }

        private static bool IsOwnGoalEvent(DomainEnums.MatchEventType eventType)
        {
            return eventType == DomainEnums.MatchEventType.OwnGoal;
        }

        private static bool IsPenaltyShootoutEvent(DomainEnums.MatchEventType eventType)
        {
            return eventType == DomainEnums.MatchEventType.PenaltyScored;
        }
    }
}
