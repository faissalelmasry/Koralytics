using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Coach;
using Koralytics.Domain.Entities.Drill;
using Koralytics.Domain.Entities.Identity;
using Koralytics.Domain.Entities.Match;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Entities.SystemAdmin;
using Koralytics.Domain.Enums;
using Koralytics.Infrastructure.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Koralytics.Infrastructure.Seeding
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(ApplicationDbContext context, UserManager<User> userManager, RoleManager<Role> roleManager)
        {
            // Seed Roles
            var roles = new[] { "Scouter", "SystemAdmin", "AcademyAdmin", "Player", "Parent", "Coach", "PendingProfile" };
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new Role { Name = roleName });
                }
            }

            // Seed DrillCategories
            var categories = new[] { "Passing", "Shooting", "Dribbling", "Defending", "GoalKeeping", "Speed", "Physical" };
            foreach (var categoryName in categories)
            {
                if (!await context.DrillCategories.AnyAsync(c => c.Name == categoryName))
                {
                    context.DrillCategories.Add(new DrillCategory { Name = categoryName });
                }
            }
            await context.SaveChangesAsync();

            // Seed System Admin account
            const string adminEmail = "admin@koralytics.com";
            const string adminUserName = "SystemAdmin";
            const string adminPassword = "Admin@123456";

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new SystemAdminUser
                {
                    UserName = adminUserName,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FirstName = "System",
                    LastName = "Admin",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedById = 1
                };

                await context.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE [AspNetUsers] NOCHECK CONSTRAINT [FK_AspNetUsers_AspNetUsers_CreatedById]");

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    adminUser.CreatedById = adminUser.Id;
                    await context.SaveChangesAsync();
                    await userManager.AddToRoleAsync(adminUser, "SystemAdmin");
                }

                await context.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE [AspNetUsers] CHECK CONSTRAINT [FK_AspNetUsers_AspNetUsers_CreatedById]");
            }
            // ---- SEED TEST DATA (Development only) ----

            // Second Academy (needed for match AwayTeam)
            if (!await context.Academies.AnyAsync(a => a.Name == "Zamalek Academy"))
            {
                var academy2 = new Academy
                {
                    Name = "Zamalek Academy",
                    Status = AcademyStatus.Active,
                    AdminUserId = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                context.Academies.Add(academy2);
                await context.SaveChangesAsync();
            }

            // Main Academy
            if (!await context.Academies.AnyAsync(a => a.Name == "Al Ahly Academy"))
            {
                // --- Academy Admin User ---
                var adminUser = new User
                {
                    UserName = "academyadmin@test.com",
                    Email = "academyadmin@test.com",
                    EmailConfirmed = true,
                    FirstName = "Academy",
                    LastName = "Admin",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedById = (await userManager.FindByEmailAsync("admin@koralytics.com"))!.Id
                };
                await userManager.CreateAsync(adminUser, "Admin@123456");
                await context.SaveChangesAsync();
                await userManager.AddToRoleAsync(adminUser, "SystemAdmin");
                await context.SaveChangesAsync();

                var academyAdminUser = new User
                {
                    UserName = "academyadmin@test.com",
                    Email = "academyadmin@test.com",
                    EmailConfirmed = true,
                    FirstName = "Academy",
                    LastName = "Admin",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedById = adminUser!.Id
                };
                await userManager.CreateAsync(academyAdminUser, "Admin@123456");
                await context.SaveChangesAsync();
                context.ChangeTracker.Clear();

                var freshAdminUser = await userManager.FindByEmailAsync("academyadmin@test.com");
                await userManager.AddToRoleAsync(freshAdminUser!, "AcademyAdmin");
                await context.SaveChangesAsync();
                context.ChangeTracker.Clear();

                var academy = new Academy
                {
                    Name = "Al Ahly Academy",
                    Status = AcademyStatus.Active,
                    AdminUserId = freshAdminUser!.Id,  // ? use freshAdminUser not academyAdminUser
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                context.Academies.Add(academy);
                await context.SaveChangesAsync();
                context.ChangeTracker.Clear();

                await context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO AcademyAdmins (Id, AcademyId) VALUES ({0}, {1})",
                    freshAdminUser.Id,  // ? use freshAdminUser
                    academy.Id
                );
                // --- Academy Location ---
                var location = new AcademyLocation
                {
                    AcademyId = academy.Id,
                    Name = "Main Branch",
                    Address = "Nasr City",
                    City = "Cairo",
                    IsMain = true
                };
                context.AcademyLocations.Add(location);

                // --- Age Group ---
                var ageGroup = new AgeGroup
                {
                    AcademyId = academy.Id,
                    Name = "U17",
                    MinAge = 15,
                    MaxAge = 17
                };
                context.AgeGroups.Add(ageGroup);
                await context.SaveChangesAsync();

                // --- Team ---
                var team = new Team
                {
                    AcademyId = academy.Id,
                    AgeGroupId = ageGroup.Id,
                    LocationId = location.Id,
                    Name = "U17 Team A"
                };
                context.Teams.Add(team);

                // --- Away Team (Zamalek) ---
                var zamalekAcademy = await context.Academies.FirstAsync(a => a.Name == "Zamalek Academy");
                var zamalekAgeGroup = new AgeGroup
                {
                    AcademyId = zamalekAcademy.Id,
                    Name = "U17",
                    MinAge = 15,
                    MaxAge = 17
                };
                context.AgeGroups.Add(zamalekAgeGroup);
                await context.SaveChangesAsync();

                var zamalekLocation = new AcademyLocation
                {
                    AcademyId = zamalekAcademy.Id,
                    Name = "Main Branch",
                    Address = "Dokki",
                    City = "Giza",
                    IsMain = true
                };
                context.AcademyLocations.Add(zamalekLocation);
                await context.SaveChangesAsync();

                var awayTeam = new Team
                {
                    AcademyId = zamalekAcademy.Id,
                    AgeGroupId = zamalekAgeGroup.Id,
                    LocationId = zamalekLocation.Id,
                    Name = "U17 Team A"
                };
                context.Teams.Add(awayTeam);
                await context.SaveChangesAsync();

                // --- Coach ---
                var coachUser = new User
                {
                    UserName = "coach@test.com",
                    Email = "coach@test.com",
                    EmailConfirmed = true,
                    FirstName = "Ahmed",
                    LastName = "Hassan",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedById = adminUser.Id
                };
                await userManager.CreateAsync(coachUser, "Coach@123456");
                await userManager.AddToRoleAsync(coachUser, "Coach");

                await context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO Coaches (Id) VALUES ({0})",
                    coachUser.Id
                );

                var coachAcademy = new CoachAcademy
                {
                    CoachUserId = coachUser.Id,
                    AcademyId = academy.Id,
                    JoinedAt = DateTime.UtcNow
                };
                context.CoachAcademies.Add(coachAcademy);

                var coachTeam = new CoachTeam
                {
                    CoachUserId = coachUser.Id,
                    TeamId = team.Id,
                    AssignedAt = DateTime.UtcNow
                };
                context.CoachTeams.Add(coachTeam);
                await context.SaveChangesAsync();

                // --- Player ---
                var playerUser = new User
                {
                    UserName = "player@test.com",
                    Email = "player@test.com",
                    EmailConfirmed = true,
                    FirstName = "Omar",
                    LastName = "Khaled",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedById = adminUser.Id
                };
                await userManager.CreateAsync(playerUser, "Player@123456");
                await userManager.AddToRoleAsync(playerUser, "Player");

                await context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO Players (Id, DateOfBirth, PreferredFoot, WeakFootRating, AvailabilityStatus) VALUES ({0}, {1}, {2}, {3}, {4})",
                    playerUser.Id,
                    new DateTime(2008, 1, 1),
                    (int)PreferredFoot.Right,      // cast to int
                    2,
                    (int)AvailabilityStatus.Available  // cast to int
                );

                var playerAcademy = new PlayerAcademy
                {
                    PlayerId = playerUser.Id,
                    AcademyId = academy.Id,
                    JoinedAt = DateTime.UtcNow,
                    Status = PlayerAcademyStatus.Active
                };
                context.PlayerAcademies.Add(playerAcademy);

                var playerTeam = new PlayerTeam
                {
                    PlayerId = playerUser.Id,
                    TeamId = team.Id,
                    JoinedAt = DateTime.UtcNow
                };
                context.PlayerTeams.Add(playerTeam);

                var playerPosition = new PlayerPosition
                {
                    PlayerId = playerUser.Id,
                    Position = "ST",
                    IsPrimary = true
                };
                context.PlayerPositions.Add(playerPosition);

                var playerSubscription = new PlayerSubscription
                {
                    PlayerId = playerUser.Id,
                    AcademyId = academy.Id,
                    Status = SubscriptionStatus.Paid,
                    PaidAt = DateTime.UtcNow,
                    PaidByUserId = playerUser.Id
                };
                context.PlayerSubscriptions.Add(playerSubscription);
                await context.SaveChangesAsync();

                // --- Player 2 ---
                var player2User = new User
                {
                    UserName = "player2@test.com",
                    Email = "player2@test.com",
                    EmailConfirmed = true,
                    FirstName = "Ahmed",
                    LastName = "Saeed",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedById = adminUser.Id
                };
                await userManager.CreateAsync(player2User, "Player@123456");
                await userManager.AddToRoleAsync(player2User, "Player");

                await context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO Players (Id, DateOfBirth, PreferredFoot, WeakFootRating, AvailabilityStatus) VALUES ({0}, {1}, {2}, {3}, {4})",
                    player2User.Id, new DateTime(2009, 3, 10), (int)PreferredFoot.Left, 3, (int)AvailabilityStatus.Available);
                context.PlayerTeams.Add(new PlayerTeam { PlayerId = player2User.Id, TeamId = team.Id, JoinedAt = DateTime.UtcNow });
                context.PlayerAcademies.Add(new PlayerAcademy { PlayerId = player2User.Id, AcademyId = academy.Id, JoinedAt = DateTime.UtcNow, Status = PlayerAcademyStatus.Active });
                context.PlayerPositions.Add(new PlayerPosition { PlayerId = player2User.Id, Position = "LW", IsPrimary = true });
                context.PlayerSubscriptions.Add(new PlayerSubscription { PlayerId = player2User.Id, AcademyId = academy.Id, Status = SubscriptionStatus.Paid, PaidAt = DateTime.UtcNow, PaidByUserId = player2User.Id });
                await context.SaveChangesAsync();

                // --- Player 3 ---
                var player3User = new User
                {
                    UserName = "player3@test.com",
                    Email = "player3@test.com",
                    EmailConfirmed = true,
                    FirstName = "Mohamed",
                    LastName = "Ali",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedById = adminUser.Id
                };
                await userManager.CreateAsync(player3User, "Player@123456");
                await userManager.AddToRoleAsync(player3User, "Player");

                await context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO Players (Id, DateOfBirth, PreferredFoot, WeakFootRating, AvailabilityStatus) VALUES ({0}, {1}, {2}, {3}, {4})",
                    player3User.Id, new DateTime(2008, 8, 22), (int)PreferredFoot.Right, 2, (int)AvailabilityStatus.Available);
                context.PlayerTeams.Add(new PlayerTeam { PlayerId = player3User.Id, TeamId = team.Id, JoinedAt = DateTime.UtcNow });
                context.PlayerAcademies.Add(new PlayerAcademy { PlayerId = player3User.Id, AcademyId = academy.Id, JoinedAt = DateTime.UtcNow, Status = PlayerAcademyStatus.Active });
                context.PlayerPositions.Add(new PlayerPosition { PlayerId = player3User.Id, Position = "CB", IsPrimary = true });
                context.PlayerSubscriptions.Add(new PlayerSubscription { PlayerId = player3User.Id, AcademyId = academy.Id, Status = SubscriptionStatus.Paid, PaidAt = DateTime.UtcNow, PaidByUserId = player3User.Id });
                await context.SaveChangesAsync();

                // --- Drill Session ---
                var shootingCategory = await context.DrillCategories.FirstAsync(c => c.Name == "Shooting");
                var passingCategory = await context.DrillCategories.FirstAsync(c => c.Name == "Passing");

                var drillTemplate = new DrillTemplate
                {
                    CategoryId = shootingCategory.Id,
                    AcademyId = null,
                    Name = "Long Shot Drill",
                    DifficultyLevel = DifficultyLevel.Intermediate,
                    DrillMode = DrillMode.SuccessOrMissed,
                    IsShared = true,
                    CreatedById = adminUser.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                context.DrillTemplates.Add(drillTemplate);

                var passTemplate = new DrillTemplate
                {
                    CategoryId = passingCategory.Id,
                    AcademyId = null,
                    Name = "Short Pass Drill",
                    DifficultyLevel = DifficultyLevel.Beginner,
                    DrillMode = DrillMode.Manual,
                    IsShared = true,
                    CreatedById = adminUser.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                context.DrillTemplates.Add(passTemplate);
                await context.SaveChangesAsync();

                var session = new DrillSession
                {
                    AcademyId = academy.Id,
                    TeamId = team.Id,
                    CoachId = coachUser.Id,
                    SessionDate = DateTime.UtcNow.AddDays(-7),
                  //  Type = SessionType.Regular,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                context.DrillSessions.Add(session);
                await context.SaveChangesAsync();

                var sessionAttendance = new SessionAttendance
                {
                    SessionId = session.Id,
                    playerId = playerUser.Id,
                    IsPresent = true
                };
                context.SessionAttendances.Add(sessionAttendance);

                var sessionDrill = new Drill
                {
                    SessionId = session.Id,
                    DrillTemplateId = drillTemplate.Id,
                    Mode = DrillMode.SuccessOrMissed
                };
                context.Drills.Add(sessionDrill);

                var sessionDrill2 = new Drill
                {
                    SessionId = session.Id,
                    DrillTemplateId = passTemplate.Id,
                    Mode = DrillMode.Manual
                };
                context.Drills.Add(sessionDrill2);
                await context.SaveChangesAsync();

                var drillResult = new DrillResult
                {
                    DrillId = sessionDrill.Id,
                    PlayerId = playerUser.Id,
                    DoneCount = 7,
                    MissedCount = 3,
                    FinalScore = 7.0m,
                    CreatedAt = DateTime.UtcNow
                };
                context.DrillResults.Add(drillResult);

                var drillResult2 = new DrillResult
                {
                    DrillId = sessionDrill2.Id,
                    PlayerId = playerUser.Id,
                    ManualScore = 8.0m,
                    FinalScore = 8.0m,
                    CreatedAt = DateTime.UtcNow
                };
                context.DrillResults.Add(drillResult2);
                await context.SaveChangesAsync();

                // --- Match ---
                var match = new Match
                {
                    HomeTeamId = team.Id,
                    AwayTeamId = awayTeam.Id,
                    Type = Domain.Enums.MatchType.Friendly,
                    Format = MatchFormat.ElevenSide,
                    Status = MatchStatus.Completed,
                    MatchDate = DateTime.UtcNow.AddDays(-3),
                    HomeScore = 2,
                    AwayScore = 1,
                    Location="Ittihad Club",
                    WinningTeamId = team.Id,
                    CreatedById = coachUser.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                context.Matches.Add(match);
                await context.SaveChangesAsync();

                var matchLineup = new MatchLineup
                {
                    MatchId = match.Id,
                    PlayerId = playerUser.Id,
                    TeamId = team.Id,
                    IsStarting = true,
                    JerseyNumber = 9
                };
                context.MatchLineups.Add(matchLineup);

                var matchEvent = new MatchEvent
                {
                    MatchId = match.Id,
                    PlayerId = playerUser.Id,
                    TeamId = team.Id,
                    EventType = MatchEventType.Goal,
                    Minute = 23,
                    CreatedById = coachUser.Id
                };
                context.MatchEvents.Add(matchEvent);

                var matchEvent2 = new MatchEvent
                {
                    MatchId = match.Id,
                    PlayerId = playerUser.Id,
                    TeamId = team.Id,
                    EventType = MatchEventType.Goal,
                    Minute = 67,
                    CreatedById = coachUser.Id
                };
                context.MatchEvents.Add(matchEvent2);

                var matchRating = new MatchPlayerRating
                {
                    MatchId = match.Id,
                    PlayerId = playerUser.Id,
                    CoachId = coachUser.Id,
                    Goals = 2,
                    Assists = 0,
                    MinutesPlayed = 90,
                    IsMOTM = true
                };
                context.MatchPlayerRatings.Add(matchRating);
                await context.SaveChangesAsync();

                // Friendly match per-category expert ratings
                context.MatchPlayerCategoryRatings.AddRange(
                    new MatchPlayerCategoryRating { MatchPlayerRatingId = matchRating.Id, DrillCategoryId = shootingCategory.Id, Rating = 8.0m },
                    new MatchPlayerCategoryRating { MatchPlayerRatingId = matchRating.Id, DrillCategoryId = passingCategory.Id,  Rating = 9.0m }
                );
                await context.SaveChangesAsync();

                // Friendly match with draw + penalty shootout (for penalty logic testing)
                var penaltyMatch = new Match
                {
                    HomeTeamId = team.Id, AwayTeamId = awayTeam.Id,
                    Type = Domain.Enums.MatchType.Friendly, Format = MatchFormat.ElevenSide,
                    Status = MatchStatus.Completed, MatchDate = DateTime.UtcNow.AddDays(-10),
                    HomeScore = 2, AwayScore = 2, Location = "Neutral Ground",
                    HomePenaltyScore = 5, AwayPenaltyScore = 4,
                    WinningTeamId = team.Id, CreatedById = coachUser.Id,
                    CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                };
                context.Matches.Add(penaltyMatch);
                await context.SaveChangesAsync();

                context.MatchLineups.Add(
                    new MatchLineup { MatchId = penaltyMatch.Id, PlayerId = playerUser.Id, TeamId = team.Id, IsStarting = true, JerseyNumber = 9 }
                );

                var penaltyMatchRating = new MatchPlayerRating
                {
                    MatchId = penaltyMatch.Id, PlayerId = playerUser.Id, CoachId = coachUser.Id,
                    Goals = 1, Assists = 0, MinutesPlayed = 90, IsMOTM = true
                };
                context.MatchPlayerRatings.Add(penaltyMatchRating);
                await context.SaveChangesAsync();

                context.MatchPlayerCategoryRatings.AddRange(
                    new MatchPlayerCategoryRating { MatchPlayerRatingId = penaltyMatchRating.Id, DrillCategoryId = shootingCategory.Id, Rating = 8.5m },
                    new MatchPlayerCategoryRating { MatchPlayerRatingId = penaltyMatchRating.Id, DrillCategoryId = passingCategory.Id, Rating = 7.0m }
                );
                await context.SaveChangesAsync();

                // ===== PLAYER CARD TEST DATA: DRILLS =====
                var allCategories = await context.DrillCategories.ToListAsync();
                var dribblingCat = allCategories.First(c => c.Name == "Dribbling");
                var defendingCat = allCategories.First(c => c.Name == "Defending");
                var physicalCat = allCategories.First(c => c.Name == "Physical");
                var speedCat = allCategories.First(c => c.Name == "Speed");

                // Drill Templates for remaining categories
                var dribbleTemplate = new DrillTemplate
                {
                    CategoryId = dribblingCat.Id, AcademyId = null,
                    Name = "Cone Dribbling Drill", DifficultyLevel = DifficultyLevel.Advanced,
                    DrillMode = DrillMode.SuccessOrMissed, IsShared = true,
                    CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                };
                context.DrillTemplates.Add(dribbleTemplate);

                var defendTemplate = new DrillTemplate
                {
                    CategoryId = defendingCat.Id, AcademyId = null,
                    Name = "1v1 Defending Drill", DifficultyLevel = DifficultyLevel.Intermediate,
                    DrillMode = DrillMode.Manual, IsShared = true,
                    CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                };
                context.DrillTemplates.Add(defendTemplate);

                var physicalTemplate = new DrillTemplate
                {
                    CategoryId = physicalCat.Id, AcademyId = null,
                    Name = "Agility Ladder Drill", DifficultyLevel = DifficultyLevel.Intermediate,
                    DrillMode = DrillMode.SuccessOrMissed, IsShared = true,
                    CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                };
                context.DrillTemplates.Add(physicalTemplate);

                var speedTemplate = new DrillTemplate
                {
                    CategoryId = speedCat.Id, AcademyId = null,
                    Name = "Sprint Test Drill", DifficultyLevel = DifficultyLevel.Beginner,
                    DrillMode = DrillMode.SuccessOrMissed, IsShared = true,
                    CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                };
                context.DrillTemplates.Add(speedTemplate);
                await context.SaveChangesAsync();

                // Session 2 with all new drills
                var session2 = new DrillSession
                {
                    AcademyId = academy.Id, TeamId = team.Id, CoachId = coachUser.Id,
                    SessionDate = DateTime.UtcNow.AddDays(-4), Type = SessionType.Regular,
                    CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                };
                context.DrillSessions.Add(session2);
                await context.SaveChangesAsync();

                var drillDribble = new Drill { SessionId = session2.Id, DrillTemplateId = dribbleTemplate.Id, Mode = DrillMode.SuccessOrMissed };
                var drillDefend = new Drill { SessionId = session2.Id, DrillTemplateId = defendTemplate.Id, Mode = DrillMode.Manual };
                var drillPhysical = new Drill { SessionId = session2.Id, DrillTemplateId = physicalTemplate.Id, Mode = DrillMode.SuccessOrMissed };
                var drillSpeed = new Drill { SessionId = session2.Id, DrillTemplateId = speedTemplate.Id, Mode = DrillMode.SuccessOrMissed };
                context.Drills.AddRange(drillDribble, drillDefend, drillPhysical, drillSpeed);
                await context.SaveChangesAsync();

                // Drill Results - varied scores per category
                context.DrillResults.AddRange(
                    new DrillResult { DrillId = drillDribble.Id, PlayerId = playerUser.Id, DoneCount = 5, MissedCount = 5, FinalScore = 6.0m, CreatedAt = DateTime.UtcNow },
                    new DrillResult { DrillId = drillDefend.Id, PlayerId = playerUser.Id, ManualScore = 8.0m, FinalScore = 8.0m, CreatedAt = DateTime.UtcNow },
                    new DrillResult { DrillId = drillPhysical.Id, PlayerId = playerUser.Id, DoneCount = 9, MissedCount = 1, FinalScore = 9.0m, CreatedAt = DateTime.UtcNow },
                    new DrillResult { DrillId = drillSpeed.Id, PlayerId = playerUser.Id, DoneCount = 7, MissedCount = 3, FinalScore = 7.0m, CreatedAt = DateTime.UtcNow }
                );
                await context.SaveChangesAsync();

                // ===== PLAYER CARD TEST DATA: MATCHES =====
                // Tournament Match
                var tournMatch = new Match
                {
                    HomeTeamId = team.Id, AwayTeamId = awayTeam.Id,
                    Type = Domain.Enums.MatchType.Tournament, Format = MatchFormat.ElevenSide,
                    Status = MatchStatus.Completed, MatchDate = DateTime.UtcNow.AddDays(-2),
                    HomeScore = 3, AwayScore = 1, Location = "Cairo Stadium",
                    WinningTeamId = team.Id, CreatedById = coachUser.Id,
                    CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                };
                context.Matches.Add(tournMatch);
                await context.SaveChangesAsync();

                var tournRating = new MatchPlayerRating
                {
                    MatchId = tournMatch.Id, PlayerId = playerUser.Id, CoachId = coachUser.Id,
                    Goals = 1, Assists = 0, MinutesPlayed = 75, IsMOTM = false
                };
                context.MatchPlayerRatings.Add(tournRating);
                await context.SaveChangesAsync();

                // Tournament per-category expert ratings
                context.MatchPlayerCategoryRatings.AddRange(
                    new MatchPlayerCategoryRating { MatchPlayerRatingId = tournRating.Id, DrillCategoryId = shootingCategory.Id,  Rating = 7.0m },
                    new MatchPlayerCategoryRating { MatchPlayerRatingId = tournRating.Id, DrillCategoryId = passingCategory.Id,   Rating = 8.0m },
                    new MatchPlayerCategoryRating { MatchPlayerRatingId = tournRating.Id, DrillCategoryId = dribblingCat.Id,       Rating = 6.5m },
                    new MatchPlayerCategoryRating { MatchPlayerRatingId = tournRating.Id, DrillCategoryId = defendingCat.Id,       Rating = 8.0m },
                    new MatchPlayerCategoryRating { MatchPlayerRatingId = tournRating.Id, DrillCategoryId = physicalCat.Id,        Rating = 7.5m },
                    new MatchPlayerCategoryRating { MatchPlayerRatingId = tournRating.Id, DrillCategoryId = speedCat.Id,           Rating = 7.0m }
                );
                await context.SaveChangesAsync();

                // ===== SESSION MATCH TEST DATA =====
                // DrillSession for the session match
                var sessionMatchSession = new DrillSession
                {
                    AcademyId = academy.Id, TeamId = team.Id, CoachId = coachUser.Id,
                    SessionDate = DateTime.UtcNow.AddDays(-5), Type = SessionType.SessionMatch,
                    CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                };
                context.DrillSessions.Add(sessionMatchSession);
                await context.SaveChangesAsync();

                // Attendance for session match (all 3 players present)
                context.SessionAttendances.AddRange(
                    new SessionAttendance { SessionId = sessionMatchSession.Id, playerId = playerUser.Id, IsPresent = true },
                    new SessionAttendance { SessionId = sessionMatchSession.Id, playerId = player2User.Id, IsPresent = true },
                    new SessionAttendance { SessionId = sessionMatchSession.Id, playerId = player3User.Id, IsPresent = true }
                );
                await context.SaveChangesAsync();

                // --- Completed Session Match (for analytics/form-guide testing) ---
                var completedSessionMatch = new Match
                {
                    HomeTeamId = team.Id, AwayTeamId = team.Id,
                    SessionId = sessionMatchSession.Id,
                    Type = Domain.Enums.MatchType.Session, Format = MatchFormat.FiveSide,
                    Status = MatchStatus.Completed, MatchDate = DateTime.UtcNow.AddDays(-5),
                    HomeScore = 1, AwayScore = 2, Location = "Training Ground - Pitch B",
                    WinningTeamId = team.Id, CreatedById = coachUser.Id,
                    CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                };
                context.Matches.Add(completedSessionMatch);
                await context.SaveChangesAsync();

                // Lineup with IsHomeSide: Omar + Ahmed = home, Mohamed = away
                context.MatchLineups.AddRange(
                    new MatchLineup { MatchId = completedSessionMatch.Id, PlayerId = playerUser.Id, TeamId = team.Id, IsStarting = true, JerseyNumber = 9, IsHomeSide = true },
                    new MatchLineup { MatchId = completedSessionMatch.Id, PlayerId = player2User.Id, TeamId = team.Id, IsStarting = true, JerseyNumber = 7, IsHomeSide = true },
                    new MatchLineup { MatchId = completedSessionMatch.Id, PlayerId = player3User.Id, TeamId = team.Id, IsStarting = true, JerseyNumber = 5, IsHomeSide = false }
                );
                await context.SaveChangesAsync();

                // Events with IsHomeSide
                context.MatchEvents.AddRange(
                    new MatchEvent { MatchId = completedSessionMatch.Id, PlayerId = playerUser.Id, TeamId = team.Id, EventType = MatchEventType.Goal, Minute = 15, CreatedById = coachUser.Id, IsHomeSide = true },
                    new MatchEvent { MatchId = completedSessionMatch.Id, PlayerId = player3User.Id, TeamId = team.Id, EventType = MatchEventType.Goal, Minute = 28, CreatedById = coachUser.Id, IsHomeSide = false },
                    new MatchEvent { MatchId = completedSessionMatch.Id, PlayerId = player3User.Id, TeamId = team.Id, EventType = MatchEventType.Goal, Minute = 55, CreatedById = coachUser.Id, IsHomeSide = false }
                );
                await context.SaveChangesAsync();

                // Ratings
                var sessionRating = new MatchPlayerRating
                {
                    MatchId = completedSessionMatch.Id, PlayerId = playerUser.Id, CoachId = coachUser.Id,
                    Goals = 1, Assists = 0, MinutesPlayed = 60, IsMOTM = false
                };
                context.MatchPlayerRatings.Add(sessionRating);
                var sessionRating2 = new MatchPlayerRating
                {
                    MatchId = completedSessionMatch.Id, PlayerId = player2User.Id, CoachId = coachUser.Id,
                    Goals = 0, Assists = 1, MinutesPlayed = 60, IsMOTM = false
                };
                context.MatchPlayerRatings.Add(sessionRating2);
                var sessionRating3 = new MatchPlayerRating
                {
                    MatchId = completedSessionMatch.Id, PlayerId = player3User.Id, CoachId = coachUser.Id,
                    Goals = 2, Assists = 0, MinutesPlayed = 60, IsMOTM = true
                };
                context.MatchPlayerRatings.Add(sessionRating3);
                await context.SaveChangesAsync();

                // Per-category ratings for all 3 players
                context.MatchPlayerCategoryRatings.AddRange(
                    new MatchPlayerCategoryRating { MatchPlayerRatingId = sessionRating.Id, DrillCategoryId = shootingCategory.Id, Rating = 6.0m },
                    new MatchPlayerCategoryRating { MatchPlayerRatingId = sessionRating.Id, DrillCategoryId = passingCategory.Id, Rating = 7.0m },
                    new MatchPlayerCategoryRating { MatchPlayerRatingId = sessionRating2.Id, DrillCategoryId = dribblingCat.Id, Rating = 7.0m },
                    new MatchPlayerCategoryRating { MatchPlayerRatingId = sessionRating2.Id, DrillCategoryId = passingCategory.Id, Rating = 6.5m },
                    new MatchPlayerCategoryRating { MatchPlayerRatingId = sessionRating3.Id, DrillCategoryId = defendingCat.Id, Rating = 8.0m },
                    new MatchPlayerCategoryRating { MatchPlayerRatingId = sessionRating3.Id, DrillCategoryId = physicalCat.Id, Rating = 7.0m }
                );
                await context.SaveChangesAsync();

                // ===== LIVE Session Match (for event logging + ratings testing) =====
                var liveSessionMatchSession = new DrillSession
                {
                    AcademyId = academy.Id, TeamId = team.Id, CoachId = coachUser.Id,
                    SessionDate = DateTime.UtcNow, Type = SessionType.SessionMatch,
                    CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                };
                context.DrillSessions.Add(liveSessionMatchSession);
                await context.SaveChangesAsync();

                context.SessionAttendances.AddRange(
                    new SessionAttendance { SessionId = liveSessionMatchSession.Id, playerId = playerUser.Id, IsPresent = true },
                    new SessionAttendance { SessionId = liveSessionMatchSession.Id, playerId = player2User.Id, IsPresent = true },
                    new SessionAttendance { SessionId = liveSessionMatchSession.Id, playerId = player3User.Id, IsPresent = true }
                );
                await context.SaveChangesAsync();

                var liveSessionMatch = new Match
                {
                    HomeTeamId = team.Id, AwayTeamId = team.Id,
                    SessionId = liveSessionMatchSession.Id,
                    Type = Domain.Enums.MatchType.Session, Format = MatchFormat.FiveSide,
                    Status = MatchStatus.Live, MatchDate = DateTime.UtcNow,
                    HomeScore = 0, AwayScore = 0, Location = "Training Ground - Pitch A",
                    WinningTeamId = null, CreatedById = coachUser.Id,
                    CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                };
                context.Matches.Add(liveSessionMatch);
                await context.SaveChangesAsync();

                context.MatchLineups.AddRange(
                    new MatchLineup { MatchId = liveSessionMatch.Id, PlayerId = playerUser.Id, TeamId = team.Id, IsStarting = true, JerseyNumber = 9, IsHomeSide = true },
                    new MatchLineup { MatchId = liveSessionMatch.Id, PlayerId = player2User.Id, TeamId = team.Id, IsStarting = true, JerseyNumber = 7, IsHomeSide = true },
                    new MatchLineup { MatchId = liveSessionMatch.Id, PlayerId = player3User.Id, TeamId = team.Id, IsStarting = true, JerseyNumber = 4, IsHomeSide = false }
                );
                await context.SaveChangesAsync();

                // ===== GK PLAYER TEST DATA =====
                var gkUser = new User
                {
                    UserName = "goalkeeper@test.com", Email = "goalkeeper@test.com",
                    EmailConfirmed = true, FirstName = "Ahmed", LastName = "ElShenawy",
                    CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
                    CreatedById = adminUser.Id
                };
                await userManager.CreateAsync(gkUser, "Player@123456");
                await userManager.AddToRoleAsync(gkUser, "Player");

                await context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO Players (Id, DateOfBirth, PreferredFoot, WeakFootRating, AvailabilityStatus) VALUES ({0}, {1}, {2}, {3}, {4})",
                    gkUser.Id, new DateTime(2007, 5, 15), (int)PreferredFoot.Right, 3, (int)AvailabilityStatus.Available
                );

                context.PlayerPositions.Add(new PlayerPosition { PlayerId = gkUser.Id, Position = "GK", IsPrimary = true });
                context.PlayerAcademies.Add(new PlayerAcademy { PlayerId = gkUser.Id, AcademyId = academy.Id, JoinedAt = DateTime.UtcNow, Status = PlayerAcademyStatus.Active });
                context.PlayerTeams.Add(new PlayerTeam { PlayerId = gkUser.Id, TeamId = team.Id, JoinedAt = DateTime.UtcNow });
                context.PlayerSubscriptions.Add(new PlayerSubscription { PlayerId = gkUser.Id, AcademyId = academy.Id, Status = SubscriptionStatus.Paid, PaidAt = DateTime.UtcNow, PaidByUserId = gkUser.Id });
                await context.SaveChangesAsync();

                var gkCat = allCategories.First(c => c.Name == "GoalKeeping");
                var gkTemplate = new DrillTemplate
                {
                    CategoryId = gkCat.Id, AcademyId = null,
                    Name = "Shot Stopping Drill", DifficultyLevel = DifficultyLevel.Intermediate,
                    DrillMode = DrillMode.SuccessOrMissed, IsShared = true,
                    CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                };
                context.DrillTemplates.Add(gkTemplate);
                await context.SaveChangesAsync();

                var gkSession = new DrillSession
                {
                    AcademyId = academy.Id, TeamId = team.Id, CoachId = coachUser.Id,
                    SessionDate = DateTime.UtcNow.AddDays(-6), Type = SessionType.Regular,
                    CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                };
                context.DrillSessions.Add(gkSession);
                await context.SaveChangesAsync();

                var gkDrill = new Drill { SessionId = gkSession.Id, DrillTemplateId = gkTemplate.Id, Mode = DrillMode.SuccessOrMissed };
                context.Drills.Add(gkDrill);
                await context.SaveChangesAsync();

                context.DrillResults.Add(new DrillResult { DrillId = gkDrill.Id, PlayerId = gkUser.Id, DoneCount = 8, MissedCount = 2, FinalScore = 8.0m, CreatedAt = DateTime.UtcNow });
                await context.SaveChangesAsync();

                var gkTnMatch = new Match
                {
                    HomeTeamId = team.Id, AwayTeamId = awayTeam.Id,
                    Type = Domain.Enums.MatchType.Tournament, Format = MatchFormat.ElevenSide,
                    Status = MatchStatus.Completed, MatchDate = DateTime.UtcNow.AddDays(-1),
                    HomeScore = 2, AwayScore = 0, Location = "Cairo Stadium",
                    WinningTeamId = team.Id, CreatedById = coachUser.Id,
                    CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                };
                context.Matches.Add(gkTnMatch);
                await context.SaveChangesAsync();

                var gkRating = new MatchPlayerRating
                {
                    MatchId = gkTnMatch.Id, PlayerId = gkUser.Id, CoachId = coachUser.Id,
                    Goals = 0, Assists = 0, MinutesPlayed = 90, IsMOTM = true
                };
                context.MatchPlayerRatings.Add(gkRating);
                await context.SaveChangesAsync();

                context.MatchPlayerCategoryRatings.Add(
                    new MatchPlayerCategoryRating { MatchPlayerRatingId = gkRating.Id, DrillCategoryId = gkCat.Id, Rating = 8.5m }
                );
                await context.SaveChangesAsync();

                // === RECALCULATE PLAYER CARDS ===
                // trigger recalc by calling service? No — just note that calling
                // POST api/player/{id}/card/recalculate after seeding will calculate
            }

            // ====================================================================
            // INCREMENTAL SEED DATA FOR TESTING
            // Runs every startup; skips if data already exists
            // ====================================================================

            // --- Al Ahly Extra Players (7 more, total 11 for 11-side) ---
            if (!await context.Users.AnyAsync(u => u.Email == "ahlyplayer5@test.com"))
            {
                var ahlyAcademy = await context.Academies.FirstAsync(a => a.Name == "Al Ahly Academy");
                var ahlyTeam = await context.Teams.FirstAsync(t => t.Name == "U17 Team A" && t.AcademyId == ahlyAcademy.Id);
                var adminUser = await userManager.FindByEmailAsync("admin@koralytics.com");

                var newAhly = new[]
                {
                    new { Email = "ahlyplayer5@test.com", First = "Karim", Last = "Adel", DOB = new DateTime(2008, 5, 12), Foot = PreferredFoot.Right, Weak = 2, Pos = "RB" },
                    new { Email = "ahlyplayer6@test.com", First = "Youssef", Last = "Ibrahim", DOB = new DateTime(2008, 11, 3), Foot = PreferredFoot.Left, Weak = 2, Pos = "LB" },
                    new { Email = "ahlyplayer7@test.com", First = "Tarek", Last = "Samir", DOB = new DateTime(2009, 1, 20), Foot = PreferredFoot.Right, Weak = 2, Pos = "CM" },
                    new { Email = "ahlyplayer8@test.com", First = "Amr", Last = "Zaki", DOB = new DateTime(2008, 7, 15), Foot = PreferredFoot.Left, Weak = 2, Pos = "CAM" },
                    new { Email = "ahlyplayer9@test.com", First = "Mostafa", Last = "Hassan", DOB = new DateTime(2009, 6, 28), Foot = PreferredFoot.Right, Weak = 2, Pos = "RW" },
                    new { Email = "ahlyplayer10@test.com", First = "Ibrahim", Last = "Salah", DOB = new DateTime(2008, 2, 14), Foot = PreferredFoot.Right, Weak = 2, Pos = "CB" },
                    new { Email = "ahlyplayer11@test.com", First = "Walid", Last = "Soliman", DOB = new DateTime(2009, 9, 5), Foot = PreferredFoot.Right, Weak = 2, Pos = "ST" },
                };

                foreach (var np in newAhly)
                {
                    var user = new User
                    {
                        UserName = np.Email, Email = np.Email, EmailConfirmed = true,
                        FirstName = np.First, LastName = np.Last,
                        CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedById = adminUser!.Id
                    };
                    await userManager.CreateAsync(user, "Player@123456");
                    await userManager.AddToRoleAsync(user, "Player");

                    await context.Database.ExecuteSqlRawAsync(
                        "INSERT INTO Players (Id, DateOfBirth, PreferredFoot, WeakFootRating, AvailabilityStatus) VALUES ({0}, {1}, {2}, {3}, {4})",
                        user.Id, np.DOB, (int)np.Foot, np.Weak, (int)AvailabilityStatus.Available);

                    context.PlayerTeams.Add(new PlayerTeam { PlayerId = user.Id, TeamId = ahlyTeam.Id, JoinedAt = DateTime.UtcNow });
                    context.PlayerAcademies.Add(new PlayerAcademy { PlayerId = user.Id, AcademyId = ahlyAcademy.Id, JoinedAt = DateTime.UtcNow, Status = PlayerAcademyStatus.Active });
                    context.PlayerPositions.Add(new PlayerPosition { PlayerId = user.Id, Position = np.Pos, IsPrimary = true });
                    context.PlayerSubscriptions.Add(new PlayerSubscription { PlayerId = user.Id, AcademyId = ahlyAcademy.Id, Status = SubscriptionStatus.Paid, PaidAt = DateTime.UtcNow, PaidByUserId = user.Id });
                }
                await context.SaveChangesAsync();

                // Enhance existing live session match with 2 more players
                var liveSession = await context.Matches
                    .FirstAsync(m => m.Location == "Training Ground - Pitch A" && m.Type == Domain.Enums.MatchType.Session);
                var liveDrillSession = await context.DrillSessions.FirstAsync(s => s.Id == liveSession.SessionId);
                var extraP1 = await context.Users.FirstAsync(p => p.Email == "ahlyplayer5@test.com");
                var extraP2 = await context.Users.FirstAsync(p => p.Email == "ahlyplayer6@test.com");

                if (!await context.SessionAttendances.AnyAsync(a => a.SessionId == liveDrillSession.Id && a.playerId == extraP1.Id))
                {
                    context.SessionAttendances.AddRange(
                        new SessionAttendance { SessionId = liveDrillSession.Id, playerId = extraP1.Id, IsPresent = true },
                        new SessionAttendance { SessionId = liveDrillSession.Id, playerId = extraP2.Id, IsPresent = true }
                    );
                    await context.SaveChangesAsync();

                    context.MatchLineups.AddRange(
                        new MatchLineup { MatchId = liveSession.Id, PlayerId = extraP1.Id, TeamId = ahlyTeam.Id, IsStarting = true, JerseyNumber = 10, IsHomeSide = true },
                        new MatchLineup { MatchId = liveSession.Id, PlayerId = extraP2.Id, TeamId = ahlyTeam.Id, IsStarting = true, JerseyNumber = 3, IsHomeSide = false }
                    );
                    await context.SaveChangesAsync();
                }
            }

            // --- Zamalek Players (11, full roster) + Coach ---
            if (!await context.Users.AnyAsync(u => u.Email == "zamalekplayer1@test.com"))
            {
                var zamAcademy = await context.Academies.FirstAsync(a => a.Name == "Zamalek Academy");
                var zamTeam = await context.Teams.FirstAsync(t => t.Name == "U17 Team A" && t.AcademyId == zamAcademy.Id);
                var adminUser = await userManager.FindByEmailAsync("admin@koralytics.com");

                var newZam = new[]
                {
                    new { Email = "zamalekplayer1@test.com", First = "Mahmoud", Last = "Fathy", DOB = new DateTime(2007, 8, 20), Foot = PreferredFoot.Right, Weak = 3, Pos = "GK" },
                    new { Email = "zamalekplayer2@test.com", First = "Hossam", Last = "Ghaly", DOB = new DateTime(2008, 4, 10), Foot = PreferredFoot.Right, Weak = 2, Pos = "RB" },
                    new { Email = "zamalekplayer3@test.com", First = "Ahmed", Last = "Fathi", DOB = new DateTime(2008, 6, 17), Foot = PreferredFoot.Right, Weak = 2, Pos = "CB" },
                    new { Email = "zamalekplayer4@test.com", First = "Mohamed", Last = "Shawky", DOB = new DateTime(2008, 10, 25), Foot = PreferredFoot.Left, Weak = 2, Pos = "CB" },
                    new { Email = "zamalekplayer5@test.com", First = "Mohamed", Last = "Abdelwahab", DOB = new DateTime(2009, 2, 8), Foot = PreferredFoot.Left, Weak = 2, Pos = "LB" },
                    new { Email = "zamalekplayer6@test.com", First = "Ayman", Last = "Hefny", DOB = new DateTime(2008, 9, 12), Foot = PreferredFoot.Right, Weak = 2, Pos = "CM" },
                    new { Email = "zamalekplayer7@test.com", First = "Shikabala", Last = "Mahmoud", DOB = new DateTime(2008, 3, 30), Foot = PreferredFoot.Left, Weak = 3, Pos = "CAM" },
                    new { Email = "zamalekplayer8@test.com", First = "Mostafa", Last = "Mohamed", DOB = new DateTime(2009, 7, 14), Foot = PreferredFoot.Right, Weak = 2, Pos = "RW" },
                    new { Email = "zamalekplayer9@test.com", First = "Ahmed", Last = "Samir", DOB = new DateTime(2008, 12, 1), Foot = PreferredFoot.Left, Weak = 2, Pos = "LW" },
                    new { Email = "zamalekplayer10@test.com", First = "Omar", Last = "Gaber", DOB = new DateTime(2008, 1, 5), Foot = PreferredFoot.Right, Weak = 2, Pos = "ST" },
                    new { Email = "zamalekplayer11@test.com", First = "Kahraba", Last = "Mahmoud", DOB = new DateTime(2009, 4, 18), Foot = PreferredFoot.Left, Weak = 3, Pos = "ST" },
                };

                foreach (var np in newZam)
                {
                    var user = new User
                    {
                        UserName = np.Email, Email = np.Email, EmailConfirmed = true,
                        FirstName = np.First, LastName = np.Last,
                        CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedById = adminUser!.Id
                    };
                    await userManager.CreateAsync(user, "Player@123456");
                    await userManager.AddToRoleAsync(user, "Player");

                    await context.Database.ExecuteSqlRawAsync(
                        "INSERT INTO Players (Id, DateOfBirth, PreferredFoot, WeakFootRating, AvailabilityStatus) VALUES ({0}, {1}, {2}, {3}, {4})",
                        user.Id, np.DOB, (int)np.Foot, np.Weak, (int)AvailabilityStatus.Available);

                    context.PlayerTeams.Add(new PlayerTeam { PlayerId = user.Id, TeamId = zamTeam.Id, JoinedAt = DateTime.UtcNow });
                    context.PlayerAcademies.Add(new PlayerAcademy { PlayerId = user.Id, AcademyId = zamAcademy.Id, JoinedAt = DateTime.UtcNow, Status = PlayerAcademyStatus.Active });
                    context.PlayerPositions.Add(new PlayerPosition { PlayerId = user.Id, Position = np.Pos, IsPrimary = true });
                    context.PlayerSubscriptions.Add(new PlayerSubscription { PlayerId = user.Id, AcademyId = zamAcademy.Id, Status = SubscriptionStatus.Paid, PaidAt = DateTime.UtcNow, PaidByUserId = user.Id });
                }
                await context.SaveChangesAsync();

                // Zamalek Coach
                var zamCoachEmail = "zamalekcoach@test.com";
                if (!await context.Users.AnyAsync(u => u.Email == zamCoachEmail))
                {
                    var zamCoachUser = new User
                    {
                        UserName = zamCoachEmail, Email = zamCoachEmail, EmailConfirmed = true,
                        FirstName = "Mahmoud", LastName = "ElKhatib",
                        CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedById = adminUser!.Id
                    };
                    await userManager.CreateAsync(zamCoachUser, "Coach@123456");
                    await userManager.AddToRoleAsync(zamCoachUser, "Coach");

                    await context.Database.ExecuteSqlRawAsync("INSERT INTO Coaches (Id) VALUES ({0})", zamCoachUser.Id);

                    context.CoachAcademies.Add(new CoachAcademy { CoachUserId = zamCoachUser.Id, AcademyId = zamAcademy.Id, JoinedAt = DateTime.UtcNow });
                    context.CoachTeams.Add(new CoachTeam { CoachUserId = zamCoachUser.Id, TeamId = zamTeam.Id, AssignedAt = DateTime.UtcNow });
                    await context.SaveChangesAsync();
                }
            }

            // ===== TEST MATCH A: 11-side Scheduled (test: lineup + start + events + end + ratings) =====
            if (!await context.Matches.AnyAsync(m => m.Location == "Test - 11v11 Scheduled"))
            {
                var ahlyAc = await context.Academies.FirstAsync(a => a.Name == "Al Ahly Academy");
                var zamAc = await context.Academies.FirstAsync(a => a.Name == "Zamalek Academy");
                var ahlyTeam = await context.Teams.FirstAsync(t => t.Name == "U17 Team A" && t.AcademyId == ahlyAc.Id);
                var zamTeam = await context.Teams.FirstAsync(t => t.Name == "U17 Team A" && t.AcademyId == zamAc.Id);
                var coachUser = await userManager.FindByEmailAsync("coach@test.com");

                context.Matches.Add(new Match
                {
                    HomeTeamId = ahlyTeam.Id, AwayTeamId = zamTeam.Id,
                    Type = Domain.Enums.MatchType.Friendly, Format = MatchFormat.ElevenSide,
                    Status = MatchStatus.Scheduled, MatchDate = DateTime.UtcNow.AddDays(7),
                    HomeScore = 0, AwayScore = 0, Location = "Test - 11v11 Scheduled",
                    CreatedById = coachUser!.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            // ===== TEST MATCH B: 11-side Completed with lineups + events, NO ratings (test: ratings post-completion) =====
            if (!await context.Matches.AnyAsync(m => m.Location == "Test - 11v11 Completed"))
            {
                var ahlyAc = await context.Academies.FirstAsync(a => a.Name == "Al Ahly Academy");
                var zamAc = await context.Academies.FirstAsync(a => a.Name == "Zamalek Academy");
                var ahlyTeam = await context.Teams.FirstAsync(t => t.Name == "U17 Team A" && t.AcademyId == ahlyAc.Id);
                var zamTeam = await context.Teams.FirstAsync(t => t.Name == "U17 Team A" && t.AcademyId == zamAc.Id);
                var coachUser = await userManager.FindByEmailAsync("coach@test.com");

                var matchB = new Match
                {
                    HomeTeamId = ahlyTeam.Id, AwayTeamId = zamTeam.Id,
                    Type = Domain.Enums.MatchType.Friendly, Format = MatchFormat.ElevenSide,
                    Status = MatchStatus.Completed, MatchDate = DateTime.UtcNow.AddDays(-14),
                    HomeScore = 2, AwayScore = 1, Location = "Test - 11v11 Completed",
                    WinningTeamId = ahlyTeam.Id, CreatedById = coachUser!.Id,
                    CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                };
                context.Matches.Add(matchB);
                await context.SaveChangesAsync();

                // Fetch all players for lineups
                var ahlyEmails = new[] { "goalkeeper@test.com", "ahlyplayer5@test.com", "ahlyplayer6@test.com", "ahlyplayer10@test.com", "player3@test.com", "ahlyplayer7@test.com", "ahlyplayer8@test.com", "ahlyplayer9@test.com", "player2@test.com", "player@test.com", "ahlyplayer11@test.com" };
                var zamEmails = new[] { "zamalekplayer1@test.com", "zamalekplayer2@test.com", "zamalekplayer3@test.com", "zamalekplayer4@test.com", "zamalekplayer5@test.com", "zamalekplayer6@test.com", "zamalekplayer7@test.com", "zamalekplayer8@test.com", "zamalekplayer9@test.com", "zamalekplayer10@test.com", "zamalekplayer11@test.com" };

                var ahlyPlayers = await context.Users.Where(p => ahlyEmails.Contains(p.Email)).ToListAsync();
                var zamPlayers = await context.Users.Where(p => zamEmails.Contains(p.Email)).ToListAsync();

                // Al Ahly lineup: jersey numbers in order: 1,2,3,4,5,8,10,11,7,9,17
                var ahlyJerseys = new int?[] { 1, 2, 3, 4, 5, 8, 10, 11, 7, 9, 17 };
                // Zamalek lineup: jersey numbers in order: 1,2,4,5,3,8,10,11,7,9,17
                var zamJerseys = new int?[] { 1, 2, 4, 5, 3, 8, 10, 11, 7, 9, 17 };

                for (int i = 0; i < ahlyPlayers.Count; i++)
                {
                    context.MatchLineups.Add(new MatchLineup
                    {
                        MatchId = matchB.Id, PlayerId = ahlyPlayers[i].Id, TeamId = ahlyTeam.Id,
                        IsStarting = true, JerseyNumber = ahlyJerseys[i], IsHomeSide = null
                    });
                }
                for (int i = 0; i < zamPlayers.Count; i++)
                {
                    context.MatchLineups.Add(new MatchLineup
                    {
                        MatchId = matchB.Id, PlayerId = zamPlayers[i].Id, TeamId = zamTeam.Id,
                        IsStarting = true, JerseyNumber = zamJerseys[i], IsHomeSide = null
                    });
                }
                await context.SaveChangesAsync();

                // Events: 3 goals + 1 yellow card
                var omar = ahlyPlayers.First(p => p.Email == "player@test.com");
                var tarek = ahlyPlayers.First(p => p.Email == "ahlyplayer7@test.com");
                var ibrahim = ahlyPlayers.First(p => p.Email == "ahlyplayer10@test.com");
                var omarGaber = zamPlayers.First(p => p.Email == "zamalekplayer10@test.com");

                context.MatchEvents.AddRange(
                    new MatchEvent { MatchId = matchB.Id, PlayerId = omar.Id, TeamId = ahlyTeam.Id, EventType = MatchEventType.Goal, Minute = 12, CreatedById = coachUser.Id },
                    new MatchEvent { MatchId = matchB.Id, PlayerId = omarGaber.Id, TeamId = zamTeam.Id, EventType = MatchEventType.Goal, Minute = 30, CreatedById = coachUser.Id },
                    new MatchEvent { MatchId = matchB.Id, PlayerId = tarek.Id, TeamId = ahlyTeam.Id, EventType = MatchEventType.Goal, Minute = 45, CreatedById = coachUser.Id },
                    new MatchEvent { MatchId = matchB.Id, PlayerId = ibrahim.Id, TeamId = ahlyTeam.Id, EventType = MatchEventType.YellowCard, Minute = 65, CreatedById = coachUser.Id }
                );
                await context.SaveChangesAsync();
            }

            // ===== TEST MATCH C: 7-side Scheduled (test: lineup with 7 starting count + start) =====
            if (!await context.Matches.AnyAsync(m => m.Location == "Test - 7v7 Scheduled"))
            {
                var ahlyAc = await context.Academies.FirstAsync(a => a.Name == "Al Ahly Academy");
                var zamAc = await context.Academies.FirstAsync(a => a.Name == "Zamalek Academy");
                var ahlyTeam = await context.Teams.FirstAsync(t => t.Name == "U17 Team A" && t.AcademyId == ahlyAc.Id);
                var zamTeam = await context.Teams.FirstAsync(t => t.Name == "U17 Team A" && t.AcademyId == zamAc.Id);
                var coachUser = await userManager.FindByEmailAsync("coach@test.com");

                context.Matches.Add(new Match
                {
                    HomeTeamId = ahlyTeam.Id, AwayTeamId = zamTeam.Id,
                    Type = Domain.Enums.MatchType.Friendly, Format = MatchFormat.SevenSide,
                    Status = MatchStatus.Scheduled, MatchDate = DateTime.UtcNow.AddDays(7),
                    HomeScore = 0, AwayScore = 0, Location = "Test - 7v7 Scheduled",
                    CreatedById = coachUser!.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            // ===== TEST MATCH D: 5-side Live Session (test: session-events + end + ratings) =====
            if (!await context.Matches.AnyAsync(m => m.Location == "Test - 5v5 Live Session"))
            {
                var ahlyAc = await context.Academies.FirstAsync(a => a.Name == "Al Ahly Academy");
                var ahlyTeam = await context.Teams.FirstAsync(t => t.Name == "U17 Team A" && t.AcademyId == ahlyAc.Id);
                var coachUser = await userManager.FindByEmailAsync("coach@test.com");

                // Create drill session for the session match
                var sessionDs = new DrillSession
                {
                    AcademyId = ahlyAc.Id, TeamId = ahlyTeam.Id, CoachId = coachUser!.Id,
                    SessionDate = DateTime.UtcNow.AddDays(-1), Type = SessionType.SessionMatch,
                    CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                };
                context.DrillSessions.Add(sessionDs);
                await context.SaveChangesAsync();

                // Session attendances: 10 Al Ahly players
                var attendEmails = new[] { "player@test.com", "player2@test.com", "player3@test.com", "goalkeeper@test.com", "ahlyplayer5@test.com", "ahlyplayer6@test.com", "ahlyplayer7@test.com", "ahlyplayer8@test.com", "ahlyplayer9@test.com", "ahlyplayer10@test.com" };
                var attendPlayers = await context.Users.Where(p => attendEmails.Contains(p.Email)).ToListAsync();

                foreach (var ap in attendPlayers)
                {
                    context.SessionAttendances.Add(new SessionAttendance { SessionId = sessionDs.Id, playerId = ap.Id, IsPresent = true });
                }
                await context.SaveChangesAsync();

                var matchD = new Match
                {
                    HomeTeamId = ahlyTeam.Id, AwayTeamId = ahlyTeam.Id,
                    SessionId = sessionDs.Id,
                    Type = Domain.Enums.MatchType.Session, Format = MatchFormat.FiveSide,
                    Status = MatchStatus.Live, MatchDate = DateTime.UtcNow.AddDays(-1),
                    HomeScore = 0, AwayScore = 0, Location = "Test - 5v5 Live Session",
                    CreatedById = coachUser.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                };
                context.Matches.Add(matchD);
                await context.SaveChangesAsync();

                // Lineup: 5 home side + 5 away side
                var homeEmails = new[] { "player@test.com", "player2@test.com", "ahlyplayer5@test.com", "ahlyplayer6@test.com", "ahlyplayer7@test.com" };
                var awayEmails = new[] { "player3@test.com", "goalkeeper@test.com", "ahlyplayer8@test.com", "ahlyplayer9@test.com", "ahlyplayer10@test.com" };

                var homeSidePlayers = await context.Users.Where(p => homeEmails.Contains(p.Email)).ToListAsync();
                var awaySidePlayers = await context.Users.Where(p => awayEmails.Contains(p.Email)).ToListAsync();

                for (int i = 0; i < homeSidePlayers.Count; i++)
                {
                    context.MatchLineups.Add(new MatchLineup
                    {
                        MatchId = matchD.Id, PlayerId = homeSidePlayers[i].Id, TeamId = ahlyTeam.Id,
                        IsStarting = true, JerseyNumber = i + 1, IsHomeSide = true
                    });
                }
                for (int i = 0; i < awaySidePlayers.Count; i++)
                {
                    context.MatchLineups.Add(new MatchLineup
                    {
                        MatchId = matchD.Id, PlayerId = awaySidePlayers[i].Id, TeamId = ahlyTeam.Id,
                        IsStarting = true, JerseyNumber = i + 1, IsHomeSide = false
                    });
                }
                await context.SaveChangesAsync();
            }

            // ===== MATCH REQUEST: Pending request from Ahly Coach -> Zamalek Team =====
            if (!await context.MatchRequests.AnyAsync())
            {
                var ahlyAc = await context.Academies.FirstAsync(a => a.Name == "Al Ahly Academy");
                var zamAc = await context.Academies.FirstAsync(a => a.Name == "Zamalek Academy");
                var ahlyTeam = await context.Teams.FirstAsync(t => t.Name == "U17 Team A" && t.AcademyId == ahlyAc.Id);
                var zamTeam = await context.Teams.FirstAsync(t => t.Name == "U17 Team A" && t.AcademyId == zamAc.Id);
                var ahlyCoach = await userManager.FindByEmailAsync("coach@test.com");

                context.MatchRequests.Add(new MatchRequest
                {
                    RequesterTeamId = ahlyTeam.Id,
                    TargetTeamId = zamTeam.Id,
                    RequesterCoachId = ahlyCoach!.Id,
                    Format = MatchFormat.ElevenSide,
                    ProposedDate = DateTime.UtcNow.AddDays(14),
                    Location = "Test - Pending Friendly Request",
                    Status = MatchRequestStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            // ===== ADDITIONAL SEED DATA FOR AL AHLY ACADEMY DASHBOARD TESTING =====
            var testAcademy = await context.Academies.FirstOrDefaultAsync(a => a.Name == "Al Ahly Academy");
            if (testAcademy != null && !await context.AcademyBadges.AnyAsync(b => b.AcademyId == testAcademy.Id))
            {
                var mainAdminUser = await userManager.FindByEmailAsync("admin@koralytics.com");

                // 1. Seed Badges
                context.AcademyBadges.AddRange(
                    new AcademyBadge { AcademyId = testAcademy.Id, BadgeType = AcademyBadgeType.Premium, AwardedAt = DateTime.UtcNow },
                    new AcademyBadge { AcademyId = testAcademy.Id, BadgeType = AcademyBadgeType.TopPerformer, AwardedAt = DateTime.UtcNow },
                    new AcademyBadge { AcademyId = testAcademy.Id, BadgeType = AcademyBadgeType.Verified, AwardedAt = DateTime.UtcNow }
                );

                // 2. Extra Admin
                if (await userManager.FindByEmailAsync("admin2@test.com") == null)
                {
                    var extraAdmin = new User
                    {
                        UserName = "admin2@test.com", Email = "admin2@test.com", EmailConfirmed = true,
                        FirstName = "Second", LastName = "Admin", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedById = mainAdminUser!.Id
                    };
                    await userManager.CreateAsync(extraAdmin, "Admin@123456");
                    await userManager.AddToRoleAsync(extraAdmin, "AcademyAdmin");
                    await context.Database.ExecuteSqlRawAsync("INSERT INTO AcademyAdmins (Id, AcademyId) VALUES ({0}, {1})", extraAdmin.Id, testAcademy.Id);
                }

                // 3. Extra Coach
                if (await userManager.FindByEmailAsync("coach2@test.com") == null)
                {
                    var extraCoach = new User
                    {
                        UserName = "coach2@test.com", Email = "coach2@test.com", EmailConfirmed = true,
                        FirstName = "Hossam", LastName = "Hassan", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedById = mainAdminUser!.Id
                    };
                    await userManager.CreateAsync(extraCoach, "Coach@123456");
                    await userManager.AddToRoleAsync(extraCoach, "Coach");
                    await context.Database.ExecuteSqlRawAsync("INSERT INTO Coaches (Id) VALUES ({0})", extraCoach.Id);
                    context.CoachAcademies.Add(new CoachAcademy { CoachUserId = extraCoach.Id, AcademyId = testAcademy.Id, JoinedAt = DateTime.UtcNow });
                }

                // 4. Pending Coach Request
                if (await userManager.FindByEmailAsync("pendingcoach@test.com") == null)
                {
                    var pendingCoach = new User
                    {
                        UserName = "pendingcoach@test.com", Email = "pendingcoach@test.com", EmailConfirmed = true,
                        FirstName = "Pending", LastName = "Coach", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedById = mainAdminUser!.Id
                    };
                    await userManager.CreateAsync(pendingCoach, "Coach@123456");
                    await userManager.AddToRoleAsync(pendingCoach, "Coach");
                    await context.Database.ExecuteSqlRawAsync("INSERT INTO Coaches (Id) VALUES ({0})", pendingCoach.Id);
                    context.AcademyCoachJoinRequests.Add(new AcademyCoachJoinRequest { AcademyId = testAcademy.Id, CoachId = pendingCoach.Id, Status = JoinRequestStatus.Pending, RequestedAt = DateTime.UtcNow });
                }

                // 5. Extra Player
                if (await userManager.FindByEmailAsync("player4@test.com") == null)
                {
                    var extraPlayer = new User
                    {
                        UserName = "player4@test.com", Email = "player4@test.com", EmailConfirmed = true,
                        FirstName = "Ziad", LastName = "Tarek", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedById = mainAdminUser!.Id
                    };
                    await userManager.CreateAsync(extraPlayer, "Player@123456");
                    await userManager.AddToRoleAsync(extraPlayer, "Player");
                    await context.Database.ExecuteSqlRawAsync("INSERT INTO Players (Id, DateOfBirth, PreferredFoot, WeakFootRating, AvailabilityStatus) VALUES ({0}, {1}, {2}, {3}, {4})",
                        extraPlayer.Id, new DateTime(2008, 5, 5), (int)PreferredFoot.Right, 3, (int)AvailabilityStatus.Available);
                    context.PlayerAcademies.Add(new PlayerAcademy { PlayerId = extraPlayer.Id, AcademyId = testAcademy.Id, JoinedAt = DateTime.UtcNow, Status = PlayerAcademyStatus.Active });
                }

                // 6. Pending Player Request
                if (await userManager.FindByEmailAsync("pendingplayer@test.com") == null)
                {
                    var pendingPlayer = new User
                    {
                        UserName = "pendingplayer@test.com", Email = "pendingplayer@test.com", EmailConfirmed = true,
                        FirstName = "Pending", LastName = "Player", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedById = mainAdminUser!.Id
                    };
                    await userManager.CreateAsync(pendingPlayer, "Player@123456");
                    await userManager.AddToRoleAsync(pendingPlayer, "Player");
                    await context.Database.ExecuteSqlRawAsync("INSERT INTO Players (Id, DateOfBirth, PreferredFoot, WeakFootRating, AvailabilityStatus) VALUES ({0}, {1}, {2}, {3}, {4})",
                        pendingPlayer.Id, new DateTime(2009, 1, 1), (int)PreferredFoot.Right, 2, (int)AvailabilityStatus.Available);
                    context.AcademyPlayerJoinRequests.Add(new AcademyPlayerJoinRequest { AcademyId = testAcademy.Id, PlayerId = pendingPlayer.Id, Status = JoinRequestStatus.Pending, RequestedAt = DateTime.UtcNow });
                }

                await context.SaveChangesAsync();
            }
        }
    }
}
