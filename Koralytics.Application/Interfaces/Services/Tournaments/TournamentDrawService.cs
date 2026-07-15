using TournamentEntity = Koralytics.Domain.Entities.Tournamet.Tournament;
using TournamentTeamEntity = Koralytics.Domain.Entities.Tournamet.TournamentTeam;
using TournamentGroupEntity = Koralytics.Domain.Entities.Tournamet.TournamentGroup;
using TournamentRoundEntity = Koralytics.Domain.Entities.Tournamet.TournamentRound;
using TournamentFixtureEntity = Koralytics.Domain.Entities.Tournamet.TournamentFixture;
using TournamentGroupTeamEntity = Koralytics.Domain.Entities.Tournamet.TournamentGroupTeam;
using TournamentStandingEntity = Koralytics.Domain.Entities.Tournamet.TournamentStanding;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Tournament;
using Koralytics.Application.Services.Player.PlayerCardService;
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
        private readonly IPlayerCardService _playerCardService;
        private readonly Random _random = new();

        public TournamentDrawService(
            IUnitOfWork unitOfWork,
            ILogger<TournamentDrawService> logger,
            IPlayerCardService playerCardService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _playerCardService = playerCardService;
        }

        // ─────────────────────────────────────────────────────────────
        // SEEDING
        // ─────────────────────────────────────────────────────────────

        public async Task GenerateSeedingAsync(int tournamentId)
        {
            _logger.LogInformation(
                "Generating seeding for tournament {TournamentId}", tournamentId);

            var tournament = await _unitOfWork.Repository<TournamentEntity>()
                .FindAsync(t => t.Id == tournamentId);

            if (tournament is null)
                throw new NotFoundException(
                    $"Tournament with Id {tournamentId} not found");

            if (tournament.Status != TournamentStatus.Registration)
                throw new BadRequestException(
                    "Seeding can only be generated during Registration status");

            var tournamentTeams = await _unitOfWork
                .Repository<TournamentTeamEntity>()
                .GetQueryable()
                .Include(tt => tt.Team)
                .Where(tt =>
                    tt.TournamentId == tournamentId &&
                    tt.Status == TournamentTeamStatus.Accepted)
                .ToListAsync();

            if (tournamentTeams.Count < 2)
                throw new BadRequestException(
                    "At least 2 accepted teams are required to generate seeding");

            var teamIds = tournamentTeams.Select(tt => tt.TeamId).ToList();

            // ── FIX: Bulk fetch 1 — all completed fixtures for ALL teams ──
            // Before: one query per team inside CalculateWinRateAsync
            // After:  one query for all teams combined
            var allFixtures = await _unitOfWork
                .Repository<TournamentFixtureEntity>()
                .GetQueryable()
                .Where(f =>
                    (teamIds.Contains(f.HomeTeamId) ||
                     teamIds.Contains(f.AwayTeamId)) &&
                    f.Status == MatchStatus.Completed)
                .ToListAsync();

            // ── FIX: Bulk fetch 2 — all active players for ALL teams ──
            // Before: one query per team inside CalculateAveragePlayerRatingAsync
            // After:  one query for all teams combined
            var allTeamPlayers = await _unitOfWork
                .Repository<Domain.Entities.Player.PlayerTeam>()
                .GetQueryable()
                .Where(pt => teamIds.Contains(pt.TeamId) && pt.LeftAt == null)
                .Select(pt => new { pt.TeamId, pt.PlayerId })
                .ToListAsync();

            // ── FIX: Bulk fetch 3 — previous knockout wins for ALL teams ──
            // Before: one query per team inside CalculatePreviousTournamentScoreAsync
            // After:  one grouped query for all teams combined
            var previousWinCounts = await _unitOfWork
                .Repository<TournamentFixtureEntity>()
                .GetQueryable()
                .Where(f =>
                    f.WinnerTeamId.HasValue &&
                    teamIds.Contains(f.WinnerTeamId.Value) &&
                    f.Round != null &&
                    f.Status == MatchStatus.Completed)
                .GroupBy(f => f.WinnerTeamId!.Value)
                .Select(g => new { TeamId = g.Key, Wins = g.Count() })
                .ToListAsync();

            var winCountLookup = previousWinCounts
                .ToDictionary(x => x.TeamId, x => x.Wins);

            // ── FIX: Fetch all player cards upfront ──
            // Before: N calls inside foreach per team
            // After:  one call per unique player across all teams
            var uniquePlayerIds = allTeamPlayers
                .Select(p => p.PlayerId)
                .Distinct()
                .ToList();

            var playerRatings = new Dictionary<int, double>();
            foreach (var playerId in uniquePlayerIds)
            {
                try
                {
                    var card = await _playerCardService
                        .GetPlayerCardAsync(playerId);
                    if (card != null)
                        playerRatings[playerId] = (double)card.OverallRating;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        "Could not get card for player {PlayerId}: {Message}",
                        playerId, ex.Message);
                }
            }

            // ── Calculate all scores in memory — zero additional DB queries ──
            var seedScores = new List<(TournamentTeamEntity Team, double Score)>();

            foreach (var tournamentTeam in tournamentTeams)
            {
                var teamId = tournamentTeam.TeamId;

                // Win rate — from pre-fetched fixtures
                var teamFixtures = allFixtures
                    .Where(f =>
                        f.HomeTeamId == teamId ||
                        f.AwayTeamId == teamId)
                    .ToList();

                double winRate = teamFixtures.Count == 0
                    ? 0
                    : (double)teamFixtures.Count(f => f.WinnerTeamId == teamId)
                      / teamFixtures.Count;

                // Player rating — from pre-fetched player data + cached cards
                var teamPlayerIds = allTeamPlayers
                    .Where(p => p.TeamId == teamId)
                    .Select(p => p.PlayerId)
                    .ToList();

                double playerRating = teamPlayerIds.Count == 0
                    ? 0
                    : teamPlayerIds
                        .Where(pid => playerRatings.ContainsKey(pid))
                        .Select(pid => playerRatings[pid])
                        .DefaultIfEmpty(0)
                        .Average() / 10.0;

                // Previous results — from pre-fetched win counts
                var previousWins = winCountLookup.GetValueOrDefault(teamId, 0);
                double previousResults = Math.Min(previousWins / 10.0, 1.0);

                // Composite score
                double score =
                    (winRate * 0.4) +
                    (playerRating * 0.4) +
                    (previousResults * 0.2);

                seedScores.Add((tournamentTeam, score));
            }

            // Order and assign seed numbers
            var ordered = seedScores
                .OrderByDescending(x => x.Score)
                .ToList();

            for (int i = 0; i < ordered.Count; i++)
                ordered[i].Team.SeedNumber = i + 1;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Seeding generated for {Count} teams in tournament {TournamentId}",
                ordered.Count, tournamentId);
        }

        // ─────────────────────────────────────────────────────────────
        // DRAW GENERATION
        // ─────────────────────────────────────────────────────────────

        public async Task GenerateDrawAsync(int tournamentId)
        {
            _logger.LogInformation(
                "Generating draw for tournament {TournamentId}", tournamentId);

            var tournament = await _unitOfWork.Repository<TournamentEntity>()
                .FindAsync(t => t.Id == tournamentId);

            if (tournament is null)
                throw new NotFoundException(
                    $"Tournament with Id {tournamentId} not found");

            if (tournament.Status != TournamentStatus.Registration)
                throw new BadRequestException(
                    "Draw can only be generated during Registration status");

            var drawExists = await _unitOfWork.Repository<TournamentRoundEntity>()
                .ExistsAsync(r => r.TournamentId == tournamentId);

            var groupsExist = await _unitOfWork.Repository<TournamentGroupEntity>()
                .ExistsAsync(g =>
                    g.TournamentId == tournamentId &&
                    !g.IsDummy);

            if (drawExists || groupsExist)
                throw new ConflictException(
                    "Draw has already been generated for this tournament");

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

            if (seededTeams.Any(tt => tt.SeedNumber == null))
                throw new BadRequestException(
                    "All teams must be seeded before generating the draw. " +
                    "Run GenerateSeedingAsync first");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                switch (tournament.Structure)
                {
                    case TournamentStructure.Knockout:
                        await GenerateKnockoutDrawAsync(tournament, seededTeams);
                        break;
                    case TournamentStructure.GroupAndKnockout:
                        await GenerateGroupAndKnockoutDrawAsync(
                            tournament, seededTeams);
                        break;
                    case TournamentStructure.League:
                        await GenerateLeagueDrawAsync(tournament, seededTeams);
                        break;
                    default:
                        throw new BadRequestException(
                            $"Unknown tournament structure: {tournament.Structure}");
                }

                tournament.Status = TournamentStatus.InProgress;
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            _logger.LogInformation(
                "Draw generated successfully for tournament {TournamentId}",
                tournamentId);
        }

        // ─────────────────────────────────────────────────────────────
        // KNOCKOUT
        // ─────────────────────────────────────────────────────────────

        private async Task GenerateKnockoutDrawAsync(
            TournamentEntity tournament,
            List<TournamentTeamEntity> seededTeams)
        {
            if (!IsPowerOfTwo(seededTeams.Count))
                throw new BadRequestException(
                    $"Knockout tournaments require a power of 2 number of teams " +
                    $"(2, 4, 8, 16). Got {seededTeams.Count}");

            var round = new TournamentRoundEntity
            {
                TournamentId = tournament.Id,
                Name = GetRoundName(seededTeams.Count),
                RoundNumber = 1
            };

            await _unitOfWork.Repository<TournamentRoundEntity>().AddAsync(round);
            await _unitOfWork.SaveChangesAsync();

            var fixtures = PairTeamsBySeeding(seededTeams);

            foreach (var (home, away) in fixtures)
            {
                await _unitOfWork.Repository<TournamentFixtureEntity>()
                    .AddAsync(new TournamentFixtureEntity
                    {
                        RoundId = round.Id,
                        GroupId = null,
                        HomeTeamId = home.Id,
                        AwayTeamId = away.Id,
                        Status = MatchStatus.Scheduled,
                        LegNumber = tournament.HasTwoLegs ? 1 : null
                    });

                if (tournament.HasTwoLegs)
                    await _unitOfWork.Repository<TournamentFixtureEntity>()
                        .AddAsync(new TournamentFixtureEntity
                        {
                            RoundId = round.Id,
                            GroupId = null,
                            HomeTeamId = away.Id,
                            AwayTeamId = home.Id,
                            Status = MatchStatus.Scheduled,
                            LegNumber = 2
                        });
            }

            await _unitOfWork.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────────────────────
        // GROUP + KNOCKOUT
        // ─────────────────────────────────────────────────────────────

        private async Task GenerateGroupAndKnockoutDrawAsync(
            TournamentEntity tournament,
            List<TournamentTeamEntity> seededTeams)
        {
            int groupCount = Math.Max(2, seededTeams.Count / 4);

            if (seededTeams.Count < groupCount * 2)
                throw new BadRequestException(
                    $"Not enough teams for {groupCount} groups. " +
                    $"Need at least {groupCount * 2} teams");

            var groupNames = GenerateGroupNames(groupCount);
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

            var pots = CreateSeedingPots(seededTeams, groupCount);
            var groupAssignments = AssignTeamsToGroups(groups, pots);

            foreach (var (group, teams) in groupAssignments)
            {
                foreach (var team in teams)
                {
                    await _unitOfWork.Repository<TournamentGroupTeamEntity>()
                        .AddAsync(new TournamentGroupTeamEntity
                        {
                            GroupId = group.Id,
                            TournamentTeamId = team.Id
                        });

                    await _unitOfWork.Repository<TournamentStandingEntity>()
                        .AddAsync(new TournamentStandingEntity
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
                        });
                }

                var teamList = groupAssignments[group];
                for (int i = 0; i < teamList.Count; i++)
                {
                    for (int j = i + 1; j < teamList.Count; j++)
                    {
                        await _unitOfWork.Repository<TournamentFixtureEntity>()
                            .AddAsync(new TournamentFixtureEntity
                            {
                                GroupId = group.Id,
                                RoundId = null,
                                HomeTeamId = teamList[i].Id,
                                AwayTeamId = teamList[j].Id,
                                Status = MatchStatus.Scheduled,
                                LegNumber = tournament.HasTwoLegs ? 1 : null
                            });

                        if (tournament.HasTwoLegs)
                            await _unitOfWork.Repository<TournamentFixtureEntity>()
                                .AddAsync(new TournamentFixtureEntity
                                {
                                    GroupId = group.Id,
                                    RoundId = null,
                                    HomeTeamId = teamList[j].Id,
                                    AwayTeamId = teamList[i].Id,
                                    Status = MatchStatus.Scheduled,
                                    LegNumber = 2
                                });
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────────────────────
        // LEAGUE
        // ─────────────────────────────────────────────────────────────

        private async Task GenerateLeagueDrawAsync(
            TournamentEntity tournament,
            List<TournamentTeamEntity> seededTeams)
        {
            var dummyGroup = await _unitOfWork.Repository<TournamentGroupEntity>()
                .FindAsync(g =>
                    g.TournamentId == tournament.Id &&
                    g.IsDummy == true);

            if (dummyGroup is null)
                throw new NotFoundException(
                    "League dummy group not found");

            foreach (var team in seededTeams)
            {
                await _unitOfWork.Repository<TournamentGroupTeamEntity>()
                    .AddAsync(new TournamentGroupTeamEntity
                    {
                        GroupId = dummyGroup.Id,
                        TournamentTeamId = team.Id
                    });

                await _unitOfWork.Repository<TournamentStandingEntity>()
                    .AddAsync(new TournamentStandingEntity
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
                    });
            }

            await _unitOfWork.SaveChangesAsync();

            for (int i = 0; i < seededTeams.Count; i++)
            {
                for (int j = i + 1; j < seededTeams.Count; j++)
                {
                    await _unitOfWork.Repository<TournamentFixtureEntity>()
                        .AddAsync(new TournamentFixtureEntity
                        {
                            GroupId = dummyGroup.Id,
                            RoundId = null,
                            HomeTeamId = seededTeams[i].Id,
                            AwayTeamId = seededTeams[j].Id,
                            Status = MatchStatus.Scheduled,
                            LegNumber = tournament.HasTwoLegs ? 1 : null
                        });

                    if (tournament.HasTwoLegs)
                        await _unitOfWork.Repository<TournamentFixtureEntity>()
                            .AddAsync(new TournamentFixtureEntity
                            {
                                GroupId = dummyGroup.Id,
                                RoundId = null,
                                HomeTeamId = seededTeams[j].Id,
                                AwayTeamId = seededTeams[i].Id,
                                Status = MatchStatus.Scheduled,
                                LegNumber = 2
                            });
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────

        private List<(TournamentTeamEntity, TournamentTeamEntity)>
            PairTeamsBySeeding(List<TournamentTeamEntity> seededTeams)
        {
            var pairs = new List<(TournamentTeamEntity, TournamentTeamEntity)>();
            int left = 0, right = seededTeams.Count - 1;

            while (left < right)
            {
                pairs.Add((seededTeams[left], seededTeams[right]));
                left++;
                right--;
            }

            return pairs;
        }

        private List<List<TournamentTeamEntity>> CreateSeedingPots(
            List<TournamentTeamEntity> seededTeams, int groupCount)
        {
            var pots = new List<List<TournamentTeamEntity>>();
            int potSize = seededTeams.Count / groupCount;

            for (int i = 0; i < groupCount; i++)
                pots.Add(seededTeams.Skip(i * potSize).Take(potSize).ToList());

            return pots;
        }

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
                var shuffled = pot.OrderBy(_ => _random.Next()).ToList();
                for (int i = 0; i < groups.Count && i < shuffled.Count; i++)
                    assignments[groups[i]].Add(shuffled[i]);
            }

            return assignments;
        }

        private List<string> GenerateGroupNames(int count) =>
            Enumerable.Range(0, count)
                .Select(i => $"Group {(char)('A' + i)}")
                .ToList();

        private string GetRoundName(int teamCount) => teamCount switch
        {
            2 => "Final",
            4 => "Semi-Final",
            8 => "Quarter-Final",
            16 => "Round of 16",
            _ => $"Round of {teamCount}"
        };

        private bool IsPowerOfTwo(int n) =>
            n > 0 && (n & (n - 1)) == 0;
    }
}