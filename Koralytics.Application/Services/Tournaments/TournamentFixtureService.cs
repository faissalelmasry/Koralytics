using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Tournament;
using Koralytics.Application.Interfaces.Tournaments;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MatchEntity = Koralytics.Domain.Entities.Match.Match;
using TournamentEntity = Koralytics.Domain.Entities.Tournamet.Tournament;
using TournamentFixtureEntity = Koralytics.Domain.Entities.Tournamet.TournamentFixture;
using TournamentRoundEntity = Koralytics.Domain.Entities.Tournamet.TournamentRound;
using TournamentStandingEntity = Koralytics.Domain.Entities.Tournamet.TournamentStanding;

namespace Koralytics.Application.Services.Tournaments
{
    public class TournamentFixtureService : ITournamentFixtureService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TournamentFixtureService> _logger;

        public TournamentFixtureService(
            IUnitOfWork unitOfWork,
            ILogger<TournamentFixtureService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task UpdateStandingsAsync(int groupId, int matchId)
        {
            _logger.LogInformation(
                "Updating standings for group {GroupId} after match {MatchId}",
                groupId, matchId);

            // Idempotency — check if standings already updated for this match
            var alreadyUpdated = await _unitOfWork
                .Repository<TournamentFixtureEntity>()
                .ExistsAsync(f =>
                    f.MatchId == matchId &&
                    f.GroupId == groupId &&
                    f.Status == MatchStatus.Completed);

            if (alreadyUpdated)
                throw new ConflictException(
                    "Standings already updated for this match");

            var match = await _unitOfWork.Repository<MatchEntity>()
                .FindAsync(m => m.Id == matchId);

            if (match is null)
                throw new NotFoundException(
                    $"Match with Id {matchId} not found");

            if (match.Status != MatchStatus.Completed)
                throw new BadRequestException(
                    "Match must be completed before updating standings");

            var fixture = await _unitOfWork.Repository<TournamentFixtureEntity>()
                .FindAsync(f =>
                    f.MatchId == matchId &&
                    f.GroupId == groupId);

            if (fixture is null)
                throw new NotFoundException(
                    $"Fixture not found for match {matchId} in group {groupId}");

            var homeStanding = await _unitOfWork
                .Repository<TournamentStandingEntity>()
                .FindAsync(s =>
                    s.GroupId == groupId &&
                    s.TournamentTeamId == fixture.HomeTeamId);

            var awayStanding = await _unitOfWork
                .Repository<TournamentStandingEntity>()
                .FindAsync(s =>
                    s.GroupId == groupId &&
                    s.TournamentTeamId == fixture.AwayTeamId);

            if (homeStanding is null)
                throw new NotFoundException(
                    $"Standing not found for home team in group {groupId}");

            if (awayStanding is null)
                throw new NotFoundException(
                    $"Standing not found for away team in group {groupId}");

            homeStanding.Played++;
            awayStanding.Played++;

            homeStanding.GoalsFor += match.HomeScore;
            homeStanding.GoalsAgainst += match.AwayScore;
            awayStanding.GoalsFor += match.AwayScore;
            awayStanding.GoalsAgainst += match.HomeScore;

            if (match.HomeScore > match.AwayScore)
            {
                homeStanding.Won++;
                homeStanding.Points += 3;
                awayStanding.Lost++;
            }
            else if (match.HomeScore < match.AwayScore)
            {
                awayStanding.Won++;
                awayStanding.Points += 3;
                homeStanding.Lost++;
            }
            else
            {
                homeStanding.Drawn++;
                homeStanding.Points++;
                awayStanding.Drawn++;
                awayStanding.Points++;
            }

            fixture.Status = MatchStatus.Completed;
            fixture.HomeScore = match.HomeScore;
            fixture.AwayScore = match.AwayScore;
            fixture.WinnerTeamId = match.WinningTeamId;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Standings updated for group {GroupId} after match {MatchId}",
                groupId, matchId);
        }

