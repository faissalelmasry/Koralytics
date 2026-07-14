using AutoMapper;
using Koralytics.Application.DTOs.Match;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Match;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Drill;
using Koralytics.Domain.Entities.Tournamet;
using DomainEnums = Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;
using MatchEntity = Koralytics.Domain.Entities.Match.Match;
using MatchLineupEntity = Koralytics.Domain.Entities.Match.MatchLineup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koralytics.Application.Services.Match
{
    public class MatchService : IMatchService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<MatchService> _logger;

        public MatchService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<MatchService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<MatchResponseDto> CreateFriendlyMatchAsync(CreateFriendlyMatchDto dto)
        {
            _logger.LogInformation(
                "Creating friendly match: HomeTeam {HomeTeamId} vs AwayTeam {AwayTeamId}, Format {Format}",
                dto.HomeTeamId, dto.AwayTeamId, dto.Format);

            if (dto.HomeTeamId == dto.AwayTeamId)
                throw new BadRequestException("Home and Away teams cannot be the same.");

            var homeTeam = await _unitOfWork.Repository<Team>()
                .FindAsNoTrackingAsync(t => t.Id == dto.HomeTeamId);

            if (homeTeam is null)
                throw new NotFoundException($"Home team with Id {dto.HomeTeamId} not found");

            var awayTeam = await _unitOfWork.Repository<Team>()
                .FindAsNoTrackingAsync(t => t.Id == dto.AwayTeamId);

            if (awayTeam is null)
                throw new NotFoundException($"Away team with Id {dto.AwayTeamId} not found");


            var match = _mapper.Map<MatchEntity>(dto);
            match.Type = DomainEnums.MatchType.Friendly;
            match.Status = DomainEnums.MatchStatus.Scheduled;
            match.HomeScore = 0;
            match.AwayScore = 0;

            await _unitOfWork.Repository<MatchEntity>().AddAsync(match);
            await _unitOfWork.SaveChangesAsync();

            var created = await _unitOfWork.Repository<MatchEntity>()
                .GetQueryableAsNoTracking()
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Include(m => m.WinningTeam)
                .FirstOrDefaultAsync(m => m.Id == match.Id);

            _logger.LogInformation(
                "Friendly match created with Id {MatchId}",
                match.Id);

            return _mapper.Map<MatchResponseDto>(created!);
        }

        public async Task<MatchResponseDto> CreateTournamentMatchAsync(CreateTournamentMatchDto dto)
        {
            _logger.LogInformation(
                "Creating tournament match for fixture {FixtureId}: HomeTeam {HomeTeamId} vs AwayTeam {AwayTeamId}",
                dto.TournamentFixtureId, dto.HomeTeamId, dto.AwayTeamId);

            var fixture = await _unitOfWork.Repository<TournamentFixture>()
                .GetQueryable()
                .Include(f => f.Group)
                .Include(f => f.Round)
                .Include(f => f.HomeTeam)
                .Include(f => f.AwayTeam)
                .FirstOrDefaultAsync(f => f.Id == dto.TournamentFixtureId);

            if (fixture is null)
                throw new NotFoundException($"TournamentFixture with Id {dto.TournamentFixtureId} not found");

            if (fixture.MatchId.HasValue)
                throw new BadRequestException("This tournament fixture already has a match assigned.");

            var tournamentId = fixture.Group?.TournamentId
                ?? fixture.Round?.TournamentId
                ?? throw new BadRequestException("TournamentFixture is not associated with a group or round.");

            if (fixture.HomeTeamId != dto.HomeTeamId)
                throw new BadRequestException("HomeTeamId does not match the fixture's home team.");

            if (fixture.AwayTeamId != dto.AwayTeamId)
                throw new BadRequestException("AwayTeamId does not match the fixture's away team.");

            var homeTeam = await _unitOfWork.Repository<Team>()
                .FindAsNoTrackingAsync(t => t.Id == fixture.HomeTeam.TeamId);

            if (homeTeam is null)
                throw new NotFoundException($"Home team with Id {fixture.HomeTeam.TeamId} not found");

            var awayTeam = await _unitOfWork.Repository<Team>()
                .FindAsNoTrackingAsync(t => t.Id == fixture.AwayTeam.TeamId);

            if (awayTeam is null)
                throw new NotFoundException($"Away team with Id {fixture.AwayTeam.TeamId} not found");

            var match = _mapper.Map<MatchEntity>(dto);
            match.Type = DomainEnums.MatchType.Tournament;
            match.Status = DomainEnums.MatchStatus.Scheduled;
            match.TournamentId = tournamentId;
            match.HomeTeamId = homeTeam.Id;
            match.AwayTeamId = awayTeam.Id;
            match.HomeScore = 0;
            match.AwayScore = 0;

            await _unitOfWork.Repository<MatchEntity>().AddAsync(match);
            await _unitOfWork.SaveChangesAsync();

            fixture.MatchId = match.Id;
            fixture.Status = DomainEnums.MatchStatus.Scheduled;
            await _unitOfWork.SaveChangesAsync();

            var created = await _unitOfWork.Repository<MatchEntity>()
                .GetQueryableAsNoTracking()
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Include(m => m.WinningTeam)
                .FirstOrDefaultAsync(m => m.Id == match.Id);

            _logger.LogInformation(
                "Tournament match created with Id {MatchId} for tournament {TournamentId}",
                match.Id, tournamentId);

            return _mapper.Map<MatchResponseDto>(created!);
        }

        public async Task<MatchResponseDto> CreateSessionMatchAsync(CreateSessionMatchDto dto)
        {
            _logger.LogInformation(
                "Creating session match for session {SessionId}: Home {HomeCount} vs Away {AwayCount}",
                dto.SessionId, dto.HomePlayers.Count, dto.AwayPlayers.Count);

            var session = await _unitOfWork.Repository<DrillSession>()
                .GetQueryable()
                .Include(s => s.SessionAttendances)
                .FirstOrDefaultAsync(s => s.Id == dto.SessionId);

            if (session is null)
                throw new NotFoundException($"DrillSession with Id {dto.SessionId} not found");

            var presentPlayerIds = session.SessionAttendances
                .Where(a => a.IsPresent)
                .Select(a => a.playerId)
                .ToHashSet();

            if (presentPlayerIds.Count == 0)
                throw new BadRequestException("Session has no present players.");

            var formatStartingCount = dto.Format switch
            {
                DomainEnums.MatchFormat.FiveSide => 5,
                DomainEnums.MatchFormat.SevenSide => 7,
                DomainEnums.MatchFormat.ElevenSide => 11,
                _ => throw new BadRequestException("Invalid match format.")
            };

            var homeStarting = dto.HomePlayers.Count(p => p.IsStarting);
            if (homeStarting != formatStartingCount)
                throw new BadRequestException(
                    $"Home side must have exactly {formatStartingCount} starting players ({homeStarting} provided).");

            var awayStarting = dto.AwayPlayers.Count(p => p.IsStarting);
            if (awayStarting != formatStartingCount)
                throw new BadRequestException(
                    $"Away side must have exactly {formatStartingCount} starting players ({awayStarting} provided).");

            var allPlayerIds = dto.HomePlayers.Select(p => p.PlayerId)
                .Concat(dto.AwayPlayers.Select(p => p.PlayerId))
                .ToList();

            var duplicatedPlayers = allPlayerIds
                .GroupBy(x => x)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicatedPlayers.Any())
                throw new BadRequestException("Player is found more than one time");

            var missingPlayers = allPlayerIds.Where(id => !presentPlayerIds.Contains(id)).ToList();
            if (missingPlayers.Count != 0)
                throw new BadRequestException(
                    $"Players {string.Join(", ", missingPlayers)} are not present in session {dto.SessionId}.");

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {

                var match = _mapper.Map<MatchEntity>(dto);
                match.HomeTeamId = session.TeamId;
                match.AwayTeamId = session.TeamId;
                match.Type = DomainEnums.MatchType.Session;
                match.Status = DomainEnums.MatchStatus.Live;
                match.HomeScore = 0;
                match.AwayScore = 0;

                await _unitOfWork.Repository<MatchEntity>().AddAsync(match);
                await _unitOfWork.SaveChangesAsync();

                foreach (var player in dto.HomePlayers)
                {
                    var lineup = new MatchLineupEntity
                    {
                        MatchId = match.Id,
                        PlayerId = player.PlayerId,
                        TeamId = session.TeamId,
                        IsStarting = player.IsStarting,
                        JerseyNumber = player.JerseyNumber,
                        IsHomeSide = true
                    };
                    await _unitOfWork.Repository<MatchLineupEntity>().AddAsync(lineup);
                }

                foreach (var player in dto.AwayPlayers)
                {
                    var lineup = new MatchLineupEntity
                    {
                        MatchId = match.Id,
                        PlayerId = player.PlayerId,
                        TeamId = session.TeamId,
                        IsStarting = player.IsStarting,
                        JerseyNumber = player.JerseyNumber,
                        IsHomeSide = false
                    };
                    await _unitOfWork.Repository<MatchLineupEntity>().AddAsync(lineup);
                }

                await _unitOfWork.SaveChangesAsync();

                var created = await _unitOfWork.Repository<MatchEntity>()
                    .GetQueryableAsNoTracking()
                    .Include(m => m.HomeTeam)
                    .Include(m => m.AwayTeam)
                    .Include(m => m.WinningTeam)
                    .FirstOrDefaultAsync(m => m.Id == match.Id);

                _logger.LogInformation(
                    "Session match created with Id {MatchId} for session {SessionId}, status Live",
                    match.Id, dto.SessionId);

                return _mapper.Map<MatchResponseDto>(created!);
            }
            catch
            {
                await transaction.RollbackAsync();
                _logger.LogError("Error occurred while creating session match for session {SessionId}", dto.SessionId);
                throw new BadRequestException("Unable to create the match because of invalid database data."); ;
            }
        }

        public async Task<MatchResponseDto> GetMatchAsync(int matchId)
        {
            var match = await _unitOfWork.Repository<MatchEntity>()
                .GetQueryableAsNoTracking()
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Include(m => m.WinningTeam)
                .FirstOrDefaultAsync(m => m.Id == matchId);

            if (match is null)
                throw new NotFoundException($"Match with Id {matchId} not found");

            return _mapper.Map<MatchResponseDto>(match);
        }
        public async Task CancelMatchAsync(int matchId)
        {
            var match = await _unitOfWork.Repository<MatchEntity>().FindAsync(m => m.Id == matchId);
            if (match is null)
                throw new NotFoundException($"Match with Id {matchId} not found");
            if(match.Status != DomainEnums.MatchStatus.Scheduled)
                throw new NotFoundException($"Can't delete this match,it's done or cancelled");
            match.Status = DomainEnums.MatchStatus.Cancelled;
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task StartMatchAsync(int matchId)
        {
            _logger.LogInformation("Starting match {MatchId}", matchId);

            var match = await _unitOfWork.Repository<MatchEntity>()
                .GetQueryable()
                .FirstOrDefaultAsync(m => m.Id == matchId);

            if (match is null)
                throw new NotFoundException($"Match with Id {matchId} not found");

            if (match.Status != DomainEnums.MatchStatus.Scheduled)
                throw new BadRequestException("Only scheduled matches can be started.");

            match.Status = DomainEnums.MatchStatus.Live;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Match {MatchId} started", matchId);

        }

        public async Task<MatchResponseDto> EndMatchAsync(int matchId)
        {
            _logger.LogInformation("Ending match {MatchId}", matchId);

            var match = await _unitOfWork.Repository<MatchEntity>()
                .GetQueryable()
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Include(m => m.WinningTeam)
                .FirstOrDefaultAsync(m => m.Id == matchId);

            if (match is null)
                throw new NotFoundException($"Match with Id {matchId} not found");

            if (match.Status != DomainEnums.MatchStatus.Live)
                throw new BadRequestException("Only live matches can be ended.");

            match.Status = DomainEnums.MatchStatus.Completed;

            if (match.HomeScore > match.AwayScore)
                match.WinningTeamId = match.HomeTeamId;
            else if (match.AwayScore > match.HomeScore)
                match.WinningTeamId = match.AwayTeamId;
            else if (match.HomePenaltyScore.HasValue && match.AwayPenaltyScore.HasValue)
                match.WinningTeamId = match.HomePenaltyScore > match.AwayPenaltyScore
                    ? match.HomeTeamId
                    : match.HomePenaltyScore < match.AwayPenaltyScore
                        ? match.AwayTeamId
                        : null;
            else
                match.WinningTeamId = null;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Match {MatchId} ended. Score: {Home}-{Away}. Winner: {Winner}",
                matchId, match.HomeScore, match.AwayScore, match.WinningTeamId);

            return _mapper.Map<MatchResponseDto>(match);
        }

        public async Task<FormGuideResponseDto> GetFormGuideAsync(int teamId, DomainEnums.MatchFormat format)
        {
            var team = await _unitOfWork.Repository<Team>()
                .FindAsNoTrackingAsync(t => t.Id == teamId);

            if (team is null)
                throw new NotFoundException($"Team with Id {teamId} not found");

            var matches = await _unitOfWork.Repository<MatchEntity>()
                .GetQueryableAsNoTracking()
                .Where(m => m.Status == DomainEnums.MatchStatus.Completed
                    && m.Format == format
                    && m.Type != DomainEnums.MatchType.Session
                    && (m.HomeTeamId == teamId || m.AwayTeamId == teamId))
                .OrderByDescending(m => m.MatchDate)
                .Take(5)
                .ToListAsync();

            var results = new List<string>();

            foreach (var m in matches)
            {
                if (m.HomeScore == m.AwayScore
                    && (!m.HomePenaltyScore.HasValue || m.HomePenaltyScore == m.AwayPenaltyScore))
                    results.Add("D");
                else if (m.HomeTeamId == teamId
                    ? (m.HomeScore > m.AwayScore
                        || (m.HomeScore == m.AwayScore && m.HomePenaltyScore > m.AwayPenaltyScore))
                    : (m.AwayScore > m.HomeScore
                        || (m.HomeScore == m.AwayScore && m.AwayPenaltyScore > m.HomePenaltyScore)))
                    results.Add("W");
                else
                    results.Add("L");
            }

            return new FormGuideResponseDto
            {
                TeamId = teamId,
                TeamName = team.Name,
                FormFormat = format.ToString(),
                Results = results
            };
        }

        public async Task<MatchListResponseDto> GetMatchesByDateAsync(DateTime date, int page, int pageSize)
        {
            var query = _unitOfWork.Repository<MatchEntity>()
                .GetQueryableAsNoTracking()
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Include(m => m.WinningTeam)
                .Where(m => m.MatchDate >= date.Date && m.MatchDate < date.Date.AddDays(1))
                .OrderByDescending(m => m.MatchDate);

            var totalCount = await query.CountAsync();
            var matches = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new MatchListResponseDto
            {
                Matches = _mapper.Map<List<MatchResponseDto>>(matches),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<MatchListResponseDto> GetTeamMatchesByStatusAsync(int teamId, DomainEnums.MatchStatus? status, int page, int pageSize)
        {
            var team = await _unitOfWork.Repository<Team>()
                .FindAsNoTrackingAsync(t => t.Id == teamId);

            if (team is null)
                throw new NotFoundException($"Team with Id {teamId} not found");

            var query = _unitOfWork.Repository<MatchEntity>()
                .GetQueryableAsNoTracking()
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Include(m => m.WinningTeam)
                .Where(m => m.HomeTeamId == teamId || m.AwayTeamId == teamId);

            if (status.HasValue)
                query = query.Where(m => m.Status == status.Value);

            query = query.OrderByDescending(m => m.MatchDate);

            var totalCount = await query.CountAsync();
            var matches = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new MatchListResponseDto
            {
                Matches = _mapper.Map<List<MatchResponseDto>>(matches),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}
