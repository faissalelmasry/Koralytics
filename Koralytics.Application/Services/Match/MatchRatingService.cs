using AutoMapper;
using Koralytics.Application.DTOs.Match;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Match;
using Koralytics.Application.Services.Player.Helpers;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Coach;
using Koralytics.Domain.Entities.Drill;
using Koralytics.Domain.Entities.Match;
using Koralytics.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using PlayerEntity = Koralytics.Domain.Entities.Player.Player;
using DomainEnums = Koralytics.Domain.Enums;
using MatchEntity = Koralytics.Domain.Entities.Match.Match;
using Microsoft.Extensions.DependencyInjection;

namespace Koralytics.Application.Services.Match
{
    public class MatchRatingService : IMatchRatingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<MatchRatingService> _logger;
        private readonly ICardInvalidationList _invalidationList;
        private readonly IMatchReportService _matchReportService;
        private readonly IBackgroundTaskQueue _taskQueue;

        public MatchRatingService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<MatchRatingService> logger,
            ICardInvalidationList invalidationList,
            IMatchReportService matchReportService,
            IBackgroundTaskQueue taskQueue

            )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _invalidationList = invalidationList;            _matchReportService = matchReportService;
            _taskQueue = taskQueue;

        }

        public async Task SubmitLineupAsync(int matchId, int coachId, SubmitLineupDto dto)
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

            var existingEntries = await _unitOfWork.Repository<MatchLineup>()
                .GetQueryable()
                .Where(ml => ml.MatchId == matchId && ml.TeamId == coachTeamId)
                .ToListAsync();

            if (existingEntries.Count != 0)
                _unitOfWork.Repository<MatchLineup>().SoftDeleteRange(existingEntries);

            var playerIds = dto.Players.Select(p => p.PlayerId).Distinct().ToList();

            var existingPlayerIds = await _unitOfWork.Repository<PlayerEntity>()
                .GetQueryableAsNoTracking()
                .Where(p => playerIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync();

            var existingPlayerIdSet = existingPlayerIds.ToHashSet();

            var missingIds = playerIds.Where(id => !existingPlayerIdSet.Contains(id)).ToList();
            if (missingIds.Count > 0)
                throw new NotFoundException($"Players not found: {string.Join(", ", missingIds)}");

            foreach (var player in dto.Players)
            {
                var lineup = _mapper.Map<MatchLineup>(player);
                lineup.MatchId = matchId;
                await _unitOfWork.Repository<MatchLineup>().AddAsync(lineup);
            }

            if (coachTeamId == match.HomeTeamId)
            {
                match.Formation = dto.Formation;
            }
            else if (coachTeamId == match.AwayTeamId)
            {
                match.AwayFormation = dto.Formation;
            }
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Lineup submitted for match {MatchId} by coach {CoachId}: {Count} players",
                matchId, coachId, startingCount);
        }

        public async Task<List<LineupResponseDto>> GetLineupAsync(int matchId)
        {
            var lineup = await _unitOfWork.Repository<MatchLineup>()
                .GetQueryableAsNoTracking()
                .Include(ml => ml.Player)
                .Where(ml => ml.MatchId == matchId)
                .ToListAsync();

            if (lineup.Count == 0)
            {
                var matchExists = await _unitOfWork.Repository<MatchEntity>()
                    .ExistsAsync(m => m.Id == matchId);

                if (!matchExists)
                    throw new NotFoundException($"Match with Id {matchId} not found");
            }

            return _mapper.Map<List<LineupResponseDto>>(lineup);
        }

        public async Task SubmitMatchRatingsAsync(int matchId, int coachId, SubmitMatchRatingsDto dto)
        {
            _logger.LogInformation("Coach {CoachId} submitting ratings for match {MatchId} with {Count} players",
                coachId, matchId, dto.Ratings.Count);

            var duplicatePlayerIds = dto.Ratings
                .GroupBy(r => r.PlayerId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicatePlayerIds.Count > 0)
                throw new BadRequestException(
                    $"Players rated more than once: {string.Join(", ", duplicatePlayerIds)}");

            foreach (var rating in dto.Ratings)
            {
                var distinctCategoryCount = rating.CategoryRatings
                    .Select(c => c.DrillCategoryId)
                    .Distinct()
                    .Count();

                if (distinctCategoryCount != rating.CategoryRatings.Count)
                    throw new BadRequestException(
                        $"Player {rating.PlayerId} has duplicate DrillCategoryIds.");
            }

            var match = await _unitOfWork.Repository<MatchEntity>()
                .FindAsync(m => m.Id == matchId);

            if (match is null)
                throw new NotFoundException($"Match with Id {matchId} not found");

            if (match.Status != DomainEnums.MatchStatus.Completed)
                throw new BadRequestException("Ratings can only be submitted after the match has ended.");

            var lineups = await _unitOfWork.Repository<MatchLineup>()
                .GetQueryableAsNoTracking()
                .Where(ml => ml.MatchId == matchId)
                .ToListAsync();

            var eligiblePlayerIds = await GetEligiblePlayerIdsAsync(
                match.Type, match.HomeTeamId, match.AwayTeamId, lineups, coachId);

            if (dto.Ratings.Any(r => !eligiblePlayerIds.Contains(r.PlayerId)))
                throw new BadRequestException("Coach can only rate players from their own team.");

            var motmCount = dto.Ratings.Count(r => r.IsMOTM);
            if (motmCount != 1)
                throw new BadRequestException("Exactly one MOTM must be selected.");

            foreach (var rating in dto.Ratings)
            {
                var isInLineup = lineups.Any(ml => ml.PlayerId == rating.PlayerId);
                if (!isInLineup)
                    throw new BadRequestException($"Player {rating.PlayerId} is not in the lineup.");
            }

            var matchEvents = await _unitOfWork.Repository<MatchEvent>()
                .GetQueryableAsNoTracking()
                .Where(e => e.MatchId == matchId)
                .ToListAsync();

            var playerGoals = matchEvents
                .Where(e => IsGoalEvent(e.EventType))
                .GroupBy(e => e.PlayerId)
                .ToDictionary(g => g.Key, g => g.Count());

            var playerAssists = matchEvents
                .Where(e => IsGoalEvent(e.EventType) && e.AssistPlayerId.HasValue)
                .GroupBy(e => e.AssistPlayerId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            var allCategoryIds = dto.Ratings
                .SelectMany(r => r.CategoryRatings)
                .Select(c => c.DrillCategoryId)
                .Distinct()
                .ToList();

            var existingCategoryIds = await _unitOfWork.Repository<DrillCategory>()
                .GetQueryableAsNoTracking()
                .Where(dc => allCategoryIds.Contains(dc.Id))
                .Select(dc => dc.Id)
                .ToListAsync();

            var existingCategoryIdSet = existingCategoryIds.ToHashSet();
            var missingCategories = allCategoryIds
                .Where(id => !existingCategoryIdSet.Contains(id))
                .ToList();

            if (missingCategories.Count > 0)
                throw new NotFoundException(
                    $"DrillCategories not found: {string.Join(", ", missingCategories)}");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var rating in dto.Ratings)
                {
                    var matchPlayerRating = new MatchPlayerRating
                    {
                        MatchId = matchId,
                        PlayerId = rating.PlayerId,
                        CoachId = coachId,
                        Goals = playerGoals.GetValueOrDefault(rating.PlayerId, 0),
                        Assists = playerAssists.GetValueOrDefault(rating.PlayerId, 0),
                        MinutesPlayed = rating.MinutesPlayed,
                        IsMOTM = rating.IsMOTM,
                        CoachNote = rating.CoachNote,
                        CategoryRatings = rating.CategoryRatings.Select(c => new MatchPlayerCategoryRating
                        {
                            DrillCategoryId = c.DrillCategoryId,
                            Rating = c.Rating
                        }).ToList()
                    };

                    await _unitOfWork.Repository<MatchPlayerRating>().AddAsync(matchPlayerRating);
                    _invalidationList.Invalidate(rating.PlayerId);
                }

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            _logger.LogInformation(
                "Ratings submitted for match {MatchId} by coach {CoachId}",
                matchId, coachId);

            bool shouldQueueReport = true;
            if (match.Type == DomainEnums.MatchType.Friendly)
            {
                var ratedPlayerIds = await _unitOfWork.Repository<MatchPlayerRating>()
                    .GetQueryableAsNoTracking()
                    .Where(r => r.MatchId == matchId)
                    .Select(r => r.PlayerId)
                    .Distinct()
                    .ToListAsync();

                var homePlayerIds = lineups.Where(ml => ml.TeamId == match.HomeTeamId).Select(ml => ml.PlayerId).ToHashSet();
                var awayPlayerIds = lineups.Where(ml => ml.TeamId == match.AwayTeamId).Select(ml => ml.PlayerId).ToHashSet();

                bool hasHomeRating = ratedPlayerIds.Any(id => homePlayerIds.Contains(id));
                bool hasAwayRating = ratedPlayerIds.Any(id => awayPlayerIds.Contains(id));

                if (!hasHomeRating || !hasAwayRating)
                {
                    shouldQueueReport = false;
                    _logger.LogInformation(
                        "Skipping AI match report generation for friendly match {MatchId} because not all teams have player ratings yet (Home: {HasHome}, Away: {HasAway}).",
                        matchId, hasHomeRating, hasAwayRating);
                }
            }

            if (shouldQueueReport)
            {
                _logger.LogInformation("Queuing AI match report generation for match {MatchId}...", matchId);

                _taskQueue.QueueBackgroundWorkItem(async (serviceProvider, cancellationToken) =>
                {
                    var reportService = serviceProvider.GetRequiredService<IMatchReportService>();
                    var logger = serviceProvider.GetRequiredService<ILogger<MatchRatingService>>();

                    try
                    {
                        logger.LogInformation("Generating AI match report for match {MatchId}...", matchId);
                        await reportService.GenerateMatchReportAsync(matchId);
                        logger.LogInformation("AI match report generated successfully for match {MatchId}.", matchId);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to generate AI match report for match {MatchId}", matchId);
                    }
                });
            }
        }

        public async Task<MatchRatingsResponseDto> GetMatchRatingsAsync(int matchId)
        {
            _logger.LogInformation("Fetching ratings for match {MatchId}", matchId);

            var ratings = await _unitOfWork.Repository<MatchPlayerRating>()
                .GetQueryableAsNoTracking()
                .Include(r => r.CategoryRatings)
                    .ThenInclude(cr => cr.DrillCategory)
                .Include(r => r.Player)
                .Include(r => r.Coach)
                .Where(r => r.MatchId == matchId)
                .ToListAsync();

            if (ratings.Count == 0)
            {
                var matchExists = await _unitOfWork.Repository<MatchEntity>()
                    .ExistsAsync(m => m.Id == matchId);

                if (!matchExists)
                    throw new NotFoundException($"Match with Id {matchId} not found");
            }

            var ratingDtos = ratings.Select(r =>
            {
                var dto = _mapper.Map<MatchPlayerRatingDto>(r);
                dto.PlayerName = $"{r.Player.FirstName} {r.Player.LastName}";
                dto.CoachName = $"{r.Coach.FirstName} {r.Coach.LastName}";
                dto.CategoryRatings = r.CategoryRatings
                    .Select(cr => new CategoryRatingDto
                    {
                        DrillCategoryId = cr.DrillCategoryId,
                        CategoryName = cr.DrillCategory != null ? cr.DrillCategory.Name : "Unknown",
                        Rating = cr.Rating
                    })
                    .ToList();
                return dto;
            }).ToList();

            _logger.LogInformation("Retrieved {Count} ratings for match {MatchId}",
                ratingDtos.Count, matchId);

            return new MatchRatingsResponseDto
            {
                MatchId = matchId,
                Ratings = ratingDtos
            };
        }

        public async Task<AIReportResponseDto> GetMatchReportAsync(int matchId)
        {
            return await _matchReportService.GetMatchReportAsync(matchId, DomainEnums.AIReportType.Match);
        }

        private async Task<HashSet<int>> GetEligiblePlayerIdsAsync(
            DomainEnums.MatchType matchType,
            int homeTeamId,
            int awayTeamId,
            List<MatchLineup> lineups,
            int coachId)
        {
            if (matchType == DomainEnums.MatchType.Tournament)
                return lineups.Select(ml => ml.PlayerId).ToHashSet();

            var coachTeam = await _unitOfWork.Repository<CoachTeam>()
                .GetQueryableAsNoTracking()
                .FirstOrDefaultAsync(ct => ct.CoachUserId == coachId
                    && ct.RemovedAt == null
                    && (ct.TeamId == homeTeamId || ct.TeamId == awayTeamId));

            if (coachTeam is null)
                throw new ForbiddenException(
                    $"Coach {coachId} is not assigned to either team of this match.");

            if (matchType == DomainEnums.MatchType.Session)
                return lineups.Select(ml => ml.PlayerId).ToHashSet();

            return lineups
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
