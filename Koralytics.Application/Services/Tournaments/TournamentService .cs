using AutoMapper;
using TournamentEntity = Koralytics.Domain.Entities.Tournamet.Tournament;
using TournamentGroupEntity = Koralytics.Domain.Entities.Tournamet.TournamentGroup;
using TournamentTeamEntity = Koralytics.Domain.Entities.Tournamet.TournamentTeam;
using TournamentSquadEntity = Koralytics.Domain.Entities.Tournamet.TournamentSquad;
using PlayerTeamEntity = Koralytics.Domain.Entities.Player.PlayerTeam;
using AcademyEntity = Koralytics.Domain.Entities.Academy.Academy;
using Koralytics.Application.DTOs.Tournament;
using Koralytics.Application.DTOs.Tournaments;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Tournament;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Koralytics.Domain.Entities.Match;
using Koralytics.Domain.Entities.Identity;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Entities.Drill;
using Koralytics.Application.Interfaces.Tournaments;
using System;
using System.Collections.Generic;
using System.Linq;

using PlayerEntity = Koralytics.Domain.Entities.Player.Player;
using MatchEntity = Koralytics.Domain.Entities.Match.Match;
using TournamentFixtureEntity = Koralytics.Domain.Entities.Tournamet.TournamentFixture;
using TournamentRoundEntity = Koralytics.Domain.Entities.Tournamet.TournamentRound;
using TournamentStandingEntity = Koralytics.Domain.Entities.Tournamet.TournamentStanding;

namespace Koralytics.Application.Services.Tournaments
{
    public class TournamentService : ITournamentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<TournamentService> _logger;
        private readonly ITournamentDrawService _tournamentDrawService;
        private readonly ITournamentFixtureService _tournamentFixtureService;
        private readonly ITournamentReportService _tournamentReportService;
        private readonly IBackgroundTaskQueue _taskQueue;

