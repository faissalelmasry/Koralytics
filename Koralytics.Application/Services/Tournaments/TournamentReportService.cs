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

            // TODO: trigger AIReportService.GenerateTournamentReportAsync()
            // Add try-catch here when implemented so AI failure doesn't
            // roll back the tournament completion

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

                if (finalRound is null) return null;

                var finalFixture = await _unitOfWork
                    .Repository<TournamentFixtureEntity>()
                    .GetQueryable()
                    .Where(f =>
                        f.RoundId == finalRound.Id &&
                        (f.LegNumber == null || f.LegNumber == 2))
                    .OrderByDescending(f => f.LegNumber)
                    .FirstOrDefaultAsync();

                return finalFixture?.WinnerTeamId;
            }

            if (tournament.Structure == TournamentStructure.League)
            {
                var dummyGroup = await _unitOfWork
                    .Repository<TournamentGroupEntity>()
                    .FindAsync(g =>
                        g.TournamentId == tournamentId &&
                        g.IsDummy == true);

                if (dummyGroup is null) return null;

                var topTeam = await _unitOfWork
                    .Repository<TournamentStandingEntity>()
                    .GetQueryable()
                    .Where(s => s.GroupId == dummyGroup.Id)
                    .OrderByDescending(s => s.Points)
                    .ThenByDescending(s => s.GoalsFor - s.GoalsAgainst)
                    .ThenByDescending(s => s.GoalsFor)
                    .FirstOrDefaultAsync();

                return topTeam?.TournamentTeamId;
            }

            return null;
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

            if (tournamentMatchIds.Count == 0) return;

            var allRatings = await _unitOfWork
                .Repository<MatchPlayerRatingEntity>()
                .GetQueryable()
                .Where(r => tournamentMatchIds.Contains(r.MatchId))
                .ToListAsync();

            if (allRatings.Count == 0) return;

            var hallOfFameRecords = new List<TournamentHallOfFameEntity>();

            // Top Scorer — Goals → Assists → AvgRating
            var topScorer = allRatings
                .GroupBy(r => r.PlayerId)
                .Select(g => new
                {
                    PlayerId = g.Key,
                    Goals = g.Sum(r => r.Goals),
                    Assists = g.Sum(r => r.Assists),
                    AvgRating = g.Average(r => (double)r.Rating)
                })
                .OrderByDescending(x => x.Goals)
                .ThenByDescending(x => x.Assists)
                .ThenByDescending(x => x.AvgRating)
                .FirstOrDefault();

            if (topScorer != null)
                hallOfFameRecords.Add(new TournamentHallOfFameEntity
                {
                    TournamentId = tournamentId,
                    PlayerId = topScorer.PlayerId,
                    AwardType = "TopScorer"
                });

            // Most Assists — Assists → Goals → AvgRating
            var mostAssists = allRatings
                .GroupBy(r => r.PlayerId)
                .Select(g => new
                {
                    PlayerId = g.Key,
                    Assists = g.Sum(r => r.Assists),
                    Goals = g.Sum(r => r.Goals),
                    AvgRating = g.Average(r => (double)r.Rating)
                })
                .OrderByDescending(x => x.Assists)
                .ThenByDescending(x => x.Goals)
                .ThenByDescending(x => x.AvgRating)
                .FirstOrDefault();

            if (mostAssists != null)
                hallOfFameRecords.Add(new TournamentHallOfFameEntity
                {
                    TournamentId = tournamentId,
                    PlayerId = mostAssists.PlayerId,
                    AwardType = "MostAssists"
                });

            // Most MOTM — MOTM count → AvgRating
            var mostMOTM = allRatings
                .Where(r => r.IsMOTM)
                .GroupBy(r => r.PlayerId)
                .Select(g => new
                {
                    PlayerId = g.Key,
                    MOTMCount = g.Count(),
                    AvgRating = g.Average(r => (double)r.Rating)
                })
                .OrderByDescending(x => x.MOTMCount)
                .ThenByDescending(x => x.AvgRating)
                .FirstOrDefault();

            if (mostMOTM != null)
                hallOfFameRecords.Add(new TournamentHallOfFameEntity
                {
                    TournamentId = tournamentId,
                    PlayerId = mostMOTM.PlayerId,
                    AwardType = "MostMOTM"
                });

            // Best Goalkeeper — AvgRating → MinutesPlayed
            var playerIds = allRatings
                .Select(r => r.PlayerId)
                .Distinct()
                .ToList();

            var goalkeeperIds = await _unitOfWork
                .Repository<Domain.Entities.Player.PlayerPosition>()
                .GetQueryable()
                .Where(p =>
                    playerIds.Contains(p.PlayerId) &&
                    p.Position == "GK" &&
                    p.IsPrimary)
                .Select(p => p.PlayerId)
                .ToListAsync();

            if (goalkeeperIds.Count > 0)
            {
                var bestGoalkeeper = allRatings
                    .Where(r => goalkeeperIds.Contains(r.PlayerId))
                    .GroupBy(r => r.PlayerId)
                    .Select(g => new
                    {
                        PlayerId = g.Key,
                        AvgRating = g.Average(r => (double)r.Rating),
                        TotalMinutes = g.Sum(r => r.MinutesPlayed)
                    })
                    .OrderByDescending(x => x.AvgRating)
                    .ThenByDescending(x => x.TotalMinutes)
                    .FirstOrDefault();

                if (bestGoalkeeper != null)
                    hallOfFameRecords.Add(new TournamentHallOfFameEntity
                    {
                        TournamentId = tournamentId,
                        PlayerId = bestGoalkeeper.PlayerId,
                        AwardType = "BestGoalkeeper"
                    });
            }

            // Best Player — AvgRating → Goals → Assists
            var bestPlayer = allRatings
                .GroupBy(r => r.PlayerId)
                .Select(g => new
                {
                    PlayerId = g.Key,
                    AvgRating = g.Average(r => (double)r.Rating),
                    Goals = g.Sum(r => r.Goals),
                    Assists = g.Sum(r => r.Assists)
                })
                .OrderByDescending(x => x.AvgRating)
                .ThenByDescending(x => x.Goals)
                .ThenByDescending(x => x.Assists)
                .FirstOrDefault();

            if (bestPlayer != null)
                hallOfFameRecords.Add(new TournamentHallOfFameEntity
                {
                    TournamentId = tournamentId,
                    PlayerId = bestPlayer.PlayerId,
                    AwardType = "BestPlayer"
                });

            await _unitOfWork.Repository<TournamentHallOfFameEntity>()
                .AddRangeAsync(hallOfFameRecords);

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

            return groups.Select(g => new GroupStandingDto
            {
                GroupId = g.Id,
                GroupName = g.Name,
                Standings = g.TournamentStandings
                    .OrderByDescending(s => s.Points)
                    .ThenByDescending(s => s.GoalsFor - s.GoalsAgainst)
                    .ThenByDescending(s => s.GoalsFor)
                    .Select(s => new StandingRowDto
                    {
                        TournamentTeamId = s.TournamentTeamId,
                        TeamName = s.TournamentTeam.Team.Name,
                        Played = s.Played,
                        Won = s.Won,
                        Drawn = s.Drawn,
                        Lost = s.Lost,
                        GoalsFor = s.GoalsFor,
                        GoalsAgainst = s.GoalsAgainst,
                        GoalDifference = s.GoalsFor - s.GoalsAgainst,
                        Points = s.Points
                    }).ToList(),
                Fixtures = g.TournamentFixtures
                    .Select(MapFixtureToDto)
                    .ToList()
            }).ToList();
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
