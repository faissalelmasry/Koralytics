/* Koralytics Tournament Test Seed
   SQL Server / EF Core schema
   Run on DEV database only.
*/

SET NOCOUNT ON;

DECLARE @Now datetime2 = SYSUTCDATETIME();

BEGIN TRANSACTION;

BEGIN TRY

/* =========================
   USERS
========================= */

SET IDENTITY_INSERT AspNetUsers ON;

IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = 1001)
INSERT INTO AspNetUsers
(
    Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
    PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed,
    TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount,
    FirstName, LastName, ProfileImageUrl, GoogleId,
    IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById
)
VALUES
(
    1001, 'superadmin.demo@koralytics.com', 'SUPERADMIN.DEMO@KORALYTICS.COM',
    'superadmin.demo@koralytics.com', 'SUPERADMIN.DEMO@KORALYTICS.COM', 1,
    NULL, NEWID(), NEWID(), NULL, 0,
    0, NULL, 1, 0,
    'Super', 'Admin', NULL, NULL,
    0, @Now, @Now, NULL, NULL
);

IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = 1101)
INSERT INTO AspNetUsers
(
    Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
    PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed,
    TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount,
    FirstName, LastName, ProfileImageUrl, GoogleId,
    IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById
)
VALUES
(
    1101, 'coach.demo@koralytics.com', 'COACH.DEMO@KORALYTICS.COM',
    'coach.demo@koralytics.com', 'COACH.DEMO@KORALYTICS.COM', 1,
    NULL, NEWID(), NEWID(), NULL, 0,
    0, NULL, 1, 0,
    'Omar', 'Coach', NULL, NULL,
    0, @Now, @Now, NULL, NULL
);

SET IDENTITY_INSERT AspNetUsers OFF;

IF OBJECT_ID('Coaches') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Coaches WHERE Id = 1101)
    INSERT INTO Coaches (Id) VALUES (1101);
END

/* =========================
   ACADEMIES
========================= */

SET IDENTITY_INSERT Academies ON;

