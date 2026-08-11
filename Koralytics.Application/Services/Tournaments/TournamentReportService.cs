using Koralytics.Application.DTOs.Tournament;
using Koralytics.Application.DTOs.Tournaments;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Tournament;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MatchPlayerRatingEntity = Koralytics.Domain.Entities.Match.MatchPlayerRating;
using TournamentEntity = Koralytics.Domain.Entities.Tournamet.Tournament;
using TournamentFixtureEntity = Koralytics.Domain.Entities.Tournamet.TournamentFixture;
using TournamentGroupEntity = Koralytics.Domain.Entities.Tournamet.TournamentGroup;
using TournamentHallOfFameEntity = Koralytics.Domain.Entities.Tournamet.TournamentHallOfFame;
using TournamentRoundEntity = Koralytics.Domain.Entities.Tournamet.TournamentRound;
using TournamentStandingEntity = Koralytics.Domain.Entities.Tournamet.TournamentStanding;
using TournamentSquadEntity = Koralytics.Domain.Entities.Tournamet.TournamentSquad;
using TournamentTeamEntity = Koralytics.Domain.Entities.Tournamet.TournamentTeam;

namespace Koralytics.Application.Services.Tournaments
{
    public class TournamentReportService : ITournamentReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TournamentReportService> _logger;

        public TournamentReportService(
            IUnitOfWork unitOfWork,
            ILogger<TournamentReportService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task CompleteTournamentAsync(int tournamentId)
        {
            _logger.LogInformation(
                "Completing tournament {TournamentId}", tournamentId);

            var tournament = await _unitOfWork.Repository<TournamentEntity>()
                .FindAsync(t => t.Id == tournamentId);

            if (tournament is null)
                throw new NotFoundException(
                    $"Tournament with Id {tournamentId} not found");

            // Check not already completed
            if (tournament.Status == TournamentStatus.Completed)
                throw new ConflictException(
                    "Tournament is already completed");

            if (tournament.Status != TournamentStatus.InProgress)
                throw new BadRequestException(
                    "Tournament must be InProgress to complete it");

            // Validate all fixtures completed
            var hasIncomplete = await _unitOfWork
                .Repository<TournamentFixtureEntity>()
                .ExistsAsync(f =>
                    f.Status != MatchStatus.Completed &&
                    (f.Round != null
                        ? f.Round.TournamentId == tournamentId
                        : f.Group != null &&
                          f.Group.TournamentId == tournamentId));

            if (hasIncomplete)
                throw new BadRequestException(
                    "All fixtures must be completed before completing the tournament");

            // Determine winner
            var finalWinnerId = await GetTournamentWinnerAsync(
                tournamentId, tournament);

            if (finalWinnerId is null)
                throw new BadRequestException(
                    "Could not determine tournament winner");

            // Wrap in transaction
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await CreateHallOfFameAsync(tournamentId, finalWinnerId.Value);

                tournament.Status = TournamentStatus.Completed;
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            // Create a pending AI report entry after completion so the background
            // report worker can generate the final summary without blocking.
            var existingReport = await _unitOfWork.Repository<Domain.Entities.AI.AIReport>()
                .FindAsync(r =>
                    r.ReportType == AIReportType.Tournament &&
                    r.ReferenceId == tournamentId);

            if (existingReport is null)
            {
                await _unitOfWork.Repository<Domain.Entities.AI.AIReport>()
                    .AddAsync(new Domain.Entities.AI.AIReport
                    {
                        ReportType = AIReportType.Tournament,
                        ReferenceId = tournamentId,
                        AcademyId = null,
                        ReportText = string.Empty,
                        Status = AIReportStatus.Pending
                    });

                await _unitOfWork.SaveChangesAsync();
            }

            _logger.LogInformation(
                "Tournament {TournamentId} completed successfully", tournamentId);
        }

        public async Task<BracketDto> GetBracketAsync(int tournamentId)
        {
            _logger.LogInformation(
                "Fetching bracket for tournament {TournamentId}", tournamentId);

            var tournament = await _unitOfWork.Repository<TournamentEntity>()
                .GetQueryable()
                .Include(t => t.AgeGroup)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament is null)
                throw new NotFoundException(
                    $"Tournament with Id {tournamentId} not found");

            return new BracketDto
            {
                TournamentId = tournament.Id,
                TournamentName = tournament.Name,
                Status = tournament.Status,
                Groups = await GetGroupStandingsAsync(tournamentId),
                Rounds = await GetRoundsAsync(tournamentId)
            };
        }

