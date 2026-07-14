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

                // Session Match
                var sessionMatch = new Match
                {
                    HomeTeamId = team.Id, AwayTeamId = awayTeam.Id,
                    Type = Domain.Enums.MatchType.Session, Format = MatchFormat.ElevenSide,
                    Status = MatchStatus.Completed, MatchDate = DateTime.UtcNow.AddDays(-5),
                    HomeScore = 1, AwayScore = 2, Location = "Training Ground",
                    WinningTeamId = awayTeam.Id, CreatedById = coachUser.Id,
                    CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                };
                context.Matches.Add(sessionMatch);
                await context.SaveChangesAsync();

                var sessionRating = new MatchPlayerRating
                {
                    MatchId = sessionMatch.Id, PlayerId = playerUser.Id, CoachId = coachUser.Id,
                    Goals = 0, Assists = 0, MinutesPlayed = 90, IsMOTM = false
                };
                context.MatchPlayerRatings.Add(sessionRating);
                await context.SaveChangesAsync();

                // Session per-category expert ratings
                context.MatchPlayerCategoryRatings.AddRange(
                    new MatchPlayerCategoryRating { MatchPlayerRatingId = sessionRating.Id, DrillCategoryId = shootingCategory.Id,  Rating = 6.0m },
                    new MatchPlayerCategoryRating { MatchPlayerRatingId = sessionRating.Id, DrillCategoryId = passingCategory.Id,   Rating = 7.0m },
                    new MatchPlayerCategoryRating { MatchPlayerRatingId = sessionRating.Id, DrillCategoryId = dribblingCat.Id,       Rating = 6.0m },
                    new MatchPlayerCategoryRating { MatchPlayerRatingId = sessionRating.Id, DrillCategoryId = defendingCat.Id,       Rating = 7.5m },
                    new MatchPlayerCategoryRating { MatchPlayerRatingId = sessionRating.Id, DrillCategoryId = physicalCat.Id,        Rating = 6.0m },
                    new MatchPlayerCategoryRating { MatchPlayerRatingId = sessionRating.Id, DrillCategoryId = speedCat.Id,           Rating = 6.0m }
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
        }
    }
}