IF NOT EXISTS (SELECT 1 FROM Academies WHERE Id = 2001)
INSERT INTO Academies
(Id, Name, LogoUrl, PrimaryColor, SecondaryColor, FoundedAt, Status, AdminUserId, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
(2001, 'Elite Strikers Academy', NULL, '#0F766E', '#FACC15', '2018-01-01', 1, 1001, 0, @Now, @Now, NULL, NULL),
(2002, 'Future Legends FC', NULL, '#2563EB', '#F97316', '2019-01-01', 1, 1001, 0, @Now, @Now, NULL, NULL),
(2003, 'Golden Boot Academy', NULL, '#111827', '#F59E0B', '2020-01-01', 1, 1001, 0, @Now, @Now, NULL, NULL),
(2004, 'Nile Stars Academy', NULL, '#7C3AED', '#22C55E', '2021-01-01', 1, 1001, 0, @Now, @Now, NULL, NULL);

SET IDENTITY_INSERT Academies OFF;

/* =========================
   AGE GROUPS
========================= */

SET IDENTITY_INSERT AgeGroups ON;

IF NOT EXISTS (SELECT 1 FROM AgeGroups WHERE Id = 3001)
INSERT INTO AgeGroups
(Id, AcademyId, Name, MinAge, MaxAge, IsDeleted, CreatedAt, CreatedById)
VALUES
(3001, 2001, 'Under 18', 15, 18, 0, @Now, NULL),
(3002, 2002, 'Under 18', 15, 18, 0, @Now, NULL),
(3003, 2003, 'Under 18', 15, 18, 0, @Now, NULL),
(3004, 2004, 'Under 18', 15, 18, 0, @Now, NULL);

SET IDENTITY_INSERT AgeGroups OFF;

/* =========================
   LOCATIONS
========================= */

SET IDENTITY_INSERT AcademyLocations ON;

IF NOT EXISTS (SELECT 1 FROM AcademyLocations WHERE Id = 4001)
INSERT INTO AcademyLocations
(Id, AcademyId, Name, Address, City, IsMain, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
(4001, 2001, 'Main Ground', 'Nasr City Sports Zone', 'Cairo', 1, 0, @Now, @Now, NULL, NULL),
(4002, 2002, 'Main Ground', 'Smouha Youth Complex', 'Alexandria', 1, 0, @Now, @Now, NULL, NULL),
(4003, 2003, 'Main Ground', 'Dokki Sports Center', 'Giza', 1, 0, @Now, @Now, NULL, NULL),
(4004, 2004, 'Main Ground', 'Maadi Club Fields', 'Cairo', 1, 0, @Now, @Now, NULL, NULL);

SET IDENTITY_INSERT AcademyLocations OFF;

/* =========================
   TEAMS
========================= */

SET IDENTITY_INSERT Teams ON;

IF NOT EXISTS (SELECT 1 FROM Teams WHERE Id = 5001)
INSERT INTO Teams
(Id, Name, AgeGroupId, AcademyId, LocationId, CoachId, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
(5001, 'Elite Strikers U18', 3001, 2001, 4001, 1101, 0, @Now, @Now, NULL, NULL),
(5002, 'Future Legends U18', 3002, 2002, 4002, NULL, 0, @Now, @Now, NULL, NULL),
(5003, 'Golden Boot U18', 3003, 2003, 4003, NULL, 0, @Now, @Now, NULL, NULL),
(5004, 'Nile Stars U18', 3004, 2004, 4004, NULL, 0, @Now, @Now, NULL, NULL);

SET IDENTITY_INSERT Teams OFF;

IF OBJECT_ID('CoachTeams') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT CoachTeams ON;

    IF NOT EXISTS (SELECT 1 FROM CoachTeams WHERE Id = 5101)
    INSERT INTO CoachTeams
    (Id, CoachUserId, TeamId, AssignedAt, RemovedAt, IsDeleted, CreatedAt, CreatedById)
    VALUES
    (5101, 1101, 5001, @Now, NULL, 0, @Now, NULL);

    SET IDENTITY_INSERT CoachTeams OFF;
END

IF OBJECT_ID('CoachAcademies') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT CoachAcademies ON;

    IF NOT EXISTS (SELECT 1 FROM CoachAcademies WHERE Id = 5201)
    INSERT INTO CoachAcademies
    (Id, CoachUserId, AcademyId, JoinedAt, LeftAt, BiasScore, BiasLastCalculatedAt, IsDeleted, CreatedAt, CreatedById)
    VALUES
    (5201, 1101, 2001, @Now, NULL, NULL, NULL, 0, @Now, NULL);

    SET IDENTITY_INSERT CoachAcademies OFF;
END

/* =========================
   PLAYERS
========================= */

SET IDENTITY_INSERT AspNetUsers ON;

IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = 6001)
INSERT INTO AspNetUsers
(
    Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
    PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed,
    TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount,
    FirstName, LastName, ProfileImageUrl, GoogleId,
    IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById
)
VALUES
(6001, 'adam.samir@demo.com', 'ADAM.SAMIR@DEMO.COM', 'adam.samir@demo.com', 'ADAM.SAMIR@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Adam', 'Samir', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6002, 'omar.hassan@demo.com', 'OMAR.HASSAN@DEMO.COM', 'omar.hassan@demo.com', 'OMAR.HASSAN@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Omar', 'Hassan', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6003, 'youssef.adel@demo.com', 'YOUSSEF.ADEL@DEMO.COM', 'youssef.adel@demo.com', 'YOUSSEF.ADEL@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Youssef', 'Adel', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6004, 'karim.nabil@demo.com', 'KARIM.NABIL@DEMO.COM', 'karim.nabil@demo.com', 'KARIM.NABIL@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Karim', 'Nabil', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6005, 'malik.tarek@demo.com', 'MALIK.TAREK@DEMO.COM', 'malik.tarek@demo.com', 'MALIK.TAREK@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Malik', 'Tarek', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6006, 'seif.mostafa@demo.com', 'SEIF.MOSTAFA@DEMO.COM', 'seif.mostafa@demo.com', 'SEIF.MOSTAFA@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Seif', 'Mostafa', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6007, 'ali.fathy@demo.com', 'ALI.FATHY@DEMO.COM', 'ali.fathy@demo.com', 'ALI.FATHY@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Ali', 'Fathy', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6008, 'ziad.ashraf@demo.com', 'ZIAD.ASHRAF@DEMO.COM', 'ziad.ashraf@demo.com', 'ZIAD.ASHRAF@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Ziad', 'Ashraf', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6009, 'hadi.salem@demo.com', 'HADI.SALEM@DEMO.COM', 'hadi.salem@demo.com', 'HADI.SALEM@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Hadi', 'Salem', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6010, 'nour.farid@demo.com', 'NOUR.FARID@DEMO.COM', 'nour.farid@demo.com', 'NOUR.FARID@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Nour', 'Farid', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6011, 'fares.amin@demo.com', 'FARES.AMIN@DEMO.COM', 'fares.amin@demo.com', 'FARES.AMIN@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Fares', 'Amin', NULL, NULL, 0, @Now, @Now, NULL, NULL);

SET IDENTITY_INSERT AspNetUsers OFF;

SET IDENTITY_INSERT Players ON;

IF NOT EXISTS (SELECT 1 FROM Players WHERE Id = 6001)
INSERT INTO Players
(Id, DateOfBirth, Nationality, PreferredFoot, WeakFootRating, PlayStyleTag, ArchetypePlayerName, ArchetypeText, AvailabilityStatus)
VALUES
(6001, '2008-02-10', 'Egyptian', 1, 4, 'Playmaker', NULL, NULL, 1),
(6002, '2008-04-12', 'Egyptian', 1, 3, 'Finisher', NULL, NULL, 1),
(6003, '2008-06-03', 'Egyptian', 2, 4, 'Creator', NULL, NULL, 1),
(6004, '2008-01-21', 'Egyptian', 1, 3, 'Goalkeeper', NULL, NULL, 1),
(6005, '2008-09-08', 'Egyptian', 1, 3, 'Defender', NULL, NULL, 1),
(6006, '2008-11-14', 'Egyptian', 2, 3, 'Fullback', NULL, NULL, 1),
(6007, '2008-03-18', 'Egyptian', 1, 3, 'Midfielder', NULL, NULL, 1),
(6008, '2008-05-28', 'Egyptian', 1, 3, 'Winger', NULL, NULL, 1),
(6009, '2008-07-17', 'Egyptian', 2, 3, 'Striker', NULL, NULL, 1),
(6010, '2008-08-25', 'Egyptian', 1, 3, 'Center Back', NULL, NULL, 1),
(6011, '2008-10-30', 'Egyptian', 1, 3, 'Midfielder', NULL, NULL, 1);

SET IDENTITY_INSERT Players OFF;

SET IDENTITY_INSERT PlayerTeams ON;

IF NOT EXISTS (SELECT 1 FROM PlayerTeams WHERE Id = 7001)
INSERT INTO PlayerTeams
(Id, PlayerId, TeamId, JoinedAt, LeftAt, IsDeleted, CreatedAt, CreatedById)
VALUES
(7001, 6001, 5001, @Now, NULL, 0, @Now, NULL),
(7002, 6002, 5001, @Now, NULL, 0, @Now, NULL),
(7003, 6003, 5001, @Now, NULL, 0, @Now, NULL),
(7004, 6004, 5001, @Now, NULL, 0, @Now, NULL),
(7005, 6005, 5001, @Now, NULL, 0, @Now, NULL),
(7006, 6006, 5001, @Now, NULL, 0, @Now, NULL),
(7007, 6007, 5001, @Now, NULL, 0, @Now, NULL),
(7008, 6008, 5001, @Now, NULL, 0, @Now, NULL),
(7009, 6009, 5001, @Now, NULL, 0, @Now, NULL),
(7010, 6010, 5001, @Now, NULL, 0, @Now, NULL),
(7011, 6011, 5001, @Now, NULL, 0, @Now, NULL);

SET IDENTITY_INSERT PlayerTeams OFF;

IF OBJECT_ID('PlayerPositions') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT PlayerPositions ON;

    IF NOT EXISTS (SELECT 1 FROM PlayerPositions WHERE Id = 7101)
    INSERT INTO PlayerPositions
    (Id, PlayerId, Position, IsPrimary, IsDeleted, CreatedAt, CreatedById)
    VALUES
    (7101, 6001, 'CM', 1, 0, @Now, NULL),
    (7102, 6002, 'ST', 1, 0, @Now, NULL),
    (7103, 6003, 'AM', 1, 0, @Now, NULL),
    (7104, 6004, 'GK', 1, 0, @Now, NULL),
    (7105, 6005, 'CB', 1, 0, @Now, NULL),
    (7106, 6006, 'LB', 1, 0, @Now, NULL),
    (7107, 6007, 'DM', 1, 0, @Now, NULL),
    (7108, 6008, 'RW', 1, 0, @Now, NULL),
    (7109, 6009, 'ST', 1, 0, @Now, NULL),
    (7110, 6010, 'CB', 1, 0, @Now, NULL),
    (7111, 6011, 'CM', 1, 0, @Now, NULL);

    SET IDENTITY_INSERT PlayerPositions OFF;
END

/* =========================
   TOURNAMENTS
   Format: 11 = ElevenSide, 7 = SevenSide
   Structure: 1 Knockout, 2 GroupAndKnockout
   Status: 2 Registration, 3 InProgress, 4 Completed
========================= */

SET IDENTITY_INSERT Tournaments ON;

IF NOT EXISTS (SELECT 1 FROM Tournaments WHERE Id = 8001)
INSERT INTO Tournaments
(Id, Name, Format, Structure, AgeGroupId, HasTwoLegs, StartDate, EndDate, Status, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
(8001, 'Koralytics U18 Champions Cup', 11, 2, 3001, 0, '2026-07-01', '2026-07-20', 4, 0, @Now, @Now, NULL, NULL),
(8002, 'Koralytics Registration Test Cup', 11, 1, 3001, 0, '2026-08-01', '2026-08-10', 2, 0, @Now, @Now, NULL, NULL);

SET IDENTITY_INSERT Tournaments OFF;

/* =========================
   TOURNAMENT TEAMS
========================= */

SET IDENTITY_INSERT TournamentTeams ON;

IF NOT EXISTS (SELECT 1 FROM TournamentTeams WHERE Id = 8101)
INSERT INTO TournamentTeams
(Id, TournamentId, TeamId, SeedNumber, Status, RegisteredAt, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
(8101, 8001, 5001, 1, 2, @Now, 0, @Now, @Now, NULL, NULL),
(8102, 8001, 5002, 2, 2, @Now, 0, @Now, @Now, NULL, NULL),
(8103, 8001, 5003, 3, 2, @Now, 0, @Now, @Now, NULL, NULL),
(8104, 8001, 5004, 4, 2, @Now, 0, @Now, @Now, NULL, NULL),
(8201, 8002, 5001, NULL, 2, @Now, 0, @Now, @Now, NULL, NULL);

SET IDENTITY_INSERT TournamentTeams OFF;

/* =========================
   GROUPS / STANDINGS / FIXTURES
========================= */

SET IDENTITY_INSERT TournamentGroups ON;

IF NOT EXISTS (SELECT 1 FROM TournamentGroups WHERE Id = 8301)
INSERT INTO TournamentGroups
(Id, TournamentId, Name, IsDummy, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
(8301, 8001, 'Group A', 0, 0, @Now, @Now, NULL, NULL),
(8302, 8001, 'Group B', 0, 0, @Now, @Now, NULL, NULL);

SET IDENTITY_INSERT TournamentGroups OFF;

SET IDENTITY_INSERT TournamentGroupTeams ON;

IF NOT EXISTS (SELECT 1 FROM TournamentGroupTeams WHERE Id = 8311)
INSERT INTO TournamentGroupTeams
(Id, GroupId, TournamentTeamId, IsDeleted, CreatedAt, CreatedById)
VALUES
(8311, 8301, 8101, 0, @Now, NULL),
(8312, 8301, 8102, 0, @Now, NULL),
(8313, 8302, 8103, 0, @Now, NULL),
(8314, 8302, 8104, 0, @Now, NULL);

SET IDENTITY_INSERT TournamentGroupTeams OFF;

SET IDENTITY_INSERT TournamentStandings ON;

IF NOT EXISTS (SELECT 1 FROM TournamentStandings WHERE Id = 8321)
INSERT INTO TournamentStandings
(Id, GroupId, TournamentTeamId, Played, Won, Drawn, Lost, GoalsFor, GoalsAgainst, Points, IsDeleted, CreatedAt, CreatedById)
VALUES
(8321, 8301, 8101, 1, 1, 0, 0, 3, 1, 3, 0, @Now, NULL),
(8322, 8301, 8102, 1, 0, 0, 1, 1, 3, 0, 0, @Now, NULL),
(8323, 8302, 8103, 1, 0, 1, 0, 2, 2, 1, 0, @Now, NULL),
(8324, 8302, 8104, 1, 0, 1, 0, 2, 2, 1, 0, @Now, NULL);

SET IDENTITY_INSERT TournamentStandings OFF;

SET IDENTITY_INSERT TournamentRounds ON;

IF NOT EXISTS (SELECT 1 FROM TournamentRounds WHERE Id = 8401)
INSERT INTO TournamentRounds
(Id, TournamentId, Name, RoundNumber, IsDeleted, CreatedAt, CreatedById)
VALUES
(8401, 8001, 'Final', 1, 0, @Now, NULL);

SET IDENTITY_INSERT TournamentRounds OFF;

SET IDENTITY_INSERT TournamentFixtures ON;

IF NOT EXISTS (SELECT 1 FROM TournamentFixtures WHERE Id = 8501)
INSERT INTO TournamentFixtures
(Id, MatchId, GroupId, RoundId, HomeTeamId, AwayTeamId, HomeScore, AwayScore, WinnerTeamId, LegNumber, Status, IsDeleted, CreatedAt, CreatedById)
VALUES
(8501, NULL, 8301, NULL, 8101, 8102, 3, 1, 8101, 1, 3, 0, @Now, NULL),
(8502, NULL, 8302, NULL, 8103, 8104, 2, 2, NULL, 1, 3, 0, @Now, NULL),
(8503, NULL, NULL, 8401, 8101, 8103, 2, 0, 8101, 1, 3, 0, @Now, NULL);

SET IDENTITY_INSERT TournamentFixtures OFF;

/* =========================
   SQUADS / HALL OF FAME
========================= */

SET IDENTITY_INSERT TournamentSquads ON;

IF NOT EXISTS (SELECT 1 FROM TournamentSquads WHERE Id = 8601)
INSERT INTO TournamentSquads
(Id, TournamentId, TeamId, PlayerId, RegisteredAt, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
(8601, 8001, 5001, 6001, @Now, 0, @Now, @Now, NULL, NULL),
(8602, 8001, 5001, 6002, @Now, 0, @Now, @Now, NULL, NULL),
(8603, 8001, 5001, 6003, @Now, 0, @Now, @Now, NULL, NULL),
(8604, 8001, 5001, 6004, @Now, 0, @Now, @Now, NULL, NULL),
(8605, 8001, 5001, 6005, @Now, 0, @Now, @Now, NULL, NULL),
(8606, 8001, 5001, 6006, @Now, 0, @Now, @Now, NULL, NULL),
(8607, 8001, 5001, 6007, @Now, 0, @Now, @Now, NULL, NULL),
(8608, 8001, 5001, 6008, @Now, 0, @Now, @Now, NULL, NULL),
(8609, 8001, 5001, 6009, @Now, 0, @Now, @Now, NULL, NULL),
(8610, 8001, 5001, 6010, @Now, 0, @Now, @Now, NULL, NULL),
(8611, 8001, 5001, 6011, @Now, 0, @Now, @Now, NULL, NULL);

SET IDENTITY_INSERT TournamentSquads OFF;

SET IDENTITY_INSERT TournamentHallOfFames ON;

IF NOT EXISTS (SELECT 1 FROM TournamentHallOfFames WHERE Id = 8701)
INSERT INTO TournamentHallOfFames
(Id, TournamentId, PlayerId, AwardType, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
(8701, 8001, 6001, 'BestPlayer', 0, @Now, @Now, NULL, NULL),
(8702, 8001, 6002, 'TopScorer', 0, @Now, @Now, NULL, NULL),
(8703, 8001, 6003, 'MostAssists', 0, @Now, @Now, NULL, NULL),
(8704, 8001, 6004, 'BestGoalkeeper', 0, @Now, @Now, NULL, NULL),
(8705, 8001, 6008, 'MostMOTM', 0, @Now, @Now, NULL, NULL);

SET IDENTITY_INSERT TournamentHallOfFames OFF;

COMMIT TRANSACTION;

PRINT 'Koralytics tournament seed completed successfully.';
PRINT 'Try GET /api/Tournament';
PRINT 'Try GET /api/Tournament/8001';
PRINT 'Try GET /api/Tournament/8001/bracket';
PRINT 'Try GET /api/Tournament/8001/teams';
PRINT 'Try GET /api/Tournament/8001/hall-of-fame';
PRINT 'Try GET /api/Tournament/8002/teams';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;

    PRINT 'Seed failed.';
    PRINT ERROR_MESSAGE();

    THROW;
END CATCH;