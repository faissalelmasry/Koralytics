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

        public MatchEventService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<MatchEventService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
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
                if (dto.TeamId == match.HomeTeamId)
                    match.HomePenaltyScore = (match.HomePenaltyScore ?? 0) + 1;
                else
                    match.AwayPenaltyScore = (match.AwayPenaltyScore ?? 0) + 1;
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

            return _mapper.Map<MatchEventResponseDto>(created!);
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
                if (dto.IsHomeSide)
                    match.HomePenaltyScore = (match.HomePenaltyScore ?? 0) + 1;
                else
                    match.AwayPenaltyScore = (match.AwayPenaltyScore ?? 0) + 1;
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

            return _mapper.Map<MatchEventResponseDto>(created!);
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
