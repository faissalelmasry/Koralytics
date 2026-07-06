// TournamentDrawService.cs
using TournamentEntity = Koralytics.Domain.Entities.Tournamet.Tournament;
using TournamentTeamEntity = Koralytics.Domain.Entities.Tournamet.TournamentTeam;
using TournamentGroupEntity = Koralytics.Domain.Entities.Tournamet.TournamentGroup;
using TournamentRoundEntity = Koralytics.Domain.Entities.Tournamet.TournamentRound;
using TournamentFixtureEntity = Koralytics.Domain.Entities.Tournamet.TournamentFixture;
using TournamentGroupTeamEntity = Koralytics.Domain.Entities.Tournamet.TournamentGroupTeam;
using TournamentStandingEntity = Koralytics.Domain.Entities.Tournamet.TournamentStanding;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Tournament;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koralytics.Application.Services.Tournament
{
    public class TournamentDrawService : ITournamentDrawService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TournamentDrawService> _logger;
        private readonly Random _random = new();

        public TournamentDrawService(
            IUnitOfWork unitOfWork,
            ILogger<TournamentDrawService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────
        // SEEDING
        // ─────────────────────────────────────────────────────────────

        public async Task GenerateSeedingAsync(int tournamentId)
        {
            _logger.LogInformation(
                "Generating seeding for tournament {TournamentId}", tournamentId);

            // Validate tournament exists
            var tournament = await _unitOfWork.Repository<TournamentEntity>()
                .FindAsync(t => t.Id == tournamentId);

            if (tournament is null)
                throw new NotFoundException(
                    $"Tournament with Id {tournamentId} not found");

            if (tournament.Status != TournamentStatus.Registration)
                throw new BadRequestException(
                    "Seeding can only be generated during Registration status");

            // Fetch all accepted tournament teams with their team data
            var tournamentTeams = await _unitOfWork.Repository<TournamentTeamEntity>()
                .GetQueryable()
                .Include(tt => tt.Team)
                .Where(tt =>
                    tt.TournamentId == tournamentId &&
                    tt.Status == TournamentTeamStatus.Accepted)
                .ToListAsync();

            if (tournamentTeams.Count < 2)
                throw new BadRequestException(
                    "At least 2 accepted teams are required to generate seeding");

            // Calculate seed score for each team
            var seedScores = new List<(TournamentTeamEntity Team, double Score)>();

            foreach (var tournamentTeam in tournamentTeams)
            {
                var score = await CalculateSeedScoreAsync(
                    tournamentTeam.TeamId, tournamentId);
                seedScores.Add((tournamentTeam, score));
            }

            // Order by score descending — highest score = seed #1
            var ordered = seedScores
                .OrderByDescending(x => x.Score)
                .ToList();

            // Assign seed numbers
            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].Team.SeedNumber = i + 1;
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Seeding generated for {Count} teams in tournament {TournamentId}",
                ordered.Count, tournamentId);
        }

        // Calculates a composite seed score for a team
        // Score = WinRate (0-1) + PlayerRating (0-10) + PreviousTournamentScore (0-1)
        private async Task<double> CalculateSeedScoreAsync(
            int teamId, int currentTournamentId)
        {
            double winRate = await CalculateWinRateAsync(teamId);
            double playerRating = await CalculateAveragePlayerRatingAsync(teamId);
            double previousResults = await CalculatePreviousTournamentScoreAsync(
                teamId, currentTournamentId);

            // Weighted composite score
            // Win rate weighted 40%, player rating 40%, previous results 20%
            return (winRate * 0.4) + (playerRating * 0.4) + (previousResults * 0.2);
        }

        private async Task<double> CalculateWinRateAsync(int teamId)
        {
            // Fetch all completed fixtures where this team participated
            var fixtures = await _unitOfWork.Repository<TournamentFixtureEntity>()
                .GetQueryable()
                .Where(f =>
                    (f.HomeTeamId == teamId || f.AwayTeamId == teamId) &&
                    f.Status == MatchStatus.Completed)
                .ToListAsync();

            if (fixtures.Count == 0) return 0;

            var wins = fixtures.Count(f => f.WinnerTeamId == teamId);
            return (double)wins / fixtures.Count;
        }

        private async Task<double> CalculateAveragePlayerRatingAsync(int teamId)
        {
            // TODO: Replace with PlayerCardService.GetPlayerCardAsync()
            // once Faissal's service is implemented
            // For now returns 0 as placeholder
            await Task.CompletedTask;
            return 0;
        }

        private async Task<double> CalculatePreviousTournamentScoreAsync(
            int teamId, int currentTournamentId)
        {
            // Check if team won any previous tournaments
            var previousWins = await _unitOfWork.Repository<TournamentFixtureEntity>()
                .GetQueryable()
                .Where(f =>
                    f.WinnerTeamId == teamId &&
                    f.Round != null &&
                    f.Status == MatchStatus.Completed)
                .CountAsync();

            // Normalize to 0-1 range (cap at 10 wins)
            return Math.Min(previousWins / 10.0, 1.0);
        }

        // ─────────────────────────────────────────────────────────────
        // DRAW GENERATION
        // ─────────────────────────────────────────────────────────────

        public async Task GenerateDrawAsync(int tournamentId)
        {
            _logger.LogInformation(
                "Generating draw for tournament {TournamentId}", tournamentId);

            // Validate tournament exists
            var tournament = await _unitOfWork.Repository<TournamentEntity>()
                .FindAsync(t => t.Id == tournamentId);

            if (tournament is null)
                throw new NotFoundException(
                    $"Tournament with Id {tournamentId} not found");

            if (tournament.Status != TournamentStatus.Registration)
                throw new BadRequestException(
                    "Draw can only be generated during Registration status");

            // Fetch all accepted and seeded teams ordered by seed
            var seededTeams = await _unitOfWork.Repository<TournamentTeamEntity>()
                .GetQueryable()
                .Where(tt =>
                    tt.TournamentId == tournamentId &&
                    tt.Status == TournamentTeamStatus.Accepted)
                .OrderBy(tt => tt.SeedNumber)
                .ToListAsync();

            if (seededTeams.Count < 2)
                throw new BadRequestException(
                    "At least 2 accepted teams are required to generate draw");

            // Check seeding has been done
            var unseeded = seededTeams.Any(tt => tt.SeedNumber == null);
            if (unseeded)
                throw new BadRequestException(
                    "All teams must be seeded before generating the draw. " +
                    "Run GenerateSeedingAsync first");

            // Generate draw based on tournament structure
            switch (tournament.Structure)
            {
                case TournamentStructure.Knockout:
                    await GenerateKnockoutDrawAsync(
                        tournament, seededTeams);
                    break;

                case TournamentStructure.GroupAndKnockout:
                    await GenerateGroupAndKnockoutDrawAsync(
                        tournament, seededTeams);
                    break;

                case TournamentStructure.League:
                    await GenerateLeagueDrawAsync(
                        tournament, seededTeams);
                    break;

                default:
                    throw new BadRequestException(
                        $"Unknown tournament structure: {tournament.Structure}");
            }

            // Update tournament status to InProgress
            tournament.Status = TournamentStatus.InProgress;
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Draw generated successfully for tournament {TournamentId}",
                tournamentId);
        }

        // ─────────────────────────────────────────────────────────────
        // KNOCKOUT DRAW
        // ─────────────────────────────────────────────────────────────

        private async Task GenerateKnockoutDrawAsync(
            TournamentEntity tournament,
            List<TournamentTeamEntity> seededTeams)
        {
            _logger.LogInformation(
                "Generating knockout draw for tournament {TournamentId}",
                tournament.Id);

            // Validate team count is a power of 2 for clean bracket
            // e.g. 2, 4, 8, 16 teams
            if (!IsPowerOfTwo(seededTeams.Count))
                throw new BadRequestException(
                    $"Knockout tournaments require a power of 2 number of teams " +
                    $"(2, 4, 8, 16). Got {seededTeams.Count}");

            // Create Round 1
            var round = new TournamentRoundEntity
            {
                TournamentId = tournament.Id,
                Name = GetRoundName(seededTeams.Count),
                RoundNumber = 1
            };

            await _unitOfWork.Repository<TournamentRoundEntity>().AddAsync(round);
            await _unitOfWork.SaveChangesAsync();

            // Pair teams respecting seeds
            // Seed 1 vs Seed N, Seed 2 vs Seed N-1, etc.
            var fixtures = PairTeamsBySeeding(seededTeams);

            foreach (var (home, away) in fixtures)
            {
                var fixture = new TournamentFixtureEntity
                {
                    RoundId = round.Id,
                    GroupId = null,
                    HomeTeamId = home.Id,
                    AwayTeamId = away.Id,
                    Status = MatchStatus.Scheduled,
                    LegNumber = tournament.HasTwoLegs ? 1 : null
                };

                await _unitOfWork.Repository<TournamentFixtureEntity>()
                    .AddAsync(fixture);

                // If two legs — create second leg fixture (home/away swapped)
                if (tournament.HasTwoLegs)
                {
                    var secondLeg = new TournamentFixtureEntity
                    {
                        RoundId = round.Id,
                        GroupId = null,
                        HomeTeamId = away.Id,
                        AwayTeamId = home.Id,
                        Status = MatchStatus.Scheduled,
                        LegNumber = 2
                    };

                    await _unitOfWork.Repository<TournamentFixtureEntity>()
                        .AddAsync(secondLeg);
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────────────────────
        // GROUP + KNOCKOUT DRAW
        // ─────────────────────────────────────────────────────────────

        private async Task GenerateGroupAndKnockoutDrawAsync(
            TournamentEntity tournament,
            List<TournamentTeamEntity> seededTeams)
        {
            _logger.LogInformation(
                "Generating group and knockout draw for tournament {TournamentId}",
                tournament.Id);

            // Calculate number of groups
            // Aim for groups of 4 teams — standard football group stage
            int groupCount = Math.Max(2, seededTeams.Count / 4);
            var groupNames = GenerateGroupNames(groupCount);

            // Create groups
            var groups = new List<TournamentGroupEntity>();
            foreach (var name in groupNames)
            {
                var group = new TournamentGroupEntity
                {
                    TournamentId = tournament.Id,
                    Name = name,
                    IsDummy = false
                };

                await _unitOfWork.Repository<TournamentGroupEntity>().AddAsync(group);
                groups.Add(group);
            }

            await _unitOfWork.SaveChangesAsync();

            // Distribute teams into groups respecting seeds
            // Pot system: divide seeded teams into pots, draw one from each pot per group
            var pots = CreateSeedingPots(seededTeams, groupCount);
            var groupAssignments = AssignTeamsToGroups(groups, pots);

            // Create TournamentGroupTeam records and standing rows
            foreach (var (group, teams) in groupAssignments)
            {
                foreach (var team in teams)
                {
                    // Group team assignment
                    var groupTeam = new TournamentGroupTeamEntity
                    {
                        GroupId = group.Id,
                        TournamentTeamId = team.Id
                    };

                    await _unitOfWork.Repository<TournamentGroupTeamEntity>()
                        .AddAsync(groupTeam);

                    // Initial standing row — all zeros
                    var standing = new TournamentStandingEntity
                    {
                        GroupId = group.Id,
                        TournamentTeamId = team.Id,
                        Played = 0,
                        Won = 0,
                        Drawn = 0,
                        Lost = 0,
                        GoalsFor = 0,
                        GoalsAgainst = 0,
                        Points = 0
                    };

                    await _unitOfWork.Repository<TournamentStandingEntity>()
                        .AddAsync(standing);
                }

                // Create fixtures within the group
                // Every team plays every other team once (or twice if HasTwoLegs)
                var teamList = groupAssignments[group];
                for (int i = 0; i < teamList.Count; i++)
                {
                    for (int j = i + 1; j < teamList.Count; j++)
                    {
                        var home = teamList[i];
                        var away = teamList[j];

                        var fixture = new TournamentFixtureEntity
                        {
                            GroupId = group.Id,
                            RoundId = null,
                            HomeTeamId = home.Id,
                            AwayTeamId = away.Id,
                            Status = MatchStatus.Scheduled,
                            LegNumber = tournament.HasTwoLegs ? 1 : null
                        };

                        await _unitOfWork.Repository<TournamentFixtureEntity>()
                            .AddAsync(fixture);

                        // Second leg — home/away swapped
                        if (tournament.HasTwoLegs)
                        {
                            var secondLeg = new TournamentFixtureEntity
                            {
                                GroupId = group.Id,
                                RoundId = null,
                                HomeTeamId = away.Id,
                                AwayTeamId = home.Id,
                                Status = MatchStatus.Scheduled,
                                LegNumber = 2
                            };

                            await _unitOfWork.Repository<TournamentFixtureEntity>()
                                .AddAsync(secondLeg);
                        }
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────────────────────
        // LEAGUE DRAW
        // ─────────────────────────────────────────────────────────────

        private async Task GenerateLeagueDrawAsync(
            TournamentEntity tournament,
            List<TournamentTeamEntity> seededTeams)
        {
            _logger.LogInformation(
                "Generating league draw for tournament {TournamentId}",
                tournament.Id);

            // Fetch the existing dummy group created during tournament creation
            var dummyGroup = await _unitOfWork.Repository<TournamentGroupEntity>()
                .FindAsync(g =>
                    g.TournamentId == tournament.Id &&
                    g.IsDummy == true);

            if (dummyGroup is null)
                throw new NotFoundException(
                    "League dummy group not found. " +
                    "Ensure tournament was created with League structure");

            // Create group team records and standing rows for all teams
            foreach (var team in seededTeams)
            {
                var groupTeam = new TournamentGroupTeamEntity
                {
                    GroupId = dummyGroup.Id,
                    TournamentTeamId = team.Id
                };

                await _unitOfWork.Repository<TournamentGroupTeamEntity>()
                    .AddAsync(groupTeam);

                var standing = new TournamentStandingEntity
                {
                    GroupId = dummyGroup.Id,
                    TournamentTeamId = team.Id,
                    Played = 0,
                    Won = 0,
                    Drawn = 0,
                    Lost = 0,
                    GoalsFor = 0,
                    GoalsAgainst = 0,
                    Points = 0
                };

                await _unitOfWork.Repository<TournamentStandingEntity>()
                    .AddAsync(standing);
            }

            await _unitOfWork.SaveChangesAsync();

            // Create fixtures — every team vs every other team
            for (int i = 0; i < seededTeams.Count; i++)
            {
                for (int j = i + 1; j < seededTeams.Count; j++)
                {
                    var home = seededTeams[i];
                    var away = seededTeams[j];

                    var fixture = new TournamentFixtureEntity
                    {
                        GroupId = dummyGroup.Id,
                        RoundId = null,
                        HomeTeamId = home.Id,
                        AwayTeamId = away.Id,
                        Status = MatchStatus.Scheduled,
                        LegNumber = tournament.HasTwoLegs ? 1 : null
                    };

                    await _unitOfWork.Repository<TournamentFixtureEntity>()
                        .AddAsync(fixture);

                    // Second leg — home/away swapped
                    if (tournament.HasTwoLegs)
                    {
                        var secondLeg = new TournamentFixtureEntity
                        {
                            GroupId = dummyGroup.Id,
                            RoundId = null,
                            HomeTeamId = away.Id,
                            AwayTeamId = home.Id,
                            Status = MatchStatus.Scheduled,
                            LegNumber = 2
                        };

                        await _unitOfWork.Repository<TournamentFixtureEntity>()
                            .AddAsync(secondLeg);
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────

        // Pairs seed 1 vs seed N, seed 2 vs seed N-1, etc.
        private List<(TournamentTeamEntity Home, TournamentTeamEntity Away)>
            PairTeamsBySeeding(List<TournamentTeamEntity> seededTeams)
        {
            var pairs = new List<(TournamentTeamEntity, TournamentTeamEntity)>();
            int left = 0;
            int right = seededTeams.Count - 1;

            while (left < right)
            {
                pairs.Add((seededTeams[left], seededTeams[right]));
                left++;
                right--;
            }

            return pairs;
        }

        // Creates seeding pots — divides teams into N pots of equal size
        // Pot 1 = top seeds, Pot 2 = next seeds, etc.
        private List<List<TournamentTeamEntity>> CreateSeedingPots(
            List<TournamentTeamEntity> seededTeams, int groupCount)
        {
            var pots = new List<List<TournamentTeamEntity>>();
            int potSize = seededTeams.Count / groupCount;

            for (int i = 0; i < groupCount; i++)
            {
                pots.Add(seededTeams
                    .Skip(i * potSize)
                    .Take(potSize)
                    .ToList());
            }

            return pots;
        }

        // Assigns one team from each pot to each group randomly
        private Dictionary<TournamentGroupEntity, List<TournamentTeamEntity>>
            AssignTeamsToGroups(
                List<TournamentGroupEntity> groups,
                List<List<TournamentTeamEntity>> pots)
        {
            var assignments = groups.ToDictionary(
                g => g,
                _ => new List<TournamentTeamEntity>());

            foreach (var pot in pots)
            {
                // Shuffle the pot for randomness
                var shuffled = pot.OrderBy(_ => _random.Next()).ToList();

                for (int i = 0; i < groups.Count && i < shuffled.Count; i++)
                {
                    assignments[groups[i]].Add(shuffled[i]);
                }
            }

            return assignments;
        }

        // Generates group names: Group A, Group B, Group C...
        private List<string> GenerateGroupNames(int count)
        {
            return Enumerable.Range(0, count)
                .Select(i => $"Group {(char)('A' + i)}")
                .ToList();
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

        // Checks if a number is a power of 2
        private bool IsPowerOfTwo(int n) =>
            n > 0 && (n & (n - 1)) == 0;
    }
}