        public async Task<List<HallOfFameDto>> GetHallOfFameAsync(int tournamentId)
        {
            var tournamentExists = await _unitOfWork.Repository<TournamentEntity>()
                .ExistsAsync(t => t.Id == tournamentId);

            if (!tournamentExists)
                throw new NotFoundException(
                    $"Tournament with Id {tournamentId} not found");

            await CreateHallOfFameAsync(tournamentId, 0);

            var squads = await _unitOfWork.Repository<TournamentSquadEntity>()
                .GetQueryableAsNoTracking()
                .Include(s => s.Team)
                .Where(s => s.TournamentId == tournamentId)
                .Select(s => new
                {
                    s.PlayerId,
                    TeamName = s.Team.Name
                })
                .ToListAsync();

            var teamByPlayerId = squads
                .GroupBy(s => s.PlayerId)
                .ToDictionary(g => g.Key, g => g.First().TeamName);

            var awards = await _unitOfWork.Repository<TournamentHallOfFameEntity>()
                .GetQueryableAsNoTracking()
                .Include(h => h.Player)
                .Where(h => h.TournamentId == tournamentId)
                .OrderBy(h => h.AwardType)
                .ToListAsync();

            return awards.Select(award => new HallOfFameDto
            {
                PlayerId = award.PlayerId,
                PlayerName = $"{award.Player.FirstName} {award.Player.LastName}",
                AwardType = award.AwardType,
                TeamName = teamByPlayerId.TryGetValue(award.PlayerId, out var teamName)
                    ? teamName
                    : string.Empty
            }).ToList();
        }

        // ─────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────────────────────────

        private async Task<int?> GetTournamentWinnerAsync(
            int tournamentId, TournamentEntity tournament)
        {
            if (tournament.Structure == TournamentStructure.Knockout ||
                tournament.Structure == TournamentStructure.GroupAndKnockout)
            {
                var finalRound = await _unitOfWork
                    .Repository<TournamentRoundEntity>()
                    .GetQueryable()
                    .Where(r => r.TournamentId == tournamentId)
                    .OrderByDescending(r => r.RoundNumber)
                    .FirstOrDefaultAsync();

                if (finalRound != null)
                {
                    var finalFixture = await _unitOfWork
                        .Repository<TournamentFixtureEntity>()
                        .GetQueryable()
                        .Where(f => f.RoundId == finalRound.Id)
                        .OrderByDescending(f => f.LegNumber ?? 0)
                        .FirstOrDefaultAsync();

                    if (finalFixture?.WinnerTeamId != null)
                        return finalFixture.WinnerTeamId;
                }
            }

            // Fallback for GroupAndKnockout without final winner, League, or tied standings:
            // Find top team from group standings
            var topStanding = await _unitOfWork
                .Repository<TournamentStandingEntity>()
                .GetQueryable()
                .Where(s => s.Group.TournamentId == tournamentId)
                .OrderByDescending(s => s.Points)
                .ThenByDescending(s => s.GoalsFor - s.GoalsAgainst)
                .ThenByDescending(s => s.GoalsFor)
                .FirstOrDefaultAsync();

            if (topStanding != null)
                return topStanding.TournamentTeamId;

            // Ultimate fallback: first accepted/registered team in the tournament
            var firstTeam = await _unitOfWork
                .Repository<TournamentTeamEntity>()
                .GetQueryable()
                .FirstOrDefaultAsync(tt => tt.TournamentId == tournamentId);

            return firstTeam?.Id;
        }

