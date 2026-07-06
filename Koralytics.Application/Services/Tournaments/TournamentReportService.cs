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
using TournamentGroupTeamEntity = Koralytics.Domain.Entities.Tournamet.TournamentGroupTeam;
using TournamentHallOfFameEntity = Koralytics.Domain.Entities.Tournamet.TournamentHallOfFame;
using TournamentRoundEntity = Koralytics.Domain.Entities.Tournamet.TournamentRound;
using TournamentStandingEntity = Koralytics.Domain.Entities.Tournamet.TournamentStanding;

namespace Koralytics.Application.Services.Tournament
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

            // Validate tournament exists and is in progress
            var tournament = await _unitOfWork.Repository<TournamentEntity>()
                .FindAsync(t => t.Id == tournamentId);

            if (tournament is null)
                throw new NotFoundException(
                    $"Tournament with Id {tournamentId} not found");

            if (tournament.Status != TournamentStatus.InProgress)
                throw new BadRequestException(
                    "Tournament must be InProgress to complete it");

            // Validate all fixtures are completed
            var hasIncomplete = await _unitOfWork.Repository<TournamentFixtureEntity>()
                .ExistsAsync(f =>
                    f.Status != MatchStatus.Completed &&
                    (f.Round != null
                        ? f.Round.TournamentId == tournamentId
                        : f.Group != null && f.Group.TournamentId == tournamentId));

            if (hasIncomplete)
                throw new BadRequestException(
                    "All fixtures must be completed before completing the tournament");

            // Determine tournament winner from final fixture
            var finalWinnerId = await GetTournamentWinnerAsync(tournamentId, tournament);

            if (finalWinnerId is null)
                throw new BadRequestException(
                    "Could not determine tournament winner. " +
                    "Ensure final fixture has a winner");

            // Build Hall of Fame records
            await CreateHallOfFameAsync(tournamentId, finalWinnerId.Value);

            // Mark tournament as completed
            tournament.Status = TournamentStatus.Completed;
            await _unitOfWork.SaveChangesAsync();

            // TODO: trigger AIReportService.GenerateTournamentReportAsync(tournamentId)
            // once Faissal's AI service is implemented

            _logger.LogInformation(
                "Tournament {TournamentId} completed successfully", tournamentId);
        }

  
        public async Task<BracketDto> GetBracketAsync(int tournamentId)
        {
            _logger.LogInformation(
                "Fetching bracket for tournament {TournamentId}", tournamentId);

            // Validate tournament exists
            var tournament = await _unitOfWork.Repository<TournamentEntity>()
                .GetQueryable()
                .Include(t => t.AgeGroup)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament is null)
                throw new NotFoundException(
                    $"Tournament with Id {tournamentId} not found");

            var bracketDto = new BracketDto
            {
                TournamentId = tournament.Id,
                TournamentName = tournament.Name,
                Status = tournament.Status
            };

            // Fetch group stage data
            bracketDto.Groups = await GetGroupStandingsAsync(tournamentId);

            // Fetch knockout rounds data
            bracketDto.Rounds = await GetRoundsAsync(tournamentId);

            return bracketDto;
        }

        private async Task<int?> GetTournamentWinnerAsync(
            int tournamentId,
            TournamentEntity tournament)
        {
            // For Knockout and GroupAndKnockout — winner is from final round fixture
            if (tournament.Structure == TournamentStructure.Knockout ||
                tournament.Structure == TournamentStructure.GroupAndKnockout)
            {
                // Get the last round (highest round number)
                var finalRound = await _unitOfWork.Repository<TournamentRoundEntity>()
                    .GetQueryable()
                    .Where(r => r.TournamentId == tournamentId)
                    .OrderByDescending(r => r.RoundNumber)
                    .FirstOrDefaultAsync();

                if (finalRound is null)
                    return null;

                // Get the final fixture winner
                var finalFixture = await _unitOfWork.Repository<TournamentFixtureEntity>()
                    .GetQueryable()
                    .Where(f =>
                        f.RoundId == finalRound.Id &&
                        (f.LegNumber == null || f.LegNumber == 2))
                    .OrderByDescending(f => f.LegNumber)
                    .FirstOrDefaultAsync();

                return finalFixture?.WinnerTeamId;
            }

            // For League — winner is team with most points in dummy group
            if (tournament.Structure == TournamentStructure.League)
            {
                var dummyGroup = await _unitOfWork.Repository<TournamentGroupEntity>()
                    .FindAsync(g =>
                        g.TournamentId == tournamentId &&
                        g.IsDummy == true);

                if (dummyGroup is null)
                    return null;

                var topTeam = await _unitOfWork.Repository<TournamentStandingEntity>()
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
            // Fetch all matches in this tournament
            var tournamentMatchIds = await _unitOfWork
                .Repository<TournamentFixtureEntity>()
                .GetQueryable()
                .Where(f =>
                    f.MatchId != null &&
                    (f.Round != null
                        ? f.Round.TournamentId == tournamentId
                        : f.Group != null && f.Group.TournamentId == tournamentId))
                .Select(f => f.MatchId!.Value)
                .ToListAsync();

            if (tournamentMatchIds.Count == 0)
                return;

            // Fetch all player ratings for tournament matches
            var allRatings = await _unitOfWork.Repository<MatchPlayerRatingEntity>()
                .GetQueryable()
                .Where(r => tournamentMatchIds.Contains(r.MatchId))
                .ToListAsync();

            if (allRatings.Count == 0)
                return;

            var hallOfFameRecords = new List<TournamentHallOfFameEntity>();

            // Top Scorer — most goals
            var topScorer = allRatings
                .GroupBy(r => r.PlayerId)
                .OrderByDescending(g => g.Sum(r => r.Goals))
                .FirstOrDefault();

            if (topScorer != null)
            {
                hallOfFameRecords.Add(new TournamentHallOfFameEntity
                {
                    TournamentId = tournamentId,
                    PlayerId = topScorer.Key,
                    AwardType = "TopScorer"
                });
            }

            // Most Assists
            var mostAssists = allRatings
                .GroupBy(r => r.PlayerId)
                .OrderByDescending(g => g.Sum(r => r.Assists))
                .FirstOrDefault();

            if (mostAssists != null)
            {
                hallOfFameRecords.Add(new TournamentHallOfFameEntity
                {
                    TournamentId = tournamentId,
                    PlayerId = mostAssists.Key,
                    AwardType = "MostAssists"
                });
            }

            // Most MOTM
            var mostMOTM = allRatings
                .Where(r => r.IsMOTM)
                .GroupBy(r => r.PlayerId)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            if (mostMOTM != null)
            {
                hallOfFameRecords.Add(new TournamentHallOfFameEntity
                {
                    TournamentId = tournamentId,
                    PlayerId = mostMOTM.Key,
                    AwardType = "MostMOTM"
                });
            }

            // Best Goalkeeper — highest avg rating among goalkeepers
            // Fetch player positions to identify goalkeepers
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
                    .OrderByDescending(g => g.Average(r => r.Rating))
                    .FirstOrDefault();

                if (bestGoalkeeper != null)
                {
                    hallOfFameRecords.Add(new TournamentHallOfFameEntity
                    {
                        TournamentId = tournamentId,
                        PlayerId = bestGoalkeeper.Key,
                        AwardType = "BestGoalkeeper"
                    });
                }
            }

            // Best Player — highest avg rating overall
            var bestPlayer = allRatings
                .GroupBy(r => r.PlayerId)
                .OrderByDescending(g => g.Average(r => r.Rating))
                .FirstOrDefault();

            if (bestPlayer != null)
            {
                hallOfFameRecords.Add(new TournamentHallOfFameEntity
                {
                    TournamentId = tournamentId,
                    PlayerId = bestPlayer.Key,
                    AwardType = "BestPlayer"
                });
            }

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
                    .ThenInclude(f => f.WinnerTeam)
                        .ThenInclude(tt => tt!.Team)
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
                    .Select(f => MapFixtureToDto(f))
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
                    .ThenInclude(f => f.WinnerTeam)
                        .ThenInclude(tt => tt!.Team)
                .Where(r => r.TournamentId == tournamentId)
                .OrderBy(r => r.RoundNumber)
                .ToListAsync();

            return rounds.Select(r => new RoundDto
            {
                RoundId = r.Id,
                RoundName = r.Name,
                RoundNumber = r.RoundNumber,
                Fixtures = r.TournamentFixtures
                    .Select(f => MapFixtureToDto(f))
                    .ToList()
            }).ToList();
        }

        private FixtureDto MapFixtureToDto(TournamentFixtureEntity f) =>
            new()
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