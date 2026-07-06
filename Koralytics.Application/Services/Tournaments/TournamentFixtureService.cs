using Koralytics.Application.Interfaces;
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
using TournamentTeamEntity = Koralytics.Domain.Entities.Tournamet.TournamentTeam;

namespace Koralytics.Application.Services.Tournament
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

            // Fetch the match with full result data
            var match = await _unitOfWork.Repository<MatchEntity>()
                .FindAsync(m => m.Id == matchId);

            if (match is null)
                throw new NotFoundException($"Match with Id {matchId} not found");

            if (match.Status != MatchStatus.Completed)
                throw new BadRequestException(
                    "Match must be completed before updating standings");

            // Fetch the fixture that links this match to this group
            var fixture = await _unitOfWork.Repository<TournamentFixtureEntity>()
                .FindAsync(f =>
                    f.MatchId == matchId &&
                    f.GroupId == groupId);

            if (fixture is null)
                throw new NotFoundException(
                    $"Fixture not found for match {matchId} in group {groupId}");

            // Fetch standings for both teams in this group
            var homeStanding = await _unitOfWork.Repository<TournamentStandingEntity>()
                .FindAsync(s =>
                    s.GroupId == groupId &&
                    s.TournamentTeamId == fixture.HomeTeamId);

            var awayStanding = await _unitOfWork.Repository<TournamentStandingEntity>()
                .FindAsync(s =>
                    s.GroupId == groupId &&
                    s.TournamentTeamId == fixture.AwayTeamId);

            if (homeStanding is null)
                throw new NotFoundException(
                    $"Standing not found for home team in group {groupId}");

            if (awayStanding is null)
                throw new NotFoundException(
                    $"Standing not found for away team in group {groupId}");

            // Update played count for both teams
            homeStanding.Played++;
            awayStanding.Played++;

            // Update goals
            homeStanding.GoalsFor += match.HomeScore;
            homeStanding.GoalsAgainst += match.AwayScore;
            awayStanding.GoalsFor += match.AwayScore;
            awayStanding.GoalsAgainst += match.HomeScore;

            // Determine result and update W/D/L + points
            if (match.HomeScore > match.AwayScore)
            {
                // Home win
                homeStanding.Won++;
                homeStanding.Points += 3;
                awayStanding.Lost++;
            }
            else if (match.HomeScore < match.AwayScore)
            {
                // Away win
                awayStanding.Won++;
                awayStanding.Points += 3;
                homeStanding.Lost++;
            }
            else
            {
                // Check penalties for a decisive result
                if (match.HomePenaltyScore.HasValue &&
                    match.AwayPenaltyScore.HasValue)
                {
                    // Penalty shootout — counts as a draw in standings
                    // but winner advances (handled in AdvanceKnockoutAsync)
                    homeStanding.Drawn++;
                    homeStanding.Points++;
                    awayStanding.Drawn++;
                    awayStanding.Points++;
                }
                else
                {
                    // Regular draw
                    homeStanding.Drawn++;
                    homeStanding.Points++;
                    awayStanding.Drawn++;
                    awayStanding.Points++;
                }
            }

            // Mark fixture as completed
            fixture.Status = MatchStatus.Completed;
            fixture.HomeScore = match.HomeScore;
            fixture.AwayScore = match.AwayScore;
            fixture.WinnerTeamId = match.WinningTeamId;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Standings updated successfully for group {GroupId} after match {MatchId}",
                groupId, matchId);
        }

  
        public async Task AdvanceKnockoutAsync(int tournamentId, int roundId)
        {
            _logger.LogInformation(
                "Advancing knockout for tournament {TournamentId} from round {RoundId}",
                tournamentId, roundId);

            // Validate tournament exists and is in progress
            var tournament = await _unitOfWork.Repository<TournamentEntity>()
                .FindAsync(t => t.Id == tournamentId);

            if (tournament is null)
                throw new NotFoundException(
                    $"Tournament with Id {tournamentId} not found");

            if (tournament.Status != TournamentStatus.InProgress)
                throw new BadRequestException(
                    "Tournament must be InProgress to advance knockout rounds");

            // Validate current round exists and belongs to this tournament
            var currentRound = await _unitOfWork.Repository<TournamentRoundEntity>()
                .FindAsync(r =>
                    r.Id == roundId &&
                    r.TournamentId == tournamentId);

            if (currentRound is null)
                throw new NotFoundException(
                    $"Round with Id {roundId} not found in tournament {tournamentId}");

            // Fetch all fixtures in this round
            var fixtures = await _unitOfWork.Repository<TournamentFixtureEntity>()
                .GetQueryable()
                .Where(f => f.RoundId == roundId)
                .ToListAsync();

            if (fixtures.Count == 0)
                throw new BadRequestException(
                    $"No fixtures found in round {roundId}");

            // Validate all fixtures are completed
            var incomplete = fixtures.Any(f => f.Status != MatchStatus.Completed);
            if (incomplete)
                throw new BadRequestException(
                    "All fixtures in the current round must be completed " +
                    "before advancing to the next round");

            // If two legs — group fixtures by pairs and determine aggregate winner
            var winners = tournament.HasTwoLegs
                ? GetTwoLegWinners(fixtures)
                : GetSingleLegWinners(fixtures);

            if (winners.Count == 0)
                throw new BadRequestException(
                    "Could not determine winners from current round fixtures");

            // If only one winner — tournament is complete, no next round needed
            if (winners.Count == 1)
            {
                _logger.LogInformation(
                    "Tournament {TournamentId} has a winner. No next round needed",
                    tournamentId);
                return;
            }

            // Create next round
            var nextRound = new TournamentRoundEntity
            {
                TournamentId = tournamentId,
                RoundNumber = currentRound.RoundNumber + 1,
                Name = GetRoundName(winners.Count)
            };

            await _unitOfWork.Repository<TournamentRoundEntity>()
                .AddAsync(nextRound);
            await _unitOfWork.SaveChangesAsync();

            // Pair winners into fixtures for next round
            // Seed order preserved — first winner vs last winner etc.
            for (int i = 0; i < winners.Count / 2; i++)
            {
                var home = winners[i];
                var away = winners[winners.Count - 1 - i];

                var nextFixture = new TournamentFixtureEntity
                {
                    RoundId = nextRound.Id,
                    GroupId = null,
                    HomeTeamId = home,
                    AwayTeamId = away,
                    Status = MatchStatus.Scheduled,
                    LegNumber = tournament.HasTwoLegs ? 1 : null
                };

                await _unitOfWork.Repository<TournamentFixtureEntity>()
                    .AddAsync(nextFixture);

                // Second leg
                if (tournament.HasTwoLegs)
                {
                    var secondLeg = new TournamentFixtureEntity
                    {
                        RoundId = nextRound.Id,
                        GroupId = null,
                        HomeTeamId = away,
                        AwayTeamId = home,
                        Status = MatchStatus.Scheduled,
                        LegNumber = 2
                    };

                    await _unitOfWork.Repository<TournamentFixtureEntity>()
                        .AddAsync(secondLeg);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Next round created with {Count} fixtures for tournament {TournamentId}",
                winners.Count / 2, tournamentId);
        }

    
        // Gets winners from single leg fixtures
        private List<int> GetSingleLegWinners(
            List<TournamentFixtureEntity> fixtures)
        {
            var winners = new List<int>();

            foreach (var fixture in fixtures)
            {
                if (fixture.WinnerTeamId.HasValue)
                {
                    winners.Add(fixture.WinnerTeamId.Value);
                }
                else
                {
                    // Should not happen if fixture is completed
                    throw new BadRequestException(
                        $"Fixture {fixture.Id} is completed but has no winner");
                }
            }

            return winners;
        }

        // Gets winners from two leg fixtures using aggregate score
        private List<int> GetTwoLegWinners(
            List<TournamentFixtureEntity> fixtures)
        {
            var winners = new List<int>();

            // Group fixtures into pairs by HomeTeamId/AwayTeamId
            // Leg 1: Team A (home) vs Team B (away)
            // Leg 2: Team B (home) vs Team A (away)
            var leg1Fixtures = fixtures
                .Where(f => f.LegNumber == 1)
                .ToList();

            foreach (var leg1 in leg1Fixtures)
            {
                // Find the corresponding leg 2
                var leg2 = fixtures.FirstOrDefault(f =>
                    f.LegNumber == 2 &&
                    f.HomeTeamId == leg1.AwayTeamId &&
                    f.AwayTeamId == leg1.HomeTeamId);

                if (leg2 is null)
                    throw new BadRequestException(
                        $"Could not find second leg for fixture {leg1.Id}");

                // Aggregate scores
                // Team A total = leg1 home score + leg2 away score
                // Team B total = leg1 away score + leg2 home score
                int teamAScore = (leg1.HomeScore ?? 0) + (leg2.AwayScore ?? 0);
                int teamBScore = (leg1.AwayScore ?? 0) + (leg2.HomeScore ?? 0);

                if (teamAScore > teamBScore)
                {
                    winners.Add(leg1.HomeTeamId);
                }
                else if (teamBScore > teamAScore)
                {
                    winners.Add(leg1.AwayTeamId);
                }
                else
                {
                    // Aggregate draw — use away goals rule
                    // Away goals = goals scored away from home
                    // Team A away goals = leg2 away score
                    // Team B away goals = leg1 away score
                    int teamAAwayGoals = leg2.AwayScore ?? 0;
                    int teamBAwayGoals = leg1.AwayScore ?? 0;

                    if (teamAAwayGoals > teamBAwayGoals)
                        winners.Add(leg1.HomeTeamId);
                    else if (teamBAwayGoals > teamAAwayGoals)
                        winners.Add(leg1.AwayTeamId);
                    else
                    {
                        // Still tied — check penalty winner from leg2
                        if (leg2.WinnerTeamId.HasValue)
                            winners.Add(leg2.WinnerTeamId.Value);
                        else
                            throw new BadRequestException(
                                $"Cannot determine winner for tied two-leg " +
                                $"fixture pair. Penalty shootout result missing");
                    }
                }
            }

            return winners;
        }

        // Returns round name based on remaining team count
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