        private async Task CreateHallOfFameAsync(
            int tournamentId, int winnerTournamentTeamId)
        {
            var tournamentMatchIds = await _unitOfWork
                .Repository<TournamentFixtureEntity>()
                .GetQueryable()
                .Where(f =>
                    f.MatchId != null &&
                    (f.Round != null
                        ? f.Round.TournamentId == tournamentId
                        : f.Group != null &&
                          f.Group.TournamentId == tournamentId))
                .Select(f => f.MatchId!.Value)
                .ToListAsync();

            var allRatings = new List<MatchPlayerRatingEntity>();
            if (tournamentMatchIds.Count > 0)
            {
                allRatings = await _unitOfWork
                    .Repository<MatchPlayerRatingEntity>()
                    .GetQueryable()
                    .Where(r => tournamentMatchIds.Contains(r.MatchId))
                    .ToListAsync();
            }

            var goalEvents = new List<Koralytics.Domain.Entities.Match.MatchEvent>();
            if (tournamentMatchIds.Count > 0)
            {
                goalEvents = await _unitOfWork
                    .Repository<Koralytics.Domain.Entities.Match.MatchEvent>()
                    .GetQueryable()
                    .Where(e => tournamentMatchIds.Contains(e.MatchId) && e.EventType == MatchEventType.Goal)
                    .ToListAsync();
            }

            var squadPlayerIds = await _unitOfWork.Repository<TournamentSquadEntity>()
                .GetQueryableAsNoTracking()
                .Where(s => s.TournamentId == tournamentId)
                .Select(s => s.PlayerId)
                .Distinct()
                .ToListAsync();

            var participatingTeamIds = await _unitOfWork.Repository<TournamentTeamEntity>()
                .GetQueryableAsNoTracking()
                .Where(tt => tt.TournamentId == tournamentId)
                .Select(tt => tt.TeamId)
                .ToListAsync();

            var teamPlayerIds = new List<int>();
            if (participatingTeamIds.Count > 0)
            {
                teamPlayerIds = await _unitOfWork.Repository<Koralytics.Domain.Entities.Player.PlayerTeam>()
                    .GetQueryableAsNoTracking()
                    .Where(pt => participatingTeamIds.Contains(pt.TeamId) && pt.LeftAt == null)
                    .Select(pt => pt.PlayerId)
                    .Distinct()
                    .ToListAsync();
            }

            var candidateIds = squadPlayerIds
                .Concat(allRatings.Select(r => r.PlayerId))
                .Concat(teamPlayerIds)
                .Distinct()
                .ToList();

            if (candidateIds.Count == 0) return;

            // Aggregate Player Stats
            var playerStats = candidateIds.Select(pId => new
            {
                PlayerId = pId,
                Goals = goalEvents.Count(e => e.PlayerId == pId) + allRatings.Where(r => r.PlayerId == pId).Sum(r => r.Goals),
                Assists = goalEvents.Count(e => e.AssistPlayerId == pId) + allRatings.Where(r => r.PlayerId == pId).Sum(r => r.Assists),
                MOTMCount = allRatings.Count(r => r.PlayerId == pId && r.IsMOTM),
                AvgRating = allRatings.Where(r => r.PlayerId == pId).Select(r => (double)r.Rating).DefaultIfEmpty(7.5).Average(),
                Minutes = allRatings.Where(r => r.PlayerId == pId).Sum(r => r.MinutesPlayed)
            }).ToList();

            var desiredAwards = new List<(int PlayerId, string AwardType)>();

            // Top Scorer
            var topScorer = playerStats
                .OrderByDescending(x => x.Goals)
                .ThenByDescending(x => x.Assists)
                .ThenByDescending(x => x.AvgRating)
                .FirstOrDefault();

            if (topScorer != null)
                desiredAwards.Add((topScorer.PlayerId, "TopScorer"));

            // Most Assists
            var mostAssists = playerStats
                .OrderByDescending(x => x.Assists)
                .ThenByDescending(x => x.Goals)
                .ThenByDescending(x => x.AvgRating)
                .FirstOrDefault();

            if (mostAssists != null && !desiredAwards.Any(r => r.AwardType == "MostAssists"))
                desiredAwards.Add((mostAssists.PlayerId, "MostAssists"));

            // Most MOTM
            var mostMOTM = playerStats
                .OrderByDescending(x => x.MOTMCount)
                .ThenByDescending(x => x.AvgRating)
                .FirstOrDefault();

            if (mostMOTM != null && !desiredAwards.Any(r => r.AwardType == "MostMOTM"))
                desiredAwards.Add((mostMOTM.PlayerId, "MostMOTM"));

            // Best Goalkeeper
            var goalkeeperIds = await _unitOfWork
                .Repository<Domain.Entities.Player.PlayerPosition>()
                .GetQueryable()
                .Where(p => candidateIds.Contains(p.PlayerId) && p.Position == "GK" && p.IsPrimary)
                .Select(p => p.PlayerId)
                .ToListAsync();

            var bestGkCandidate = goalkeeperIds.Count > 0
                ? playerStats.Where(x => goalkeeperIds.Contains(x.PlayerId)).OrderByDescending(x => x.AvgRating).ThenByDescending(x => x.Minutes).Select(x => x.PlayerId).FirstOrDefault()
                : candidateIds.FirstOrDefault();

            if (bestGkCandidate > 0 && !desiredAwards.Any(r => r.AwardType == "BestGoalkeeper"))
                desiredAwards.Add((bestGkCandidate, "BestGoalkeeper"));

            // Best Player
            var bestPlayer = playerStats
                .OrderByDescending(x => x.AvgRating)
                .ThenByDescending(x => x.Goals)
                .ThenByDescending(x => x.Assists)
                .FirstOrDefault();

            if (bestPlayer != null && !desiredAwards.Any(r => r.AwardType == "BestPlayer"))
                desiredAwards.Add((bestPlayer.PlayerId, "BestPlayer"));

            // Load ALL existing records for this tournament (including soft-deleted ones)
            var existingRecords = await _unitOfWork.Repository<TournamentHallOfFameEntity>()
                .GetQueryable()
                .IgnoreQueryFilters()
                .Where(h => h.TournamentId == tournamentId)
                .ToListAsync();

            var desiredSet = desiredAwards.ToHashSet();

            // 1. Soft-delete records no longer in desired set
            foreach (var record in existingRecords)
            {
                if (!desiredSet.Contains((record.PlayerId, record.AwardType)))
                {
                    record.IsDeleted = true;
                }
            }

            // 2. Insert or un-delete desired records
            foreach (var (pId, awardType) in desiredAwards)
            {
                var existing = existingRecords.FirstOrDefault(r => r.PlayerId == pId && r.AwardType == awardType);
                if (existing != null)
                {
                    existing.IsDeleted = false;
                }
                else
                {
                    await _unitOfWork.Repository<TournamentHallOfFameEntity>().AddAsync(new TournamentHallOfFameEntity
                    {
                        TournamentId = tournamentId,
                        PlayerId = pId,
                        AwardType = awardType
                    });
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }

        private async Task<List<GroupStandingDto>> GetGroupStandingsAsync(
            int tournamentId)
        {
            var groups = await _unitOfWork.Repository<TournamentGroupEntity>()
                .GetQueryable()
                .Include(g => g.TournamentStandings)
                    .ThenInclude(s => s.TournamentTeam)
                        .ThenInclude(tt => tt.Team)
                .Include(g => g.TournamentFixtures)
                    .ThenInclude(f => f.HomeTeam)
                        .ThenInclude(tt => tt.Team)
                .Include(g => g.TournamentFixtures)
                    .ThenInclude(f => f.AwayTeam)
                        .ThenInclude(tt => tt.Team)
                .Include(g => g.TournamentFixtures)
                    .ThenInclude(f => f.WinnerTeam!)
                        .ThenInclude(tt => tt.Team)
                .Where(g => g.TournamentId == tournamentId)
                .ToListAsync();

            bool needSave = false;

            var result = groups.Select(g =>
            {
                var completedFixtures = g.TournamentFixtures
                    .Where(f => f.Status == MatchStatus.Completed || (f.HomeScore.HasValue && f.AwayScore.HasValue))
                    .ToList();

                var teamRows = new Dictionary<int, StandingRowDto>();
                foreach (var s in g.TournamentStandings)
                {
                    teamRows[s.TournamentTeamId] = new StandingRowDto
                    {
                        TournamentTeamId = s.TournamentTeamId,
                        TeamName = s.TournamentTeam?.Team?.Name ?? string.Empty,
                        Played = 0,
                        Won = 0,
                        Drawn = 0,
                        Lost = 0,
                        GoalsFor = 0,
                        GoalsAgainst = 0,
                        GoalDifference = 0,
                        Points = 0
                    };
                }

                foreach (var f in completedFixtures)
                {
                    int hScore = f.HomeScore ?? 0;
                    int aScore = f.AwayScore ?? 0;

                    if (teamRows.TryGetValue(f.HomeTeamId, out var homeRow))
                    {
                        homeRow.Played++;
                        homeRow.GoalsFor += hScore;
                        homeRow.GoalsAgainst += aScore;
                        if (hScore > aScore) { homeRow.Won++; homeRow.Points += 3; }
                        else if (hScore < aScore) { homeRow.Lost++; }
                        else { homeRow.Drawn++; homeRow.Points += 1; }
                    }

                    if (teamRows.TryGetValue(f.AwayTeamId, out var awayRow))
                    {
                        awayRow.Played++;
                        awayRow.GoalsFor += aScore;
                        awayRow.GoalsAgainst += hScore;
                        if (aScore > hScore) { awayRow.Won++; awayRow.Points += 3; }
                        else if (aScore < hScore) { awayRow.Lost++; }
                        else { awayRow.Drawn++; awayRow.Points += 1; }
                    }
                }

                foreach (var s in g.TournamentStandings)
                {
                    if (teamRows.TryGetValue(s.TournamentTeamId, out var calc))
                    {
                        if (s.Played != calc.Played || s.Points != calc.Points || s.GoalsFor != calc.GoalsFor)
                        {
                            s.Played = calc.Played;
                            s.Won = calc.Won;
                            s.Drawn = calc.Drawn;
                            s.Lost = calc.Lost;
                            s.GoalsFor = calc.GoalsFor;
                            s.GoalsAgainst = calc.GoalsAgainst;
                            s.Points = calc.Points;
                            needSave = true;
                        }
                    }
                }

                var sortedRows = teamRows.Values
                    .Select(r => { r.GoalDifference = r.GoalsFor - r.GoalsAgainst; return r; })
                    .OrderByDescending(s => s.Points)
                    .ThenByDescending(s => s.GoalDifference)
                    .ThenByDescending(s => s.GoalsFor)
                    .ToList();

                return new GroupStandingDto
                {
                    GroupId = g.Id,
                    GroupName = g.Name,
                    Standings = sortedRows,
                    Fixtures = g.TournamentFixtures
                        .Select(MapFixtureToDto)
                        .ToList()
                };
            }).ToList();

            if (needSave)
            {
                await _unitOfWork.SaveChangesAsync();
            }

            return result;
        }

        private async Task<List<RoundDto>> GetRoundsAsync(int tournamentId)
        {
            var rounds = await _unitOfWork.Repository<TournamentRoundEntity>()
                .GetQueryable()
                .Include(r => r.TournamentFixtures)
                    .ThenInclude(f => f.HomeTeam)
                        .ThenInclude(tt => tt.Team)
                .Include(r => r.TournamentFixtures)
                    .ThenInclude(f => f.AwayTeam)
                        .ThenInclude(tt => tt.Team)
                .Include(r => r.TournamentFixtures)
                    .ThenInclude(f => f.WinnerTeam!)
                        .ThenInclude(tt => tt.Team)
                .Where(r => r.TournamentId == tournamentId)
                .OrderBy(r => r.RoundNumber)
                .ToListAsync();

            return rounds.Select(r => new RoundDto
            {
                RoundId = r.Id,
                RoundName = r.Name,
                RoundNumber = r.RoundNumber,
                Fixtures = r.TournamentFixtures
                    .Select(MapFixtureToDto)
                    .ToList()
            }).ToList();
        }

        private FixtureDto MapFixtureToDto(TournamentFixtureEntity f) => new()
        {
            FixtureId = f.Id,
            MatchId = f.MatchId,
            HomeTeamId = f.HomeTeamId,
            AwayTeamId = f.AwayTeamId,
            HomeRealTeamId = f.HomeTeam?.TeamId ?? f.HomeTeamId,
            AwayRealTeamId = f.AwayTeam?.TeamId ?? f.AwayTeamId,
            HomeTeamName = f.HomeTeam.Team.Name,
            AwayTeamName = f.AwayTeam.Team.Name,
            HomeScore = f.HomeScore,
            AwayScore = f.AwayScore,
            WinnerTeamName = f.WinnerTeam?.Team.Name,
            Status = f.Status,
            LegNumber = f.LegNumber
        };
    }
}