        public async Task AdvanceKnockoutAsync(int tournamentId, int roundId)
        {
            _logger.LogInformation(
                "Advancing knockout for tournament {TournamentId} from round {RoundId}",
                tournamentId, roundId);

            var tournament = await _unitOfWork.Repository<TournamentEntity>()
                .FindAsync(t => t.Id == tournamentId);

            if (tournament is null)
                throw new NotFoundException(
                    $"Tournament with Id {tournamentId} not found");

            if (tournament.Status != TournamentStatus.InProgress)
                throw new BadRequestException(
                    "Tournament must be InProgress to advance knockout rounds");

            var currentRound = await _unitOfWork.Repository<TournamentRoundEntity>()
                .FindAsync(r =>
                    r.Id == roundId &&
                    r.TournamentId == tournamentId);

            if (currentRound is null)
                throw new NotFoundException(
                    $"Round {roundId} not found in tournament {tournamentId}");

            // Idempotency — check next round doesn't already exist
            var nextRoundExists = await _unitOfWork
                .Repository<TournamentRoundEntity>()
                .ExistsAsync(r =>
                    r.TournamentId == tournamentId &&
                    r.RoundNumber == currentRound.RoundNumber + 1);

            if (nextRoundExists)
                throw new ConflictException(
                    "Next round already exists for this tournament");

            var fixtures = await _unitOfWork.Repository<TournamentFixtureEntity>()
                .GetQueryable()
                .Where(f => f.RoundId == roundId)
                .ToListAsync();

            if (fixtures.Count == 0)
                throw new BadRequestException(
                    $"No fixtures found in round {roundId}");

            if (fixtures.Any(f => f.Status != MatchStatus.Completed))
                throw new BadRequestException(
                    "All fixtures in the current round must be completed " +
                    "before advancing");

            var winners = tournament.HasTwoLegs
                ? GetTwoLegWinners(fixtures)
                : GetSingleLegWinners(fixtures);

            if (winners.Count == 0)
                throw new BadRequestException(
                    "Could not determine winners from current round");

            // Final round — no next round needed
            if (winners.Count == 1)
            {
                _logger.LogInformation(
                    "Tournament {TournamentId} has a winner. No next round needed",
                    tournamentId);
                return;
            }

            // Wrap in transaction
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var nextRound = new TournamentRoundEntity
                {
                    TournamentId = tournamentId,
                    RoundNumber = currentRound.RoundNumber + 1,
                    Name = GetRoundName(winners.Count)
                };

                await _unitOfWork.Repository<TournamentRoundEntity>()
                    .AddAsync(nextRound);
                await _unitOfWork.SaveChangesAsync();

                for (int i = 0; i < winners.Count / 2; i++)
                {
                    var home = winners[i];
                    var away = winners[winners.Count - 1 - i];

                    await _unitOfWork.Repository<TournamentFixtureEntity>()
                        .AddAsync(new TournamentFixtureEntity
                        {
                            RoundId = nextRound.Id,
                            GroupId = null,
                            HomeTeamId = home,
                            AwayTeamId = away,
                            Status = MatchStatus.Scheduled,
                            LegNumber = tournament.HasTwoLegs ? 1 : null
                        });

                    if (tournament.HasTwoLegs)
                        await _unitOfWork.Repository<TournamentFixtureEntity>()
                            .AddAsync(new TournamentFixtureEntity
                            {
                                RoundId = nextRound.Id,
                                GroupId = null,
                                HomeTeamId = away,
                                AwayTeamId = home,
                                Status = MatchStatus.Scheduled,
                                LegNumber = 2
                            });
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
                "Next round created for tournament {TournamentId}",
                tournamentId);
        }

        // ─────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────

        private List<int> GetSingleLegWinners(
            List<TournamentFixtureEntity> fixtures)
        {
            var winners = new List<int>();

            foreach (var fixture in fixtures)
            {
                if (!fixture.WinnerTeamId.HasValue)
                    throw new BadRequestException(
                        $"Fixture {fixture.Id} is completed but has no winner");

                winners.Add(fixture.WinnerTeamId.Value);
            }

            return winners;
        }

        private List<int> GetTwoLegWinners(
            List<TournamentFixtureEntity> fixtures)
        {
            var winners = new List<int>();
            var leg1Fixtures = fixtures.Where(f => f.LegNumber == 1).ToList();

            foreach (var leg1 in leg1Fixtures)
            {
                var leg2 = fixtures.FirstOrDefault(f =>
                    f.LegNumber == 2 &&
                    f.HomeTeamId == leg1.AwayTeamId &&
                    f.AwayTeamId == leg1.HomeTeamId);

                if (leg2 is null)
                    throw new BadRequestException(
                        $"Could not find second leg for fixture {leg1.Id}");

                int teamAScore = (leg1.HomeScore ?? 0) + (leg2.AwayScore ?? 0);
                int teamBScore = (leg1.AwayScore ?? 0) + (leg2.HomeScore ?? 0);

                if (teamAScore > teamBScore)
                    winners.Add(leg1.HomeTeamId);
                else if (teamBScore > teamAScore)
                    winners.Add(leg1.AwayTeamId);
                else
                {
                    int teamAAwayGoals = leg2.AwayScore ?? 0;
                    int teamBAwayGoals = leg1.AwayScore ?? 0;

                    if (teamAAwayGoals > teamBAwayGoals)
                        winners.Add(leg1.HomeTeamId);
                    else if (teamBAwayGoals > teamAAwayGoals)
                        winners.Add(leg1.AwayTeamId);
                    else if (leg2.WinnerTeamId.HasValue)
                        winners.Add(leg2.WinnerTeamId.Value);
                    else
                        throw new BadRequestException(
                            $"Cannot determine winner for tied two-leg fixture. " +
                            $"Penalty result missing");
                }
            }

            return winners;
        }

        private string GetRoundName(int teamCount) => teamCount switch
        {
            2 => "Final",
            4 => "Semi-Final",
            8 => "Quarter-Final",
            16 => "Round of 16",
            _ => $"Round of {teamCount}"
        };
    }
}