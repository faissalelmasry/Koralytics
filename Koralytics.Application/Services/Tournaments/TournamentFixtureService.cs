using Koralytics.Application.DTOs.Tournaments;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Tournament;
using Koralytics.Application.Interfaces.Tournaments;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DomainEnums = Koralytics.Domain.Enums;
using MatchEntity = Koralytics.Domain.Entities.Match.Match;
using TournamentEntity = Koralytics.Domain.Entities.Tournamet.Tournament;
using TournamentFixtureEntity = Koralytics.Domain.Entities.Tournamet.TournamentFixture;
using TournamentGroupEntity = Koralytics.Domain.Entities.Tournamet.TournamentGroup;
using TournamentRoundEntity = Koralytics.Domain.Entities.Tournamet.TournamentRound;
using TournamentStandingEntity = Koralytics.Domain.Entities.Tournamet.TournamentStanding;
using Koralytics.Domain.Entities.Match;
using Koralytics.Domain.Entities.Academy;
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

        public async Task<TournamentFixtureDetailDto> GetFixtureDetailsAsync(int fixtureId)
        {
            var fixture = await _unitOfWork.Repository<TournamentFixtureEntity>()
                .GetQueryable()
                .Include(f => f.Group).ThenInclude(g => g!.Tournament)
                .Include(f => f.Round).ThenInclude(r => r!.Tournament)
                .Include(f => f.HomeTeam).ThenInclude(tt => tt.Team).ThenInclude(t => t.AgeGroup).ThenInclude(t=>t.Academy)
                .Include(f => f.AwayTeam).ThenInclude(tt => tt.Team).ThenInclude(t => t.AgeGroup).ThenInclude(t => t.Academy)
                .FirstOrDefaultAsync(f => f.Id == fixtureId);

            if (fixture is null)
                throw new NotFoundException($"TournamentFixture with Id {fixtureId} not found");

            var tournament = fixture.Group?.Tournament ?? fixture.Round?.Tournament;

            return new TournamentFixtureDetailDto
            {
                FixtureId = fixture.Id,
                MatchId = fixture.MatchId,
                HomeTeamId = fixture.HomeTeamId,
                HomeRealTeamId = fixture.HomeTeam?.TeamId ?? fixture.HomeTeamId,
                HomeTeamName = fixture.HomeTeam.Team.Name,
                HomeAcademyName = fixture.HomeTeam.Team.AgeGroup?.Academy?.Name,
                AwayTeamId = fixture.AwayTeamId,
                AwayRealTeamId = fixture.AwayTeam?.TeamId ?? fixture.AwayTeamId,
                AwayTeamName = fixture.AwayTeam.Team.Name,
                AwayAcademyName = fixture.AwayTeam.Team.AgeGroup.Academy?.Name,
                TournamentId = tournament?.Id ?? 0,
                TournamentName = tournament?.Name ?? string.Empty,
                GroupOrRoundName = fixture.Group?.Name ?? fixture.Round?.Name,
                Format = tournament?.Format ?? DomainEnums.MatchFormat.ElevenSide,
                Status = fixture.Status,
                LegNumber = fixture.LegNumber
            };
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

        public async Task UpdateFixtureResultAsync(int fixtureId, int homeScore, int awayScore)
        {
            _logger.LogInformation("Updating result for fixture {FixtureId}: {HomeScore}-{AwayScore}", fixtureId, homeScore, awayScore);

            var fixture = await _unitOfWork.Repository<TournamentFixtureEntity>()
                .FindAsync(f => f.Id == fixtureId);

            if (fixture is null)
                throw new NotFoundException($"Fixture with Id {fixtureId} not found");

            int oldHomeScore = fixture.HomeScore ?? 0;
            int oldAwayScore = fixture.AwayScore ?? 0;
            bool isPreviouslyCompleted = fixture.Status == MatchStatus.Completed;

            fixture.HomeScore = homeScore;
            fixture.AwayScore = awayScore;
            fixture.Status = MatchStatus.Completed;

            if (homeScore > awayScore)
            {
                fixture.WinnerTeamId = fixture.HomeTeamId;
            }
            else if (awayScore > homeScore)
            {
                fixture.WinnerTeamId = fixture.AwayTeamId;
            }
            else
            {
                fixture.WinnerTeamId = null;
            }

            if (fixture.GroupId.HasValue)
            {
                var groupId = fixture.GroupId.Value;
                var homeStanding = await _unitOfWork.Repository<TournamentStandingEntity>()
                    .FindAsync(s => s.GroupId == groupId && s.TournamentTeamId == fixture.HomeTeamId);
                var awayStanding = await _unitOfWork.Repository<TournamentStandingEntity>()
                    .FindAsync(s => s.GroupId == groupId && s.TournamentTeamId == fixture.AwayTeamId);

                if (homeStanding != null && awayStanding != null)
                {
                    if (!isPreviouslyCompleted)
                    {
                        homeStanding.Played++;
                        awayStanding.Played++;
                        homeStanding.GoalsFor += homeScore;
                        homeStanding.GoalsAgainst += awayScore;
                        awayStanding.GoalsFor += awayScore;
                        awayStanding.GoalsAgainst += homeScore;

                        if (homeScore > awayScore)
                        {
                            homeStanding.Won++;
                            homeStanding.Points += 3;
                            awayStanding.Lost++;
                        }
                        else if (awayScore > homeScore)
                        {
                            awayStanding.Won++;
                            awayStanding.Points += 3;
                            homeStanding.Lost++;
                        }
                        else
                        {
                            homeStanding.Drawn++;
                            homeStanding.Points += 1;
                            awayStanding.Drawn++;
                            awayStanding.Points += 1;
                        }
                    }
                    else
                    {
                        homeStanding.GoalsFor = homeStanding.GoalsFor - oldHomeScore + homeScore;
                        homeStanding.GoalsAgainst = homeStanding.GoalsAgainst - oldAwayScore + awayScore;
                        awayStanding.GoalsFor = awayStanding.GoalsFor - oldAwayScore + awayScore;
                        awayStanding.GoalsAgainst = awayStanding.GoalsAgainst - oldHomeScore + homeScore;

                        if (oldHomeScore > oldAwayScore)
                        {
                            homeStanding.Won--; homeStanding.Points -= 3; awayStanding.Lost--;
                        }
                        else if (oldAwayScore > oldHomeScore)
                        {
                            awayStanding.Won--; awayStanding.Points -= 3; homeStanding.Lost--;
                        }
                        else
                        {
                            homeStanding.Drawn--; homeStanding.Points -= 1; awayStanding.Drawn--; awayStanding.Points -= 1;
                        }

                        if (homeScore > awayScore)
                        {
                            homeStanding.Won++; homeStanding.Points += 3; awayStanding.Lost++;
                        }
                        else if (awayScore > homeScore)
                        {
                            awayStanding.Won++; awayStanding.Points += 3; homeStanding.Lost++;
                        }
                        else
                        {
                            homeStanding.Drawn++; homeStanding.Points += 1; awayStanding.Drawn++; awayStanding.Points += 1;
                        }
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task GenerateKnockoutFromGroupsAsync(int tournamentId)
        {
            _logger.LogInformation("Generating knockout stage from group results for tournament {TournamentId}", tournamentId);

            var tournament = await _unitOfWork.Repository<TournamentEntity>()
                .FindAsync(t => t.Id == tournamentId);

            if (tournament is null)
                throw new NotFoundException($"Tournament with Id {tournamentId} not found");

            if (tournament.Status != TournamentStatus.InProgress)
                throw new BadRequestException("Tournament must be InProgress to generate knockout stage");

            // Check no knockout rounds already exist
            var existingRound = await _unitOfWork.Repository<TournamentRoundEntity>()
                .ExistsAsync(r => r.TournamentId == tournamentId);
            if (existingRound)
                throw new ConflictException("Knockout rounds already exist for this tournament");

            // Load all groups with their standings for this tournament
            var groups = await _unitOfWork.Repository<TournamentGroupEntity>()
                .GetQueryable()
                .Where(g => g.TournamentId == tournamentId && !g.IsDummy)
                .ToListAsync();

            if (groups.Count == 0)
                throw new BadRequestException("No groups found for this tournament");

            // Load all fixtures from groups — ensure all are completed
            var allGroupFixtures = await _unitOfWork.Repository<TournamentFixtureEntity>()
                .GetQueryable()
                .Where(f => f.GroupId != null && groups.Select(g => g.Id).Contains(f.GroupId!.Value))
                .ToListAsync();

            if (allGroupFixtures.Count == 0)
                throw new BadRequestException("No group fixtures found. Run the draw first.");

            var incompleteCount = allGroupFixtures.Count(f => f.Status != MatchStatus.Completed);
            if (incompleteCount > 0)
                throw new BadRequestException(
                    $"All group stage fixtures must be completed before generating the knockout. " +
                    $"{incompleteCount} fixture(s) still pending.");

            // Load standings per group — pick the top team by Points, then GD (GoalsFor - GoalsAgainst), then GF
            var standings = await _unitOfWork.Repository<TournamentStandingEntity>()
                .GetQueryable()
                .Where(s => groups.Select(g => g.Id).Contains(s.GroupId))
                .ToListAsync();

            var qualifiers = new List<int>(); // TournamentTeamIds of group winners

            foreach (var group in groups.OrderBy(g => g.Name))
            {
                var groupStandings = standings
                    .Where(s => s.GroupId == group.Id)
                    .OrderByDescending(s => s.Points)
                    .ThenByDescending(s => s.GoalsFor - s.GoalsAgainst)
                    .ThenByDescending(s => s.GoalsFor)
                    .ToList();

                if (groupStandings.Count == 0)
                    throw new BadRequestException($"No standings found for group {group.Name}");

                qualifiers.Add(groupStandings.First().TournamentTeamId);
            }

            if (qualifiers.Count < 2)
                throw new BadRequestException("Need at least 2 group winners to generate a knockout stage");

            // Create Round 1
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var round = new TournamentRoundEntity
                {
                    TournamentId = tournamentId,
                    RoundNumber = 1,
                    Name = GetRoundName(qualifiers.Count)
                };

                await _unitOfWork.Repository<TournamentRoundEntity>().AddAsync(round);
                await _unitOfWork.SaveChangesAsync();

                // Pair group winners: Group A winner vs Group B winner, etc.
                // Standard format: 1st Group A vs 1st Group B, 1st Group C vs 1st Group D ...
                for (int i = 0; i < qualifiers.Count / 2; i++)
                {
                    var home = qualifiers[i];
                    var away = qualifiers[qualifiers.Count - 1 - i];

                    await _unitOfWork.Repository<TournamentFixtureEntity>().AddAsync(new TournamentFixtureEntity
                    {
                        RoundId = round.Id,
                        GroupId = null,
                        HomeTeamId = home,
                        AwayTeamId = away,
                        Status = MatchStatus.Scheduled,
                        LegNumber = tournament.HasTwoLegs ? 1 : null
                    });

                    if (tournament.HasTwoLegs)
                        await _unitOfWork.Repository<TournamentFixtureEntity>().AddAsync(new TournamentFixtureEntity
                        {
                            RoundId = round.Id,
                            GroupId = null,
                            HomeTeamId = away,
                            AwayTeamId = home,
                            Status = MatchStatus.Scheduled,
                            LegNumber = 2
                        });
                }

                // Handle odd number of qualifiers — give a bye to the last team (make them a finalist directly)
                if (qualifiers.Count % 2 != 0)
                {
                    _logger.LogInformation("Odd number of group winners ({Count}), last team gets a bye", qualifiers.Count);
                }

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Knockout stage generated for tournament {TournamentId} with {Count} qualifiers", tournamentId, qualifiers.Count);
        }

        public async Task UpdateFixtureStatsAsync(int fixtureId, UpdateFixtureStatsDto dto)
        {
            var fixture = await _unitOfWork.Repository<TournamentFixtureEntity>()
                .GetQueryable()
                .Include(f => f.HomeTeam)
                .Include(f => f.AwayTeam)
                .Include(f => f.Group).ThenInclude(g => g.Tournament)
                .Include(f => f.Round).ThenInclude(r => r.Tournament)
                .FirstOrDefaultAsync(f => f.Id == fixtureId);

            if (fixture is null)
                throw new NotFoundException($"TournamentFixture with Id {fixtureId} not found");

            var tournament = fixture.Group?.Tournament ?? fixture.Round?.Tournament;

            int matchId;

            // If match doesn't exist, create it transparently
            if (!fixture.MatchId.HasValue)
            {
                var match = new MatchEntity
                {
                    TournamentId = tournament?.Id,
                    HomeTeamId = fixture.HomeTeam.TeamId,
                    AwayTeamId = fixture.AwayTeam.TeamId,
                    Type = DomainEnums.MatchType.Tournament,
                    Status = DomainEnums.MatchStatus.Completed,
                    Format = tournament?.Format ?? MatchFormat.ElevenSide,
                    HomeScore = fixture.HomeScore ?? 0,
                    AwayScore = fixture.AwayScore ?? 0,
                    WinningTeamId = fixture.WinnerTeamId > 0 ? (fixture.WinnerTeamId == fixture.HomeTeamId ? fixture.HomeTeam.TeamId : fixture.AwayTeam.TeamId) : null,
                    MatchDate = DateTime.UtcNow
                };

                await _unitOfWork.Repository<MatchEntity>().AddAsync(match);
                await _unitOfWork.SaveChangesAsync();

                fixture.MatchId = match.Id;
                await _unitOfWork.SaveChangesAsync();
                matchId = match.Id;
            }
            else
            {
                matchId = fixture.MatchId.Value;
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Clean up old events & MOTM
                var existingEvents = await _unitOfWork.Repository<MatchEvent>()
                    .GetQueryable()
                    .Where(e => e.MatchId == matchId && e.EventType == MatchEventType.Goal)
                    .ToListAsync();

                foreach (var ev in existingEvents)
                {
                    _unitOfWork.Repository<MatchEvent>().SoftDelete(ev);
                }

                var existingRatings = await _unitOfWork.Repository<MatchPlayerRating>()
                    .GetQueryable()
                    .Where(r => r.MatchId == matchId && r.IsMOTM)
                    .ToListAsync();

                foreach (var r in existingRatings)
                {
                    r.IsMOTM = false;
                }

                await _unitOfWork.SaveChangesAsync();

                // Add new Goals
                foreach (var goal in dto.Goals)
                {
                    var teamId = goal.IsHomeSide ? fixture.HomeTeam.TeamId : fixture.AwayTeam.TeamId;
                    var matchEvent = new MatchEvent
                    {
                        MatchId = matchId,
                        TeamId = teamId,
                        PlayerId = goal.PlayerId,
                        AssistPlayerId = goal.AssistPlayerId,
                        EventType = MatchEventType.Goal,
                        Minute = goal.Minute,
                        IsHomeSide = goal.IsHomeSide
                    };
                    await _unitOfWork.Repository<MatchEvent>().AddAsync(matchEvent);
                }

                // Set new MOTM
                if (dto.MotmPlayerId.HasValue && dto.MotmPlayerId.Value > 0)
                {
                    var existingRating = await _unitOfWork.Repository<MatchPlayerRating>()
                        .GetQueryable()
                        .FirstOrDefaultAsync(r => r.MatchId == matchId && r.PlayerId == dto.MotmPlayerId.Value);

                    if (existingRating != null)
                    {
                        existingRating.IsMOTM = true;
                    }
                    else
                    {
                        var rating = new MatchPlayerRating
                        {
                            MatchId = matchId,
                            PlayerId = dto.MotmPlayerId.Value,
                            CoachId = fixture.CreatedById ?? 1,
                            IsMOTM = true,
                            MinutesPlayed = 0
                        };
                        await _unitOfWork.Repository<MatchPlayerRating>().AddAsync(rating);
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

}