        public TournamentService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<TournamentService> logger,
            ITournamentDrawService tournamentDrawService,
            ITournamentFixtureService tournamentFixtureService,
            ITournamentReportService tournamentReportService,
            IBackgroundTaskQueue taskQueue)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _tournamentDrawService = tournamentDrawService;
            _tournamentFixtureService = tournamentFixtureService;
            _tournamentReportService = tournamentReportService;
            _taskQueue = taskQueue;
        }

        public async Task<IEnumerable<TournamentDto>> GetAllAsync()
        {
            var tournaments = await _unitOfWork.Repository<TournamentEntity>()
                .GetQueryableAsNoTracking()
                .ToListAsync();

            return _mapper.Map<IEnumerable<TournamentDto>>(tournaments);
        }

        public async Task<TournamentDto?> GetByIdAsync(int id)
        {
            var tournament = await _unitOfWork.Repository<TournamentEntity>()
                .GetQueryableAsNoTracking()
                .Include(t => t.AgeGroup)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tournament is null)
                return null;

            return _mapper.Map<TournamentDto>(tournament);
        }

        public async Task<List<TournamentTeamDto>> GetTeamsAsync(int tournamentId)
        {
            var teams = await _unitOfWork.Repository<TournamentTeamEntity>()
                .GetQueryableAsNoTracking()
                .Include(tt => tt.Team)
                .ThenInclude(t => t.AgeGroup)
                .ThenInclude(ag => ag.Academy)
                .Where(tt => tt.TournamentId == tournamentId)
                .Select(tt => new TournamentTeamDto
                {
                    TournamentTeamId = tt.Id,
                    TournamentId = tt.TournamentId,
                    TournamentName = tt.Tournament.Name,
                    TeamId = tt.TeamId,
                    TeamName = tt.Team.Name,
                    AcademyId = tt.Team.AgeGroup.Academy.Id,
                    AcademyName = tt.Team.AgeGroup.Academy.Name,
                    Status = tt.Status,
                    SeedNumber = tt.SeedNumber,
                    RegisteredAt = tt.RegisteredAt
                })
                .ToListAsync();

            return teams;
        }

        public async Task<List<TournamentTeamDto>> GetInvitationsForAcademyAsync(int academyId)
        {
            var teams = await _unitOfWork.Repository<TournamentTeamEntity>()
                .GetQueryableAsNoTracking()
                .Include(tt => tt.Team)
                .ThenInclude(t => t.AgeGroup)
                .ThenInclude(ag => ag.Academy)
                .Include(tt => tt.Tournament)
                .Where(tt => tt.Team.AgeGroup.Academy.Id == academyId)
                .Select(tt => new TournamentTeamDto
                {
                    TournamentTeamId = tt.Id,
                    TournamentId = tt.TournamentId,
                    TournamentName = tt.Tournament.Name,
                    TeamId = tt.TeamId,
                    TeamName = tt.Team.Name,
                    AcademyId = tt.Team.AgeGroup.Academy.Id,
                    AcademyName = tt.Team.AgeGroup.Academy.Name,
                    Status = tt.Status,
                    SeedNumber = tt.SeedNumber,
                    RegisteredAt = tt.RegisteredAt
                })
                .ToListAsync();

            return teams;
        }

        public async Task<List<int>> GetRegisteredPlayerIdsAsync(int tournamentId, int teamId)
        {
            var playerIds = await _unitOfWork.Repository<TournamentSquadEntity>()
                .GetQueryableAsNoTracking()
                .Where(ts => ts.TournamentId == tournamentId && ts.TeamId == teamId)
                .Select(ts => ts.PlayerId)
                .ToListAsync();

            return playerIds;
        }

        public async Task<TournamentDto> CreateTournamentAsync(
            CreateTournamentDto dto, int requestingUserId)
        {
            _logger.LogInformation(
                "User {UserId} attempting to create tournament {Name}",
                requestingUserId, dto.Name);

            var ageGroup = await _unitOfWork.Repository<AgeGroup>()
                .FindAsync(a => a.Id == dto.AgeGroupId);

            if (ageGroup is null)
                throw new NotFoundException(
                    $"AgeGroup with Id {dto.AgeGroupId} not found");

            if (dto.EndDate <= dto.StartDate)
                throw new BadRequestException(
                    "EndDate must be after StartDate");

            var nameExists = await _unitOfWork.Repository<TournamentEntity>()
                .ExistsAsync(t => t.Name == dto.Name);

            if (nameExists)
                throw new ConflictException(
                    $"Tournament with name '{dto.Name}' already exists");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var tournament = _mapper.Map<TournamentEntity>(dto);
                tournament.Status = TournamentStatus.Draft;

                await _unitOfWork.Repository<TournamentEntity>()
                    .AddAsync(tournament);
                await _unitOfWork.SaveChangesAsync();

                if (dto.Structure == TournamentStructure.League)
                {
                    var dummyGroup = new TournamentGroupEntity
                    {
                        TournamentId = tournament.Id,
                        Name = "League",
                        IsDummy = true
                    };
                    await _unitOfWork.Repository<TournamentGroupEntity>()
                        .AddAsync(dummyGroup);
                    await _unitOfWork.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                var createdTournamentId = tournament.Id;
                var createdTournamentName = tournament.Name;
                _taskQueue.QueueBackgroundWorkItem(async (sp, ct) =>
                {
                    try
                    {
                        var indexer = sp.GetRequiredService<ISearchableEntityIndexer>();
                        await indexer.IndexAsync("Tournament", createdTournamentId, createdTournamentName, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to index embedding for Tournament {TournamentId} ({Name})", createdTournamentId, createdTournamentName);
                    }
                });

                var created = await _unitOfWork.Repository<TournamentEntity>()
                    .GetQueryable()
                    .Include(t => t.AgeGroup)
                    .FirstOrDefaultAsync(t => t.Id == tournament.Id);

                _logger.LogInformation(
                    "Tournament {Name} created successfully with Id {Id}",
                    tournament.Name, tournament.Id);

                return _mapper.Map<TournamentDto>(created!);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task InviteAcademyAsync(int tournamentId, int academyId)
        {
            _logger.LogInformation(
                "Inviting academy {AcademyId} to tournament {TournamentId}",
                academyId, tournamentId);

            var tournament = await _unitOfWork.Repository<TournamentEntity>()
                .FindAsync(t => t.Id == tournamentId);

            if (tournament is null)
                throw new NotFoundException(
                    $"Tournament with Id {tournamentId} not found");

            if (tournament.Status != TournamentStatus.Registration)
                throw new BadRequestException(
                    "Tournament must be in Registration status to invite teams");

            var academy = await _unitOfWork.Repository<AcademyEntity>()
                .FindAsync(a => a.Id == academyId);

            if (academy is null)
                throw new NotFoundException(
                    $"Academy with Id {academyId} not found");

            var alreadyInvited = await _unitOfWork.Repository<TournamentTeamEntity>()
                .ExistsAsync(tt =>
                    tt.TournamentId == tournamentId &&
                    tt.Team.AcademyId == academyId);

            if (alreadyInvited)
                throw new ConflictException(
                    "This academy is already invited to the tournament");

            var tournamentAgeGroup = await _unitOfWork.Repository<AgeGroup>()
                .FindAsync(a => a.Id == tournament.AgeGroupId);

            if (tournamentAgeGroup == null)
                throw new NotFoundException("Tournament age group not found");

            var team = await _unitOfWork.Repository<Team>()
                .GetQueryable()
                .Include(t => t.AgeGroup)
                .FirstOrDefaultAsync(t =>
                    t.AcademyId == academyId &&
                    t.AgeGroup.MinAge == tournamentAgeGroup.MinAge &&
                    t.AgeGroup.MaxAge == tournamentAgeGroup.MaxAge);

            if (team is null)
                throw new NotFoundException(
                    $"Academy {academyId} has no team matching the tournament's age criteria ({tournamentAgeGroup.MinAge}-{tournamentAgeGroup.MaxAge})");

            var tournamentTeam = new TournamentTeamEntity
            {
                TournamentId = tournamentId,
                TeamId = team.Id,
                Status = TournamentTeamStatus.Invited,
                RegisteredAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<TournamentTeamEntity>()
                .AddAsync(tournamentTeam);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Academy {AcademyId} successfully invited to tournament {TournamentId}",
                academyId, tournamentId);
        }

        public async Task AcceptInvitationAsync(int tournamentId, int academyId, int? teamId = null)
        {
            _logger.LogInformation(
                "Academy {AcademyId} accepting invitation for tournament {TournamentId} (selected TeamId: {TeamId})",
                academyId, tournamentId, teamId);

            var tournamentTeam = await _unitOfWork.Repository<TournamentTeamEntity>()
                .GetQueryable()
                .Include(tt => tt.Team)
                .ThenInclude(t => t.AgeGroup)
                .FirstOrDefaultAsync(tt =>
                    tt.TournamentId == tournamentId &&
                    tt.Team.AcademyId == academyId);

            if (tournamentTeam is null)
                throw new NotFoundException(
                    "Tournament invitation not found for this academy");

            if (tournamentTeam.Status != TournamentTeamStatus.Invited)
                throw new BadRequestException(
                    "Academy has already responded to this invitation");

            if (teamId.HasValue && teamId.Value != tournamentTeam.TeamId)
            {
                var selectedTeam = await _unitOfWork.Repository<Team>()
                    .GetQueryable()
                    .Include(t => t.AgeGroup)
                    .FirstOrDefaultAsync(t => t.Id == teamId.Value && t.AcademyId == academyId);

                if (selectedTeam is null)
                    throw new BadRequestException("Selected team does not belong to your academy.");

                tournamentTeam.TeamId = selectedTeam.Id;
            }

            tournamentTeam.Status = TournamentTeamStatus.Accepted;
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Academy {AcademyId} accepted invitation for tournament {TournamentId} using team {TeamId}",
                academyId, tournamentId, tournamentTeam.TeamId);
        }

        public async Task RegisterSquadAsync(
            int tournamentId, int teamId, List<int> playerIds)
        {
            _logger.LogInformation(
                "Registering squad for team {TeamId} in tournament {TournamentId}",
                teamId, tournamentId);

            var tournament = await _unitOfWork.Repository<TournamentEntity>()
                .FindAsync(t => t.Id == tournamentId);

            if (tournament is null)
                throw new NotFoundException(
                    $"Tournament with Id {tournamentId} not found");

            if (tournament.Status != TournamentStatus.Registration)
                throw new BadRequestException(
                    "Tournament must be in Registration status to register squad");

            var tournamentTeam = await _unitOfWork.Repository<TournamentTeamEntity>()
                .FindAsync(tt =>
                    tt.TournamentId == tournamentId &&
                    tt.TeamId == teamId &&
                    tt.Status == TournamentTeamStatus.Accepted);

            if (tournamentTeam is null)
                throw new BadRequestException(
                    "Team must accept the tournament invitation before registering a squad");

            if (playerIds.Count != playerIds.Distinct().Count())
                throw new BadRequestException(
                    "Duplicate players found in squad registration");

            var minPlayers = (int)tournament.Format;
            if (playerIds.Count < minPlayers)
                throw new BadRequestException(
                    $"Squad must have at least {minPlayers} players " +
                    $"for {tournament.Format} format");

            var maxPlayers = (int)tournament.Format + 5;
            if (playerIds.Count > maxPlayers)
                throw new BadRequestException(
                    $"Squad exceeds maximum allowed players " +
                    $"for {tournament.Format} format");

            // ── FIX: Bulk query 1 — fetch all team members in one query ──
            // Before: ExistsAsync per player = N queries
            // After:  one query fetching all player IDs for this team
            var teamPlayerIds = await _unitOfWork.Repository<PlayerTeamEntity>()
                .GetQueryable()
                .Where(pt => pt.TeamId == teamId && pt.LeftAt == null)
                .Select(pt => pt.PlayerId)
                .ToListAsync();

            // ── FIX: Bulk query 2 — fetch all registered players in one query ──
            // Before: ExistsAsync per player = N queries
            // After:  one query fetching all already-registered player IDs
            var registeredPlayerIds = await _unitOfWork
                .Repository<TournamentSquadEntity>()
                .GetQueryable()
                .Where(ts => ts.TournamentId == tournamentId)
                .Select(ts => ts.PlayerId)
                .ToListAsync();

            // ── Validate in memory — zero additional DB hits ──
            var notInTeam = playerIds.Except(teamPlayerIds).ToList();
            if (notInTeam.Any())
                throw new BadRequestException(
                    $"Players {string.Join(", ", notInTeam)} " +
                    $"do not belong to team {teamId}");

            var alreadyRegistered = playerIds.Intersect(registeredPlayerIds).ToList();
            if (alreadyRegistered.Any())
                throw new ConflictException(
                    $"Players {string.Join(", ", alreadyRegistered)} " +
                    $"are already registered in this tournament");

            var squadRecords = playerIds.Select(playerId =>
                new TournamentSquadEntity
                {
                    TournamentId = tournamentId,
                    TeamId = teamId,
                    PlayerId = playerId,
                    RegisteredAt = DateTime.UtcNow
                }).ToList();

            await _unitOfWork.Repository<TournamentSquadEntity>()
                .AddRangeAsync(squadRecords);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Successfully registered {Count} players for team {TeamId} " +
                "in tournament {TournamentId}",
                playerIds.Count, teamId, tournamentId);
        }

        public async Task UpdateStatusAsync(
            int tournamentId, TournamentStatus status)
        {
            var tournament = await _unitOfWork.Repository<TournamentEntity>()
                .FindAsync(t => t.Id == tournamentId);

            if (tournament is null)
                throw new NotFoundException(
                    $"Tournament with Id {tournamentId} not found");

            tournament.Status = status;
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Tournament {Id} status updated to {Status}",
                tournamentId, status);
        }

        public async Task SimulateTournamentAsync(int tournamentId)
        {
            _logger.LogInformation("Simulating tournament {TournamentId} with mock data", tournamentId);

            var tournament = await _unitOfWork.Repository<TournamentEntity>()
                .FindAsync(t => t.Id == tournamentId);

            if (tournament is null)
                throw new NotFoundException($"Tournament with Id {tournamentId} not found");

            if (tournament.Status == TournamentStatus.Draft || tournament.Status == TournamentStatus.Registration)
            {
                var acceptedTeamsCount = await _unitOfWork.Repository<TournamentTeamEntity>()
                    .CountAsync(tt => tt.TournamentId == tournamentId && tt.Status == TournamentTeamStatus.Accepted);

                if (acceptedTeamsCount < 4)
                {
                    var academy = await _unitOfWork.Repository<AcademyEntity>().GetQueryable().FirstOrDefaultAsync();
                    if (academy == null)
                    {
                        academy = new AcademyEntity { Name = "أكاديمية كوراليتيكس للناشئين", Status = AcademyStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                        await _unitOfWork.Repository<AcademyEntity>().AddAsync(academy);
                        await _unitOfWork.SaveChangesAsync();
                    }

                    var ageGroup = await _unitOfWork.Repository<AgeGroup>().GetQueryable().FirstOrDefaultAsync(ag => ag.AcademyId == academy.Id && ag.Name == "U17");
                    if (ageGroup == null)
                    {
                        ageGroup = new AgeGroup { AcademyId = academy.Id, Name = "U17", MinAge = 15, MaxAge = 17 };
                        await _unitOfWork.Repository<AgeGroup>().AddAsync(ageGroup);
                        await _unitOfWork.SaveChangesAsync();
                    }

                    var location = await _unitOfWork.Repository<AcademyLocation>().GetQueryable().FirstOrDefaultAsync(al => al.AcademyId == academy.Id);
                    if (location == null)
                    {
                        location = new AcademyLocation { AcademyId = academy.Id, Name = "فرع القاهرة الرئيسي", Address = "مدينة نصر", City = "القاهرة", IsMain = true };
                        await _unitOfWork.Repository<AcademyLocation>().AddAsync(location);
                        await _unitOfWork.SaveChangesAsync();
                    }

                    var teamNames = new string[] {
                        "النادي الأهلي (U17)", "نادي الزمالك (U17)", "نادي بيراميدز (U17)", "النادي المصري (U17)",
                        "نادي الاتحاد السكندري (U17)", "نادي الإسماعيلي (U17)", "نادي سموحة (U17)", "نادي سيراميكا كليوباترا (U17)"
                    };
                    var playerNames = new string[][] {
                        new string[] {"محمد الشناوي", "محمد هاني", "ياسر إبراهيم", "رامي ربيعة", "علي معلول", "مروان عطية", "إمام عاشور", "أكرم توفيق", "حسين الشحات", "بيرسي تاو", "محمود كهربا"},
                        new string[] {"محمد صبحي", "عمر جابر", "حمزة المثلوثي", "حسام عبد المجيد", "أحمد فتوح", "نبيل عماد دنجا", "أحمد مصطفى زيزو", "عبد الله السعيد", "شيكابالا", "مصطفى شلبي", "سيف الجزيري"},
                        new string[] {"شريف إكرامي", "محمد الشيبي", "علي جبر", "أحمد سامي", "محمد حمدي", "بلاتي توريه", "مهند لاشين", "مصطفى فتحي", "وليد الكرتي", "رمضان صبحي", "فيستون ماييلي"},
                        new string[] {"محمود جاد", "كريم العراقي", "باهر المحمدي", "عمرو موسى", "حسين السيد", "محمود حمادة", "معتز زدام", "ميدو جابر", "سمير فكري", "صلاح محسن", "فخر الدين بن يوسف"},
                        new string[] {"المهدي سليمان", "هشام صلاح", "سيف تقا", "مصطفى إبراهيم", "كريم الديب", "خالد الغندور", "مورو ساليفو", "عبد الغني محمد", "أحمد عادل ميسي", "مابولولو", "بواتينج"},
                        new string[] {"أحمد عادل عبد المنعم", "محمد دسوقي", "محمد نصر", "محمد عمار", "حمدي النقاز", "عماد حمدي", "عمر الساعي", "عبد الرحمن مجدي", "إيريك تراوري", "نادر فرج", "خالد النبريص"},
                        new string[] {"الهاني سليمان", "شريف رضا", "ميدو مصطفى", "أحمد حكم", "طارق علاء", "عمرو قلاوة", "أبو بكر ليادي", "فادي فريد", "مصطفى البدري", "حسام حسن", "لحسن دحدوح"},
                        new string[] {"محمد بسام", "أحمد هاني", "رجب نبيل", "أحمد رمضان بيكهام", "محمد شكري", "محمد إبراهيم", "أحمد القندوسي", "صديق أوجولا", "محمد عادل", "جون إيبوكا", "أحمد ياسر ريان"}
                    };

                    for (int i = 0; i < teamNames.Length; i++)
                    {
                        var tName = teamNames[i];
                        var team = await _unitOfWork.Repository<Team>().GetQueryable().FirstOrDefaultAsync(t => t.AcademyId == academy.Id && t.Name == tName);
                        if (team == null)
                        {
                            team = new Team { AcademyId = academy.Id, AgeGroupId = ageGroup.Id, LocationId = location.Id, Name = tName };
                            await _unitOfWork.Repository<Team>().AddAsync(team);
                            await _unitOfWork.SaveChangesAsync();
                        }

                        var ptCount = await _unitOfWork.Repository<PlayerTeamEntity>().CountAsync(pt => pt.TeamId == team.Id && pt.LeftAt == null);
                        if (ptCount < 11)
                        {
                            var pNames = playerNames[i];
                            for (int j = 0; j < pNames.Length; j++)
                            {
                                var fullName = pNames[j];
                                var parts = fullName.Split(' ');
                                var firstN = parts[0];
                                var lastN = parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : "لاعب";
                                var email = $"mock_{i}_{j}_{Guid.NewGuid().ToString().Substring(0, 5)}@koralytics.com";

                                var player = new PlayerEntity
                                {
                                    UserName = email,
                                    Email = email,
                                    EmailConfirmed = true,
                                    FirstName = firstN,
                                    LastName = lastN,
                                    DateOfBirth = new DateTime(2008, 1, 1),
                                    PreferredFoot = j % 3 == 0 ? PreferredFoot.Left : PreferredFoot.Right,
                                    WeakFootRating = 3,
                                    AvailabilityStatus = AvailabilityStatus.Available,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                };
                                await _unitOfWork.Repository<PlayerEntity>().AddAsync(player);
                                await _unitOfWork.SaveChangesAsync();

                                var pos = j == 0 ? "GK" : (j < 5 ? "CB" : (j < 8 ? "CM" : "ST"));
                                var playerPos = new PlayerPosition { PlayerId = player.Id, Position = pos, IsPrimary = true };
                                await _unitOfWork.Repository<PlayerPosition>().AddAsync(playerPos);

                                var playerAc = new PlayerAcademy { PlayerId = player.Id, AcademyId = academy.Id, JoinedAt = DateTime.UtcNow, Status = PlayerAcademyStatus.Active };
                                await _unitOfWork.Repository<PlayerAcademy>().AddAsync(playerAc);

                                var playerTeam = new PlayerTeamEntity { PlayerId = player.Id, TeamId = team.Id, JoinedAt = DateTime.UtcNow };
                                await _unitOfWork.Repository<PlayerTeamEntity>().AddAsync(playerTeam);
                                await _unitOfWork.SaveChangesAsync();
                            }
                        }

                        var isRegistered = await _unitOfWork.Repository<TournamentTeamEntity>().ExistsAsync(tt => tt.TournamentId == tournamentId && tt.TeamId == team.Id);
                        if (!isRegistered)
                        {
                            var tt = new TournamentTeamEntity { TournamentId = tournamentId, TeamId = team.Id, Status = TournamentTeamStatus.Accepted, RegisteredAt = DateTime.UtcNow };
                            await _unitOfWork.Repository<TournamentTeamEntity>().AddAsync(tt);
                            await _unitOfWork.SaveChangesAsync();
                        }

                        var activePlayers = await _unitOfWork.Repository<PlayerTeamEntity>().GetQueryable().Where(pt => pt.TeamId == team.Id && pt.LeftAt == null).Select(pt => pt.PlayerId).ToListAsync();
                        foreach (var pid in activePlayers)
                        {
                            var inSquad = await _unitOfWork.Repository<TournamentSquadEntity>().ExistsAsync(ts => ts.TournamentId == tournamentId && ts.PlayerId == pid);
                            if (!inSquad)
                            {
                                var squad = new TournamentSquadEntity { TournamentId = tournamentId, TeamId = team.Id, PlayerId = pid, RegisteredAt = DateTime.UtcNow };
                                await _unitOfWork.Repository<TournamentSquadEntity>().AddAsync(squad);
                            }
                        }
                        await _unitOfWork.SaveChangesAsync();
                    }
                }

                tournament.Status = TournamentStatus.Registration;
                await _unitOfWork.SaveChangesAsync();

                await _tournamentDrawService.GenerateSeedingAsync(tournamentId);
                await _tournamentDrawService.GenerateDrawAsync(tournamentId);
            }

            var drillCategories = await _unitOfWork.Repository<DrillCategory>().GetQueryableAsNoTracking().ToListAsync();
            var systemAdmin = await _unitOfWork.Repository<User>().GetQueryableAsNoTracking().FirstOrDefaultAsync(u => u.Email == "admin@koralytics.com");
            int coachUserId = systemAdmin?.Id ?? 1;
            var random = new Random();

            while (true)
            {
                var currentTournament = await _unitOfWork.Repository<TournamentEntity>()
                    .FindAsync(t => t.Id == tournamentId);

                if (currentTournament == null || currentTournament.Status == TournamentStatus.Completed)
                    break;

                var fixtures = await _unitOfWork.Repository<TournamentFixtureEntity>()
                    .GetQueryable()
                    .Include(f => f.HomeTeam).ThenInclude(tt => tt.Team)
                    .Include(f => f.AwayTeam).ThenInclude(tt => tt.Team)
                    .Include(f => f.Group)
                    .Include(f => f.Round)
                    .Where(f => (f.GroupId != null ? f.Group.TournamentId == tournamentId : f.Round.TournamentId == tournamentId) && f.Status != MatchStatus.Completed)
                    .ToListAsync();

                if (fixtures.Count == 0)
                {
                    if (currentTournament.Structure == TournamentStructure.GroupAndKnockout)
                    {
                        var groups = await _unitOfWork.Repository<TournamentGroupEntity>()
                            .GetQueryable()
                            .Where(g => g.TournamentId == tournamentId)
                            .ToListAsync();

                        var hasGroupFixtures = await _unitOfWork.Repository<TournamentFixtureEntity>()
                            .ExistsAsync(f => f.GroupId != null && f.Group.TournamentId == tournamentId);

                        var hasRoundFixtures = await _unitOfWork.Repository<TournamentFixtureEntity>()
                            .ExistsAsync(f => f.RoundId != null && f.Round.TournamentId == tournamentId);

                        if (hasGroupFixtures && !hasRoundFixtures)
                        {
                            var groupTopTeams = new List<int>();
                            foreach (var g in groups)
                            {
                                var standings = await _unitOfWork.Repository<TournamentStandingEntity>()
                                    .GetQueryable()
                                    .Where(s => s.GroupId == g.Id)
                                    .OrderByDescending(s => s.Points)
                                    .ThenByDescending(s => s.GoalsFor - s.GoalsAgainst)
                                    .ThenByDescending(s => s.GoalsFor)
                                    .Take(2)
                                    .Select(s => s.TournamentTeamId)
                                    .ToListAsync();

                                groupTopTeams.AddRange(standings);
                            }

                            if (groupTopTeams.Count >= 4)
                            {
                                var nextRound = new TournamentRoundEntity
                                {
                                    TournamentId = tournamentId,
                                    RoundNumber = 1,
                                    Name = "Semi-Final"
                                };
                                await _unitOfWork.Repository<TournamentRoundEntity>().AddAsync(nextRound);
                                await _unitOfWork.SaveChangesAsync();

                                await _unitOfWork.Repository<TournamentFixtureEntity>().AddAsync(new TournamentFixtureEntity
                                {
                                    RoundId = nextRound.Id,
                                    GroupId = null,
                                    HomeTeamId = groupTopTeams[0],
                                    AwayTeamId = groupTopTeams[3],
                                    Status = MatchStatus.Scheduled
                                });

                                await _unitOfWork.Repository<TournamentFixtureEntity>().AddAsync(new TournamentFixtureEntity
                                {
                                    RoundId = nextRound.Id,
                                    GroupId = null,
                                    HomeTeamId = groupTopTeams[2],
                                    AwayTeamId = groupTopTeams[1],
                                    Status = MatchStatus.Scheduled
                                });

                                await _unitOfWork.Repository<TournamentFixtureEntity>().AddAsync(new TournamentFixtureEntity
                                {
                                    RoundId = nextRound.Id,
                                    GroupId = null,
                                    HomeTeamId = groupTopTeams[3],
                                    AwayTeamId = groupTopTeams[0],
                                    Status = MatchStatus.Scheduled,
                                    LegNumber = 2
                                });

                                await _unitOfWork.Repository<TournamentFixtureEntity>().AddAsync(new TournamentFixtureEntity
                                {
                                    RoundId = nextRound.Id,
                                    GroupId = null,
                                    HomeTeamId = groupTopTeams[1],
                                    AwayTeamId = groupTopTeams[2],
                                    Status = MatchStatus.Scheduled,
                                    LegNumber = 2
                                });

                                await _unitOfWork.SaveChangesAsync();
                                continue;
                            }
                        }
                    }
                    break;
                }

                var roundFixtures = fixtures.Where(f => f.RoundId != null).ToList();
                var activeFixtures = fixtures;
                if (roundFixtures.Count > 0)
                {
                    var minRoundNum = roundFixtures.Min(f => f.Round.RoundNumber);
                    activeFixtures = roundFixtures.Where(f => f.Round.RoundNumber == minRoundNum).ToList();
                }
                else
                {
                    activeFixtures = fixtures.Where(f => f.GroupId != null).ToList();
                }

                foreach (var fixture in activeFixtures)
                {
                    int homeScore = random.Next(0, 5);
                    int awayScore = random.Next(0, 5);

                    if (fixture.RoundId != null && homeScore == awayScore)
                    {
                        if (random.Next(0, 2) == 0) homeScore++;
                        else awayScore++;
                    }

                    int winnerTeamId = homeScore > awayScore ? fixture.HomeTeam.TeamId : (awayScore > homeScore ? fixture.AwayTeam.TeamId : 0);
                    int? winnerTournamentTeamId = homeScore > awayScore ? fixture.HomeTeamId : (awayScore > homeScore ? fixture.AwayTeamId : (int?)null);

                    var match = new MatchEntity
                    {
                        HomeTeamId = fixture.HomeTeam.TeamId,
                        AwayTeamId = fixture.AwayTeam.TeamId,
                        TournamentId = tournamentId,
                        Type = Koralytics.Domain.Enums.MatchType.Tournament,
                        Format = currentTournament.Format,
                        MatchDate = DateTime.UtcNow,
                        Location = "استاد القاهرة الدولي",
                        Status = MatchStatus.Completed,
                        HomeScore = homeScore,
                        AwayScore = awayScore,
                        WinningTeamId = winnerTeamId > 0 ? winnerTeamId : (int?)null,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        CreatedById = coachUserId
                    };
                    await _unitOfWork.Repository<MatchEntity>().AddAsync(match);
                    await _unitOfWork.SaveChangesAsync();

                    var homeSquad = await _unitOfWork.Repository<TournamentSquadEntity>()
                        .GetQueryableAsNoTracking()
                        .Where(ts => ts.TournamentId == tournamentId && ts.TeamId == fixture.HomeTeam.TeamId)
                        .Select(ts => ts.PlayerId)
                        .ToListAsync();

                    var awaySquad = await _unitOfWork.Repository<TournamentSquadEntity>()
                        .GetQueryableAsNoTracking()
                        .Where(ts => ts.TournamentId == tournamentId && ts.TeamId == fixture.AwayTeam.TeamId)
                        .Select(ts => ts.PlayerId)
                        .ToListAsync();

                    foreach (var pid in homeSquad)
                    {
                        var lineup = new MatchLineup { MatchId = match.Id, PlayerId = pid, TeamId = fixture.HomeTeam.TeamId, IsStarting = true, JerseyNumber = random.Next(1, 99) };
                        await _unitOfWork.Repository<MatchLineup>().AddAsync(lineup);
                    }
                    foreach (var pid in awaySquad)
                    {
                        var lineup = new MatchLineup { MatchId = match.Id, PlayerId = pid, TeamId = fixture.AwayTeam.TeamId, IsStarting = true, JerseyNumber = random.Next(1, 99) };
                        await _unitOfWork.Repository<MatchLineup>().AddAsync(lineup);
                    }
                    await _unitOfWork.SaveChangesAsync();

                    var homeGoalsLeft = homeScore;
                    var awayGoalsLeft = awayScore;
                    var playerGoals = new Dictionary<int, int>();
                    var playerAssists = new Dictionary<int, int>();

                    foreach (var pid in homeSquad) { playerGoals[pid] = 0; playerAssists[pid] = 0; }
                    foreach (var pid in awaySquad) { playerGoals[pid] = 0; playerAssists[pid] = 0; }

                    if (homeGoalsLeft > 0 && homeSquad.Count > 0)
                    {
                        for (int g = 0; g < homeGoalsLeft; g++)
                        {
                            var pid = homeSquad[random.Next(homeSquad.Count)];
                            playerGoals[pid]++;
                        }
                    }
                    if (awayGoalsLeft > 0 && awaySquad.Count > 0)
                    {
                        for (int g = 0; g < awayGoalsLeft; g++)
                        {
                            var pid = awaySquad[random.Next(awaySquad.Count)];
                            playerGoals[pid]++;
                        }
                    }

                    var matchRatings = new List<MatchPlayerRating>();
                    var allLineupPlayers = homeSquad.Concat(awaySquad).ToList();
                    int motmPlayerId = allLineupPlayers.Count > 0 ? allLineupPlayers[random.Next(allLineupPlayers.Count)] : 0;

                    foreach (var pid in allLineupPlayers)
                    {
                        int goals = playerGoals.GetValueOrDefault(pid, 0);
                        int assists = playerAssists.GetValueOrDefault(pid, 0);

                        var baseRating = 6.0m + goals * 1.5m + assists * 0.8m + (decimal)(random.NextDouble() * 1.5);
                        if (baseRating > 10.0m) baseRating = 10.0m;

                        var rating = new MatchPlayerRating
                        {
                            MatchId = match.Id,
                            PlayerId = pid,
                            CoachId = coachUserId,
                            Goals = goals,
                            Assists = assists,
                            MinutesPlayed = 90,
                            IsMOTM = pid == motmPlayerId,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            CreatedById = coachUserId
                        };
                        await _unitOfWork.Repository<MatchPlayerRating>().AddAsync(rating);
                        matchRatings.Add(rating);
                        await _unitOfWork.SaveChangesAsync();

                        foreach (var cat in drillCategories)
                        {
                            var cr = new MatchPlayerCategoryRating
                            {
                                MatchPlayerRatingId = rating.Id,
                                DrillCategoryId = cat.Id,
                                Rating = baseRating
                            };
                            await _unitOfWork.Repository<MatchPlayerCategoryRating>().AddAsync(cr);
                        }
                    }
                    await _unitOfWork.SaveChangesAsync();

                    fixture.MatchId = match.Id;
                    fixture.HomeScore = homeScore;
                    fixture.AwayScore = awayScore;
                    fixture.WinnerTeamId = winnerTournamentTeamId;
                    fixture.Status = MatchStatus.Completed;
                    await _unitOfWork.SaveChangesAsync();

                    if (fixture.GroupId.HasValue)
                    {
                        var homeStanding = await _unitOfWork.Repository<TournamentStandingEntity>()
                            .FindAsync(s => s.GroupId == fixture.GroupId && s.TournamentTeamId == fixture.HomeTeamId);
                        var awayStanding = await _unitOfWork.Repository<TournamentStandingEntity>()
                            .FindAsync(s => s.GroupId == fixture.GroupId && s.TournamentTeamId == fixture.AwayTeamId);

                        if (homeStanding != null && awayStanding != null)
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
                                homeStanding.Points++;
                                awayStanding.Drawn++;
                                awayStanding.Points++;
                            }
                            await _unitOfWork.SaveChangesAsync();
                        }
                    }
                }

                if (roundFixtures.Count > 0)
                {
                    var activeRoundId = activeFixtures.First().RoundId!.Value;
                    var activeRoundFixtures = await _unitOfWork.Repository<TournamentFixtureEntity>()
                        .GetQueryableAsNoTracking()
                        .Where(f => f.RoundId == activeRoundId)
                        .ToListAsync();

                    if (activeRoundFixtures.All(f => f.Status == MatchStatus.Completed))
                    {
                        var roundWinners = activeRoundFixtures.Select(f => f.WinnerTeamId!.Value).Distinct().ToList();
                        if (roundWinners.Count > 1)
                        {
                            await _tournamentFixtureService.AdvanceKnockoutAsync(tournamentId, activeRoundId);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            await _tournamentReportService.CompleteTournamentAsync(tournamentId);
        }

        public async Task SimulateThreeAcademiesTournamentAsync(int tournamentId)
        {
            _logger.LogInformation("Simulating tournament {TournamentId} with 3 academies", tournamentId);

            var tournament = await _unitOfWork.Repository<TournamentEntity>()
                .FindAsync(t => t.Id == tournamentId);

            if (tournament is null)
                throw new NotFoundException($"Tournament with Id {tournamentId} not found");

            var systemAdmin = await _unitOfWork.Repository<User>()
                .GetQueryableAsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == "superadmin@koralytics.com" || u.Email == "admin@koralytics.com");
            int adminUserId = systemAdmin?.Id ?? 1;

            if (tournament.Status == TournamentStatus.Draft || tournament.Status == TournamentStatus.Registration)
            {
                // Create 3 academies
                var academyNames = new string[] { "أكاديمية ألفا للناشئين", "أكاديمية بيتا للناشئين", "أكاديمية غاما للناشئين" };
                var academies = new List<AcademyEntity>();

                foreach (var name in academyNames)
                {
                    var ac = await _unitOfWork.Repository<AcademyEntity>()
                        .GetQueryable()
                        .FirstOrDefaultAsync(a => a.Name == name);
                    if (ac == null)
                    {
                        ac = new AcademyEntity 
                        { 
                            Name = name, 
                            Status = AcademyStatus.Active, 
                            AdminUserId = adminUserId,
                            CreatedAt = DateTime.UtcNow, 
                            UpdatedAt = DateTime.UtcNow 
                        };
                        await _unitOfWork.Repository<AcademyEntity>().AddAsync(ac);
                        await _unitOfWork.SaveChangesAsync();
                    }
                    academies.Add(ac);
                }

                // Create AgeGroup, Locations, Teams, and Players for each academy
                var teamList = new List<Team>();
                var tournamentAgeGroup = await _unitOfWork.Repository<AgeGroup>()
                    .FindAsync(a => a.Id == tournament.AgeGroupId);

                if (tournamentAgeGroup == null)
                    throw new NotFoundException("Tournament age group not found");

                var playerNamesTemplate = new string[] {
                    "أحمد", "محمد", "يوسف", "عمر", "كريم", "علي", "مصطفى", "خالد", "حسن", "حسين", "زياد", "مروان", "إبراهيم", "طارق", "سيف"
                };
                var familyNamesTemplate = new string[] {
                    "سليم", "الشناوي", "عبد الله", "راضي", "إمام", "فتوح", "ماهر", "شريف", "كامل", "نبيل", "جاد", "بسام", "شكري", "عادل", "عمار"
                };

                var random = new Random();

                for (int i = 0; i < academies.Count; i++)
                {
                    var academy = academies[i];

                    // Location
                    var location = await _unitOfWork.Repository<AcademyLocation>()
                        .GetQueryable()
                        .FirstOrDefaultAsync(al => al.AcademyId == academy.Id);
                    if (location == null)
                    {
                        location = new AcademyLocation 
                        { 
                            AcademyId = academy.Id, 
                            Name = $"فرع {academy.Name}", 
                            Address = "شارع الرياضة", 
                            City = "القاهرة", 
                            IsMain = true 
                        };
                        await _unitOfWork.Repository<AcademyLocation>().AddAsync(location);
                        await _unitOfWork.SaveChangesAsync();
                    }

                    // AgeGroup
                    var ageGroup = await _unitOfWork.Repository<AgeGroup>()
                        .GetQueryable()
                        .FirstOrDefaultAsync(ag => ag.AcademyId == academy.Id && ag.MinAge == tournamentAgeGroup.MinAge && ag.MaxAge == tournamentAgeGroup.MaxAge);
                    if (ageGroup == null)
                    {
                        ageGroup = new AgeGroup 
                        { 
                            AcademyId = academy.Id, 
                            Name = tournamentAgeGroup.Name, 
                            MinAge = tournamentAgeGroup.MinAge, 
                            MaxAge = tournamentAgeGroup.MaxAge 
                        };
                        await _unitOfWork.Repository<AgeGroup>().AddAsync(ageGroup);
                        await _unitOfWork.SaveChangesAsync();
                    }

                    // Team
                    var teamName = $"{academy.Name.Replace("أكاديمية ", "").Replace(" للناشئين", "")} U17";
                    var team = await _unitOfWork.Repository<Team>()
                        .GetQueryable()
                        .FirstOrDefaultAsync(t => t.AcademyId == academy.Id && t.Name == teamName);
                    if (team == null)
                    {
                        team = new Team 
                        { 
                            AcademyId = academy.Id, 
                            AgeGroupId = ageGroup.Id, 
                            LocationId = location.Id, 
                            Name = teamName 
                        };
                        await _unitOfWork.Repository<Team>().AddAsync(team);
                        await _unitOfWork.SaveChangesAsync();
                    }
                    teamList.Add(team);

                    // Players
                    var ptCount = await _unitOfWork.Repository<PlayerTeamEntity>().CountAsync(pt => pt.TeamId == team.Id && pt.LeftAt == null);
                    if (ptCount < 11)
                    {
                        for (int j = 0; j < 11; j++)
                        {
                            var firstN = playerNamesTemplate[random.Next(playerNamesTemplate.Length)];
                            var lastN = familyNamesTemplate[random.Next(familyNamesTemplate.Length)];
                            var email = $"mock_test_{i}_{j}_{Guid.NewGuid().ToString().Substring(0, 5)}@koralytics.com";

                            var player = new PlayerEntity
                            {
                                UserName = email,
                                Email = email,
                                EmailConfirmed = true,
                                FirstName = firstN,
                                LastName = lastN,
                                DateOfBirth = DateTime.UtcNow.AddYears(-16),
                                PreferredFoot = j % 3 == 0 ? PreferredFoot.Left : PreferredFoot.Right,
                                WeakFootRating = 3,
                                AvailabilityStatus = AvailabilityStatus.Available,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            await _unitOfWork.Repository<PlayerEntity>().AddAsync(player);
                            await _unitOfWork.SaveChangesAsync();

                            var pos = j == 0 ? "GK" : (j < 5 ? "CB" : (j < 8 ? "CM" : "ST"));
                            var playerPos = new PlayerPosition { PlayerId = player.Id, Position = pos, IsPrimary = true };
                            await _unitOfWork.Repository<PlayerPosition>().AddAsync(playerPos);

                            var playerAc = new PlayerAcademy { PlayerId = player.Id, AcademyId = academy.Id, JoinedAt = DateTime.UtcNow, Status = PlayerAcademyStatus.Active };
                            await _unitOfWork.Repository<PlayerAcademy>().AddAsync(playerAc);

                            var playerTeam = new PlayerTeamEntity { PlayerId = player.Id, TeamId = team.Id, JoinedAt = DateTime.UtcNow };
                            await _unitOfWork.Repository<PlayerTeamEntity>().AddAsync(playerTeam);
                            await _unitOfWork.SaveChangesAsync();
                        }
                    }

                    // Register to Tournament
                    var isRegistered = await _unitOfWork.Repository<TournamentTeamEntity>()
                        .ExistsAsync(tt => tt.TournamentId == tournamentId && tt.TeamId == team.Id);
                    if (!isRegistered)
                    {
                        var tt = new TournamentTeamEntity 
                        { 
                            TournamentId = tournamentId, 
                            TeamId = team.Id, 
                            Status = TournamentTeamStatus.Accepted, 
                            RegisteredAt = DateTime.UtcNow 
                        };
                        await _unitOfWork.Repository<TournamentTeamEntity>().AddAsync(tt);
                        await _unitOfWork.SaveChangesAsync();
                    }

                    // Register Squad
                    var activePlayers = await _unitOfWork.Repository<PlayerTeamEntity>()
                        .GetQueryable()
                        .Where(pt => pt.TeamId == team.Id && pt.LeftAt == null)
                        .Select(pt => pt.PlayerId)
                        .ToListAsync();
                    foreach (var pid in activePlayers)
                    {
                        var inSquad = await _unitOfWork.Repository<TournamentSquadEntity>()
                            .ExistsAsync(ts => ts.TournamentId == tournamentId && ts.PlayerId == pid);
                        if (!inSquad)
                        {
                            var squad = new TournamentSquadEntity 
                            { 
                                TournamentId = tournamentId, 
                                TeamId = team.Id, 
                                PlayerId = pid, 
                                RegisteredAt = DateTime.UtcNow 
                            };
                            await _unitOfWork.Repository<TournamentSquadEntity>().AddAsync(squad);
                        }
                    }
                    await _unitOfWork.SaveChangesAsync();
                }

                tournament.Status = TournamentStatus.Registration;
                await _unitOfWork.SaveChangesAsync();

                await _tournamentDrawService.GenerateSeedingAsync(tournamentId);
                await _tournamentDrawService.GenerateDrawAsync(tournamentId);
            }

            var drillCategories = await _unitOfWork.Repository<DrillCategory>()
                .GetQueryableAsNoTracking()
                .ToListAsync();

            var randomEngine = new Random();

            while (true)
            {
                var currentTournament = await _unitOfWork.Repository<TournamentEntity>()
                    .FindAsync(t => t.Id == tournamentId);

                if (currentTournament == null || currentTournament.Status == TournamentStatus.Completed)
                    break;

                var fixtures = await _unitOfWork.Repository<TournamentFixtureEntity>()
                    .GetQueryable()
                    .Include(f => f.HomeTeam).ThenInclude(tt => tt.Team)
                    .Include(f => f.AwayTeam).ThenInclude(tt => tt.Team)
                    .Include(f => f.Group)
                    .Include(f => f.Round)
                    .Where(f => (f.GroupId != null ? f.Group.TournamentId == tournamentId : f.Round.TournamentId == tournamentId) && f.Status != MatchStatus.Completed)
                    .ToListAsync();

                if (fixtures.Count == 0)
                    break;

                foreach (var fixture in fixtures)
                {
                    int homeScore = randomEngine.Next(0, 5);
                    int awayScore = randomEngine.Next(0, 5);

                    if (fixture.RoundId != null && homeScore == awayScore)
                    {
                        if (randomEngine.Next(0, 2) == 0) homeScore++;
                        else awayScore++;
                    }

                    int winnerTeamId = homeScore > awayScore ? fixture.HomeTeam.TeamId : (awayScore > homeScore ? fixture.AwayTeam.TeamId : 0);
                    int? winnerTournamentTeamId = homeScore > awayScore ? fixture.HomeTeamId : (awayScore > homeScore ? fixture.AwayTeamId : (int?)null);

                    var match = new MatchEntity
                    {
                        HomeTeamId = fixture.HomeTeam.TeamId,
                        AwayTeamId = fixture.AwayTeam.TeamId,
                        TournamentId = tournamentId,
                        Type = Koralytics.Domain.Enums.MatchType.Tournament,
                        Format = currentTournament.Format,
                        MatchDate = DateTime.UtcNow,
                        Location = "استاد القاهرة الدولي",
                        Status = MatchStatus.Completed,
                        HomeScore = homeScore,
                        AwayScore = awayScore,
                        WinningTeamId = winnerTeamId > 0 ? winnerTeamId : (int?)null,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        CreatedById = adminUserId
                    };
                    await _unitOfWork.Repository<MatchEntity>().AddAsync(match);
                    await _unitOfWork.SaveChangesAsync();

                    var homeSquad = await _unitOfWork.Repository<TournamentSquadEntity>()
                        .GetQueryableAsNoTracking()
                        .Where(ts => ts.TournamentId == tournamentId && ts.TeamId == fixture.HomeTeam.TeamId)
                        .Select(ts => ts.PlayerId)
                        .ToListAsync();

                    var awaySquad = await _unitOfWork.Repository<TournamentSquadEntity>()
                        .GetQueryableAsNoTracking()
                        .Where(ts => ts.TournamentId == tournamentId && ts.TeamId == fixture.AwayTeam.TeamId)
                        .Select(ts => ts.PlayerId)
                        .ToListAsync();

                    foreach (var pid in homeSquad)
                    {
                        var lineup = new MatchLineup { MatchId = match.Id, PlayerId = pid, TeamId = fixture.HomeTeam.TeamId, IsStarting = true, JerseyNumber = randomEngine.Next(1, 99) };
                        await _unitOfWork.Repository<MatchLineup>().AddAsync(lineup);
                    }
                    foreach (var pid in awaySquad)
                    {
                        var lineup = new MatchLineup { MatchId = match.Id, PlayerId = pid, TeamId = fixture.AwayTeam.TeamId, IsStarting = true, JerseyNumber = randomEngine.Next(1, 99) };
                        await _unitOfWork.Repository<MatchLineup>().AddAsync(lineup);
                    }
                    await _unitOfWork.SaveChangesAsync();

                    var homeGoalsLeft = homeScore;
                    var awayGoalsLeft = awayScore;
                    var playerGoals = new Dictionary<int, int>();
                    var playerAssists = new Dictionary<int, int>();

                    foreach (var pid in homeSquad) { playerGoals[pid] = 0; playerAssists[pid] = 0; }
                    foreach (var pid in awaySquad) { playerGoals[pid] = 0; playerAssists[pid] = 0; }

                    if (homeGoalsLeft > 0 && homeSquad.Count > 0)
                    {
                        for (int g = 0; g < homeGoalsLeft; g++)
                        {
                            var pid = homeSquad[randomEngine.Next(homeSquad.Count)];
                            playerGoals[pid]++;
                        }
                    }
                    if (awayGoalsLeft > 0 && awaySquad.Count > 0)
                    {
                        for (int g = 0; g < awayGoalsLeft; g++)
                        {
                            var pid = awaySquad[randomEngine.Next(awaySquad.Count)];
                            playerGoals[pid]++;
                        }
                    }

                    var matchRatings = new List<MatchPlayerRating>();
                    var allLineupPlayers = homeSquad.Concat(awaySquad).ToList();
                    int motmPlayerId = allLineupPlayers.Count > 0 ? allLineupPlayers[randomEngine.Next(allLineupPlayers.Count)] : 0;

                    foreach (var pid in allLineupPlayers)
                    {
                        int goals = playerGoals.GetValueOrDefault(pid, 0);
                        int assists = playerAssists.GetValueOrDefault(pid, 0);

                        var baseRating = 6.0m + goals * 1.5m + assists * 0.8m + (decimal)(randomEngine.NextDouble() * 1.5);
                        if (baseRating > 10.0m) baseRating = 10.0m;

                        var rating = new MatchPlayerRating
                        {
                            MatchId = match.Id,
                            PlayerId = pid,
                            CoachId = adminUserId,
                            Goals = goals,
                            Assists = assists,
                            MinutesPlayed = 90,
                            IsMOTM = pid == motmPlayerId,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            CreatedById = adminUserId
                        };
                        await _unitOfWork.Repository<MatchPlayerRating>().AddAsync(rating);
                        matchRatings.Add(rating);
                        await _unitOfWork.SaveChangesAsync();

                        foreach (var cat in drillCategories)
                        {
                            var cr = new MatchPlayerCategoryRating
                            {
                                MatchPlayerRatingId = rating.Id,
                                DrillCategoryId = cat.Id,
                                Rating = baseRating
                            };
                            await _unitOfWork.Repository<MatchPlayerCategoryRating>().AddAsync(cr);
                        }
                    }
                    await _unitOfWork.SaveChangesAsync();

                    fixture.MatchId = match.Id;
                    fixture.HomeScore = homeScore;
                    fixture.AwayScore = awayScore;
                    fixture.WinnerTeamId = winnerTournamentTeamId;
                    fixture.Status = MatchStatus.Completed;
                    await _unitOfWork.SaveChangesAsync();

                    if (fixture.GroupId.HasValue)
                    {
                        var homeStanding = await _unitOfWork.Repository<TournamentStandingEntity>()
                            .FindAsync(s => s.GroupId == fixture.GroupId && s.TournamentTeamId == fixture.HomeTeamId);
                        var awayStanding = await _unitOfWork.Repository<TournamentStandingEntity>()
                            .FindAsync(s => s.GroupId == fixture.GroupId && s.TournamentTeamId == fixture.AwayTeamId);

                        if (homeStanding != null && awayStanding != null)
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
                                homeStanding.Points++;
                                awayStanding.Drawn++;
                                awayStanding.Points++;
                            }
                            await _unitOfWork.SaveChangesAsync();
                        }
                    }
                }
            }

            await _tournamentReportService.CompleteTournamentAsync(tournamentId);
        }
    }
}