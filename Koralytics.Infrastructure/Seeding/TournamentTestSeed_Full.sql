/* =============================================================
   Koralytics — COMPLETE TOURNAMENT TEST SEED
   SQL Server / EF Core schema
   Covers ALL tournament types for API testing:
     - Knockout (8 teams)
     - Group + Knockout (8 teams, 2 groups)
     - League (4 teams)
     - Two-leg knockout (4 teams)
     - Registration-only tournaments (for draw/seeding test)
     - Completed tournaments with Hall of Fame
   =============================================================
   Run ONLY on DEV database.
   ============================================================= */

SET NOCOUNT ON;

DECLARE @Now datetime2 = SYSUTCDATETIME();

BEGIN TRANSACTION;

BEGIN TRY

/* =============================================================
   SECTION 0 — BASE DATA (Users, Academies, AgeGroups, Teams)
   Uses the same IDs as TournamentTestSeed.sql
   ============================================================= */

PRINT '=== Section 0: Base Data ===';

SET IDENTITY_INSERT AspNetUsers ON;

-- Super Admin
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = 1001)
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount, FirstName, LastName, ProfileImageUrl, GoogleId, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES (1001, 'superadmin.demo@koralytics.com', 'SUPERADMIN.DEMO@KORALYTICS.COM', 'superadmin.demo@koralytics.com', 'SUPERADMIN.DEMO@KORALYTICS.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Super', 'Admin', NULL, NULL, 0, @Now, @Now, NULL, NULL);

-- Coach
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = 1101)
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount, FirstName, LastName, ProfileImageUrl, GoogleId, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES (1101, 'coach.demo@koralytics.com', 'COACH.DEMO@KORALYTICS.COM', 'coach.demo@koralytics.com', 'COACH.DEMO@KORALYTICS.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Omar', 'Coach', NULL, NULL, 0, @Now, @Now, NULL, NULL);

SET IDENTITY_INSERT AspNetUsers OFF;

-- Ensure Coaches table has the coach
IF OBJECT_ID('Coaches') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Coaches WHERE Id = 1101)
        INSERT INTO Coaches (Id) VALUES (1101);
END

-- Academies (8 academies for tournament testing)
SET IDENTITY_INSERT Academies ON;

IF NOT EXISTS (SELECT 1 FROM Academies WHERE Id = 2001)
INSERT INTO Academies (Id, Name, LogoUrl, PrimaryColor, SecondaryColor, FoundedAt, Status, AdminUserId, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
(2001, 'Elite Strikers Academy', NULL, '#0F766E', '#FACC15', '2018-01-01', 1, 1001, 0, @Now, @Now, NULL, NULL),
(2002, 'Future Legends FC', NULL, '#2563EB', '#F97316', '2019-01-01', 1, 1001, 0, @Now, @Now, NULL, NULL),
(2003, 'Golden Boot Academy', NULL, '#111827', '#F59E0B', '2020-01-01', 1, 1001, 0, @Now, @Now, NULL, NULL),
(2004, 'Nile Stars Academy', NULL, '#7C3AED', '#22C55E', '2021-01-01', 1, 1001, 0, @Now, @Now, NULL, NULL),
(2005, 'Desert Hawks FC', NULL, '#DC2626', '#FCD34D', '2020-06-01', 1, 1001, 0, @Now, @Now, NULL, NULL),
(2006, 'Medina United Academy', NULL, '#059669', '#FBBF24', '2019-09-01', 1, 1001, 0, @Now, @Now, NULL, NULL),
(2007, 'Delta Force Academy', NULL, '#1D4ED8', '#38BDF8', '2021-03-01', 1, 1001, 0, @Now, @Now, NULL, NULL),
(2008, 'Pharaohs Soccer School', NULL, '#D97706', '#78350F', '2022-01-01', 1, 1001, 0, @Now, @Now, NULL, NULL);

SET IDENTITY_INSERT Academies OFF;

-- Age Groups (one per academy)
SET IDENTITY_INSERT AgeGroups ON;

IF NOT EXISTS (SELECT 1 FROM AgeGroups WHERE Id = 3001)
INSERT INTO AgeGroups (Id, AcademyId, Name, MinAge, MaxAge, IsDeleted, CreatedAt, CreatedById)
VALUES
(3001, 2001, 'Under 18', 15, 18, 0, @Now, NULL),
(3002, 2002, 'Under 18', 15, 18, 0, @Now, NULL),
(3003, 2003, 'Under 18', 15, 18, 0, @Now, NULL),
(3004, 2004, 'Under 18', 15, 18, 0, @Now, NULL),
(3005, 2005, 'Under 18', 15, 18, 0, @Now, NULL),
(3006, 2006, 'Under 18', 15, 18, 0, @Now, NULL),
(3007, 2007, 'Under 18', 15, 18, 0, @Now, NULL),
(3008, 2008, 'Under 18', 15, 18, 0, @Now, NULL);

SET IDENTITY_INSERT AgeGroups OFF;

-- Locations
SET IDENTITY_INSERT AcademyLocations ON;

IF NOT EXISTS (SELECT 1 FROM AcademyLocations WHERE Id = 4001)
INSERT INTO AcademyLocations (Id, AcademyId, Name, Address, City, IsMain, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
(4001, 2001, 'Main Ground', 'Nasr City Sports Zone', 'Cairo', 1, 0, @Now, @Now, NULL, NULL),
(4002, 2002, 'Main Ground', 'Smouha Youth Complex', 'Alexandria', 1, 0, @Now, @Now, NULL, NULL),
(4003, 2003, 'Main Ground', 'Dokki Sports Center', 'Giza', 1, 0, @Now, @Now, NULL, NULL),
(4004, 2004, 'Main Ground', 'Maadi Club Fields', 'Cairo', 1, 0, @Now, @Now, NULL, NULL),
(4005, 2005, 'Main Ground', 'Sheraton Sports Hub', 'Cairo', 1, 0, @Now, @Now, NULL, NULL),
(4006, 2006, 'Main Ground', 'Mohandeseen Stadium', 'Giza', 1, 0, @Now, @Now, NULL, NULL),
(4007, 2007, 'Main Ground', 'El-Max Sports Complex', 'Alexandria', 1, 0, @Now, @Now, NULL, NULL),
(4008, 2008, 'Main Ground', 'Tagamoa El-Khames Fields', 'Cairo', 1, 0, @Now, @Now, NULL, NULL);

SET IDENTITY_INSERT AcademyLocations OFF;

-- Teams (one per academy, all U18)
SET IDENTITY_INSERT Teams ON;

IF NOT EXISTS (SELECT 1 FROM Teams WHERE Id = 5001)
INSERT INTO Teams (Id, Name, AgeGroupId, AcademyId, LocationId, CoachId, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
(5001, 'Elite Strikers U18', 3001, 2001, 4001, 1101, 0, @Now, @Now, NULL, NULL),
(5002, 'Future Legends U18', 3002, 2002, 4002, NULL, 0, @Now, @Now, NULL, NULL),
(5003, 'Golden Boot U18', 3003, 2003, 4003, NULL, 0, @Now, @Now, NULL, NULL),
(5004, 'Nile Stars U18', 3004, 2004, 4004, NULL, 0, @Now, @Now, NULL, NULL),
(5005, 'Desert Hawks U18', 3005, 2005, 4005, NULL, 0, @Now, @Now, NULL, NULL),
(5006, 'Medina United U18', 3006, 2006, 4006, NULL, 0, @Now, @Now, NULL, NULL),
(5007, 'Delta Force U18', 3007, 2007, 4007, NULL, 0, @Now, @Now, NULL, NULL),
(5008, 'Pharaohs Soccer U18', 3008, 2008, 4008, NULL, 0, @Now, @Now, NULL, NULL);

SET IDENTITY_INSERT Teams OFF;

-- Assign coach to Elite Strikers
IF OBJECT_ID('CoachTeams') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT CoachTeams ON;
    IF NOT EXISTS (SELECT 1 FROM CoachTeams WHERE Id = 5101)
        INSERT INTO CoachTeams (Id, CoachUserId, TeamId, AssignedAt, RemovedAt, IsDeleted, CreatedAt, CreatedById)
        VALUES (5101, 1101, 5001, @Now, NULL, 0, @Now, NULL);
    SET IDENTITY_INSERT CoachTeams OFF;
END

IF OBJECT_ID('CoachAcademies') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT CoachAcademies ON;
    IF NOT EXISTS (SELECT 1 FROM CoachAcademies WHERE Id = 5201)
        INSERT INTO CoachAcademies (Id, CoachUserId, AcademyId, JoinedAt, LeftAt, BiasScore, BiasLastCalculatedAt, IsDeleted, CreatedAt, CreatedById)
        VALUES (5201, 1101, 2001, @Now, NULL, NULL, NULL, 0, @Now, NULL);
    SET IDENTITY_INSERT CoachAcademies OFF;
END

-- Players (11 per team = 88 players across 8 teams)
SET IDENTITY_INSERT AspNetUsers ON;

-- Elite Strikers Players (6001-6011)
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = 6001)
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount, FirstName, LastName, ProfileImageUrl, GoogleId, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
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

-- Future Legends Players (6101-6111)
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = 6101)
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount, FirstName, LastName, ProfileImageUrl, GoogleId, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
(6101, 'player.fl1@demo.com', 'PLAYER.FL1@DEMO.COM', 'player.fl1@demo.com', 'PLAYER.FL1@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Ali', 'Youssef', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6102, 'player.fl2@demo.com', 'PLAYER.FL2@DEMO.COM', 'player.fl2@demo.com', 'PLAYER.FL2@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Khaled', 'Saeed', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6103, 'player.fl3@demo.com', 'PLAYER.FL3@DEMO.COM', 'player.fl3@demo.com', 'PLAYER.FL3@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Hassan', 'Mansour', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6104, 'player.fl4@demo.com', 'PLAYER.FL4@DEMO.COM', 'player.fl4@demo.com', 'PLAYER.FL4@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Mohanad', 'Lotfy', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6105, 'player.fl5@demo.com', 'PLAYER.FL5@DEMO.COM', 'player.fl5@demo.com', 'PLAYER.FL5@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Sherif', 'Nasr', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6106, 'player.fl6@demo.com', 'PLAYER.FL6@DEMO.COM', 'player.fl6@demo.com', 'PLAYER.FL6@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Osama', 'Rashid', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6107, 'player.fl7@demo.com', 'PLAYER.FL7@DEMO.COM', 'player.fl7@demo.com', 'PLAYER.FL7@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Gamal', 'Atef', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6108, 'player.fl8@demo.com', 'PLAYER.FL8@DEMO.COM', 'player.fl8@demo.com', 'PLAYER.FL8@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Ramy', 'Sabry', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6109, 'player.fl9@demo.com', 'PLAYER.FL9@DEMO.COM', 'player.fl9@demo.com', 'PLAYER.FL9@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Tamer', 'Fikry', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6110, 'player.fl10@demo.com', 'PLAYER.FL10@DEMO.COM', 'player.fl10@demo.com', 'PLAYER.FL10@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Sameh', 'Ghaly', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6111, 'player.fl11@demo.com', 'PLAYER.FL11@DEMO.COM', 'player.fl11@demo.com', 'PLAYER.FL11@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Hany', 'Shaker', NULL, NULL, 0, @Now, @Now, NULL, NULL);

-- Golden Boot Players (6201-6211)
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = 6201)
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount, FirstName, LastName, ProfileImageUrl, GoogleId, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
(6201, 'player.gb1@demo.com', 'PLAYER.GB1@DEMO.COM', 'player.gb1@demo.com', 'PLAYER.GB1@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Mahmoud', 'Elsayed', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6202, 'player.gb2@demo.com', 'PLAYER.GB2@DEMO.COM', 'player.gb2@demo.com', 'PLAYER.GB2@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Ehab', 'Galal', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6203, 'player.gb3@demo.com', 'PLAYER.GB3@DEMO.COM', 'player.gb3@demo.com', 'PLAYER.GB3@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Nader', 'Soliman', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6204, 'player.gb4@demo.com', 'PLAYER.GB4@DEMO.COM', 'player.gb4@demo.com', 'PLAYER.GB4@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Waleed', 'Salah', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6205, 'player.gb5@demo.com', 'PLAYER.GB5@DEMO.COM', 'player.gb5@demo.com', 'PLAYER.GB5@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Ashraf', 'Fawzy', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6206, 'player.gb6@demo.com', 'PLAYER.GB6@DEMO.COM', 'player.gb6@demo.com', 'PLAYER.GB6@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Salah', 'Elsaeed', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6207, 'player.gb7@demo.com', 'PLAYER.GB7@DEMO.COM', 'player.gb7@demo.com', 'PLAYER.GB7@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Belal', 'Othman', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6208, 'player.gb8@demo.com', 'PLAYER.GB8@DEMO.COM', 'player.gb8@demo.com', 'PLAYER.GB8@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Moatasem', 'Ibrahim', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6209, 'player.gb9@demo.com', 'PLAYER.GB9@DEMO.COM', 'player.gb9@demo.com', 'PLAYER.GB9@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Emad', 'Hossam', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6210, 'player.gb10@demo.com', 'PLAYER.GB10@DEMO.COM', 'player.gb10@demo.com', 'PLAYER.GB10@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Zakaria', 'Nabil', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6211, 'player.gb11@demo.com', 'PLAYER.GB11@DEMO.COM', 'player.gb11@demo.com', 'PLAYER.GB11@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Hisham', 'Gamal', NULL, NULL, 0, @Now, @Now, NULL, NULL);

-- Nile Stars Players (6301-6311)
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = 6301)
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount, FirstName, LastName, ProfileImageUrl, GoogleId, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
(6301, 'player.ns1@demo.com', 'PLAYER.NS1@DEMO.COM', 'player.ns1@demo.com', 'PLAYER.NS1@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Mina', 'Bishoy', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6302, 'player.ns2@demo.com', 'PLAYER.NS2@DEMO.COM', 'player.ns2@demo.com', 'PLAYER.NS2@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Kyrillos', 'Girgis', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6303, 'player.ns3@demo.com', 'PLAYER.NS3@DEMO.COM', 'player.ns3@demo.com', 'PLAYER.NS3@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Bishoy', 'Wahba', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6304, 'player.ns4@demo.com', 'PLAYER.NS4@DEMO.COM', 'player.ns4@demo.com', 'PLAYER.NS4@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Peter', 'Hany', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6305, 'player.ns5@demo.com', 'PLAYER.NS5@DEMO.COM', 'player.ns5@demo.com', 'PLAYER.NS5@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Micheal', 'Adel', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6306, 'player.ns6@demo.com', 'PLAYER.NS6@DEMO.COM', 'player.ns6@demo.com', 'PLAYER.NS6@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'George', 'Samir', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6307, 'player.ns7@demo.com', 'PLAYER.NS7@DEMO.COM', 'player.ns7@demo.com', 'PLAYER.NS7@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Fady', 'Magdy', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6308, 'player.ns8@demo.com', 'PLAYER.NS8@DEMO.COM', 'player.ns8@demo.com', 'PLAYER.NS8@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Marian', 'Gamil', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6309, 'player.ns9@demo.com', 'PLAYER.NS9@DEMO.COM', 'player.ns9@demo.com', 'PLAYER.NS9@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Ramy', 'Fikry', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6310, 'player.ns10@demo.com', 'PLAYER.NS10@DEMO.COM', 'player.ns10@demo.com', 'PLAYER.NS10@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Yehia', 'Karam', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6311, 'player.ns11@demo.com', 'PLAYER.NS11@DEMO.COM', 'player.ns11@demo.com', 'PLAYER.NS11@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Amgad', 'Wagdy', NULL, NULL, 0, @Now, @Now, NULL, NULL);

-- Desert Hawks Players (6401-6411)
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = 6401)
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount, FirstName, LastName, ProfileImageUrl, GoogleId, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
(6401, 'player.dh1@demo.com', 'PLAYER.DH1@DEMO.COM', 'player.dh1@demo.com', 'PLAYER.DH1@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Samer', 'William', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6402, 'player.dh2@demo.com', 'PLAYER.DH2@DEMO.COM', 'player.dh2@demo.com', 'PLAYER.DH2@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Ayman', 'Rizk', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6403, 'player.dh3@demo.com', 'PLAYER.DH3@DEMO.COM', 'player.dh3@demo.com', 'PLAYER.DH3@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Gerges', 'Mina', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6404, 'player.dh4@demo.com', 'PLAYER.DH4@DEMO.COM', 'player.dh4@demo.com', 'PLAYER.DH4@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Maged', 'Fouad', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6405, 'player.dh5@demo.com', 'PLAYER.DH5@DEMO.COM', 'player.dh5@demo.com', 'PLAYER.DH5@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Rashad', 'Hegazy', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6406, 'player.dh6@demo.com', 'PLAYER.DH6@DEMO.COM', 'player.dh6@demo.com', 'PLAYER.DH6@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Lotfy', 'Yehia', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6407, 'player.dh7@demo.com', 'PLAYER.DH7@DEMO.COM', 'player.dh7@demo.com', 'PLAYER.DH7@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Mamdouh', 'Abbas', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6408, 'player.dh8@demo.com', 'PLAYER.DH8@DEMO.COM', 'player.dh8@demo.com', 'PLAYER.DH8@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Saad', 'El-Din', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6409, 'player.dh9@demo.com', 'PLAYER.DH9@DEMO.COM', 'player.dh9@demo.com', 'PLAYER.DH9@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Morsy', 'Kamel', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6410, 'player.dh10@demo.com', 'PLAYER.DH10@DEMO.COM', 'player.dh10@demo.com', 'PLAYER.DH10@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Hany', 'Saeed', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6411, 'player.dh11@demo.com', 'PLAYER.DH11@DEMO.COM', 'player.dh11@demo.com', 'PLAYER.DH11@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Taher', 'Ramadan', NULL, NULL, 0, @Now, @Now, NULL, NULL);

-- Medina United Players (6501-6511)
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = 6501)
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount, FirstName, LastName, ProfileImageUrl, GoogleId, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
(6501, 'player.mu1@demo.com', 'PLAYER.MU1@DEMO.COM', 'player.mu1@demo.com', 'PLAYER.MU1@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Adham', 'Khaled', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6502, 'player.mu2@demo.com', 'PLAYER.MU2@DEMO.COM', 'player.mu2@demo.com', 'PLAYER.MU2@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Yasser', 'Naguib', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6503, 'player.mu3@demo.com', 'PLAYER.MU3@DEMO.COM', 'player.mu3@demo.com', 'PLAYER.MU3@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Bahaa', 'Samy', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6504, 'player.mu4@demo.com', 'PLAYER.MU4@DEMO.COM', 'player.mu4@demo.com', 'PLAYER.MU4@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Essam', 'Khalifa', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6505, 'player.mu5@demo.com', 'PLAYER.MU5@DEMO.COM', 'player.mu5@demo.com', 'PLAYER.MU5@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Mohab', 'Hesham', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6506, 'player.mu6@demo.com', 'PLAYER.MU6@DEMO.COM', 'player.mu6@demo.com', 'PLAYER.MU6@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Alaa', 'Mahrous', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6507, 'player.mu7@demo.com', 'PLAYER.MU7@DEMO.COM', 'player.mu7@demo.com', 'PLAYER.MU7@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Refaat', 'El-Gammal', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6508, 'player.mu8@demo.com', 'PLAYER.MU8@DEMO.COM', 'player.mu8@demo.com', 'PLAYER.MU8@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Atef', 'Shawky', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6509, 'player.mu9@demo.com', 'PLAYER.MU9@DEMO.COM', 'player.mu9@demo.com', 'PLAYER.MU9@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Nasr', 'El-Din', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6510, 'player.mu10@demo.com', 'PLAYER.MU10@DEMO.COM', 'player.mu10@demo.com', 'PLAYER.MU10@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Hazem', 'Ihab', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6511, 'player.mu11@demo.com', 'PLAYER.MU11@DEMO.COM', 'player.mu11@demo.com', 'PLAYER.MU11@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Badr', 'Mounir', NULL, NULL, 0, @Now, @Now, NULL, NULL);

-- Delta Force Players (6601-6611)
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = 6601)
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount, FirstName, LastName, ProfileImageUrl, GoogleId, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
(6601, 'player.df1@demo.com', 'PLAYER.DF1@DEMO.COM', 'player.df1@demo.com', 'PLAYER.DF1@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Galal', 'Salah', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6602, 'player.df2@demo.com', 'PLAYER.DF2@DEMO.COM', 'player.df2@demo.com', 'PLAYER.DF2@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Hamada', 'Shabaan', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6603, 'player.df3@demo.com', 'PLAYER.DF3@DEMO.COM', 'player.df3@demo.com', 'PLAYER.DF3@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Ismail', 'Yassin', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6604, 'player.df4@demo.com', 'PLAYER.DF4@DEMO.COM', 'player.df4@demo.com', 'PLAYER.DF4@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Jamal', 'Eid', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6605, 'player.df5@demo.com', 'PLAYER.DF5@DEMO.COM', 'player.df5@demo.com', 'PLAYER.DF5@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Karam', 'Farouk', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6606, 'player.df6@demo.com', 'PLAYER.DF6@DEMO.COM', 'player.df6@demo.com', 'PLAYER.DF6@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Mamoun', 'Abdelaziz', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6607, 'player.df7@demo.com', 'PLAYER.DF7@DEMO.COM', 'player.df7@demo.com', 'PLAYER.DF7@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Nabil', 'Farag', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6608, 'player.df8@demo.com', 'PLAYER.DF8@DEMO.COM', 'player.df8@demo.com', 'PLAYER.DF8@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Raafat', 'Moussa', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6609, 'player.df9@demo.com', 'PLAYER.DF9@DEMO.COM', 'player.df9@demo.com', 'PLAYER.DF9@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Said', 'Khalil', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6610, 'player.df10@demo.com', 'PLAYER.DF10@DEMO.COM', 'player.df10@demo.com', 'PLAYER.DF10@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Wagdy', 'El-Sayed', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6611, 'player.df11@demo.com', 'PLAYER.DF11@DEMO.COM', 'player.df11@demo.com', 'PLAYER.DF11@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Zaki', 'Hassan', NULL, NULL, 0, @Now, @Now, NULL, NULL);

-- Pharaohs Soccer Players (6701-6711)
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = 6701)
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount, FirstName, LastName, ProfileImageUrl, GoogleId, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
(6701, 'player.ps1@demo.com', 'PLAYER.PS1@DEMO.COM', 'player.ps1@demo.com', 'PLAYER.PS1@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Abdelrahman', 'Magdy', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6702, 'player.ps2@demo.com', 'PLAYER.PS2@DEMO.COM', 'player.ps2@demo.com', 'PLAYER.PS2@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Bassem', 'Arafa', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6703, 'player.ps3@demo.com', 'PLAYER.PS3@DEMO.COM', 'player.ps3@demo.com', 'PLAYER.PS3@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Diab', 'Ramadan', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6704, 'player.ps4@demo.com', 'PLAYER.PS4@DEMO.COM', 'player.ps4@demo.com', 'PLAYER.PS4@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Elsayed', 'Hany', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6705, 'player.ps5@demo.com', 'PLAYER.PS5@DEMO.COM', 'player.ps5@demo.com', 'PLAYER.PS5@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Fathy', 'Abdelaziz', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6706, 'player.ps6@demo.com', 'PLAYER.PS6@DEMO.COM', 'player.ps6@demo.com', 'PLAYER.PS6@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Hamdy', 'Taha', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6707, 'player.ps7@demo.com', 'PLAYER.PS7@DEMO.COM', 'player.ps7@demo.com', 'PLAYER.PS7@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Ihab', 'Nasser', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6708, 'player.ps8@demo.com', 'PLAYER.PS8@DEMO.COM', 'player.ps8@demo.com', 'PLAYER.PS8@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Kamal', 'Saber', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6709, 'player.ps9@demo.com', 'PLAYER.PS9@DEMO.COM', 'player.ps9@demo.com', 'PLAYER.PS9@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Labib', 'Gerges', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6710, 'player.ps10@demo.com', 'PLAYER.PS10@DEMO.COM', 'player.ps10@demo.com', 'PLAYER.PS10@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Mounir', 'Samy', NULL, NULL, 0, @Now, @Now, NULL, NULL),
(6711, 'player.ps11@demo.com', 'PLAYER.PS11@DEMO.COM', 'player.ps11@demo.com', 'PLAYER.PS11@DEMO.COM', 1, NULL, NEWID(), NEWID(), NULL, 0, 0, NULL, 1, 0, 'Reda', 'Shehata', NULL, NULL, 0, @Now, @Now, NULL, NULL);

SET IDENTITY_INSERT AspNetUsers OFF;

-- Elite Strikers Players (6001-6011) in Players table
IF NOT EXISTS (SELECT 1 FROM Players WHERE Id = 6001)
INSERT INTO Players (Id, DateOfBirth, Nationality, PreferredFoot, WeakFootRating, PlayStyleTag, ArchetypePlayerName, ArchetypeText, AvailabilityStatus)
VALUES
(6001, '2008-02-14', 'Egyptian', 1, 3, 'Midfielder', NULL, NULL, 1),
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

-- Future Legends Players
IF NOT EXISTS (SELECT 1 FROM Players WHERE Id = 6101)
INSERT INTO Players (Id, DateOfBirth, Nationality, PreferredFoot, WeakFootRating, PlayStyleTag, ArchetypePlayerName, ArchetypeText, AvailabilityStatus)
VALUES
(6101, '2008-03-15', 'Egyptian', 1, 3, 'Goalkeeper', NULL, NULL, 1),
(6102, '2008-06-22', 'Egyptian', 1, 2, 'Defender', NULL, NULL, 1),
(6103, '2008-01-10', 'Egyptian', 2, 3, 'Fullback', NULL, NULL, 1),
(6104, '2008-09-05', 'Egyptian', 1, 3, 'Midfielder', NULL, NULL, 1),
(6105, '2008-04-18', 'Egyptian', 1, 2, 'Center Back', NULL, NULL, 1),
(6106, '2008-11-30', 'Egyptian', 2, 3, 'Winger', NULL, NULL, 1),
(6107, '2008-02-14', 'Egyptian', 1, 3, 'Playmaker', NULL, NULL, 1),
(6108, '2008-07-28', 'Egyptian', 1, 2, 'Striker', NULL, NULL, 1),
(6109, '2008-05-09', 'Egyptian', 2, 3, 'Creator', NULL, NULL, 1),
(6110, '2008-08-19', 'Egyptian', 1, 3, 'Finisher', NULL, NULL, 1),
(6111, '2008-10-12', 'Egyptian', 1, 2, 'Midfielder', NULL, NULL, 1);

-- Golden Boot Players
IF NOT EXISTS (SELECT 1 FROM Players WHERE Id = 6201)
INSERT INTO Players (Id, DateOfBirth, Nationality, PreferredFoot, WeakFootRating, PlayStyleTag, ArchetypePlayerName, ArchetypeText, AvailabilityStatus)
VALUES
(6201, '2008-02-28', 'Egyptian', 1, 3, 'Goalkeeper', NULL, NULL, 1),
(6202, '2008-05-14', 'Egyptian', 1, 2, 'Defender', NULL, NULL, 1),
(6203, '2008-08-09', 'Egyptian', 2, 3, 'Fullback', NULL, NULL, 1),
(6204, '2008-11-20', 'Egyptian', 1, 3, 'Center Back', NULL, NULL, 1),
(6205, '2008-01-05', 'Egyptian', 1, 2, 'Midfielder', NULL, NULL, 1),
(6206, '2008-04-17', 'Egyptian', 2, 3, 'Winger', NULL, NULL, 1),
(6207, '2008-07-03', 'Egyptian', 1, 3, 'Playmaker', NULL, NULL, 1),
(6208, '2008-09-25', 'Egyptian', 1, 2, 'Striker', NULL, NULL, 1),
(6209, '2008-03-08', 'Egyptian', 2, 3, 'Creator', NULL, NULL, 1),
(6210, '2008-06-30', 'Egyptian', 1, 3, 'Finisher', NULL, NULL, 1),
(6211, '2008-10-15', 'Egyptian', 1, 3, 'Midfielder', NULL, NULL, 1);

-- Nile Stars Players
IF NOT EXISTS (SELECT 1 FROM Players WHERE Id = 6301)
INSERT INTO Players (Id, DateOfBirth, Nationality, PreferredFoot, WeakFootRating, PlayStyleTag, ArchetypePlayerName, ArchetypeText, AvailabilityStatus)
VALUES
(6301, '2008-01-20', 'Egyptian', 1, 3, 'Goalkeeper', NULL, NULL, 1),
(6302, '2008-04-11', 'Egyptian', 1, 2, 'Defender', NULL, NULL, 1),
(6303, '2008-07-22', 'Egyptian', 2, 3, 'Fullback', NULL, NULL, 1),
(6304, '2008-10-03', 'Egyptian', 1, 3, 'Center Back', NULL, NULL, 1),
(6305, '2008-02-15', 'Egyptian', 1, 2, 'Midfielder', NULL, NULL, 1),
(6306, '2008-05-28', 'Egyptian', 2, 3, 'Winger', NULL, NULL, 1),
(6307, '2008-08-09', 'Egyptian', 1, 3, 'Playmaker', NULL, NULL, 1),
(6308, '2008-11-18', 'Egyptian', 1, 2, 'Striker', NULL, NULL, 1),
(6309, '2008-03-05', 'Egyptian', 2, 3, 'Creator', NULL, NULL, 1),
(6310, '2008-06-14', 'Egyptian', 1, 3, 'Finisher', NULL, NULL, 1),
(6311, '2008-09-30', 'Egyptian', 1, 3, 'Midfielder', NULL, NULL, 1);

-- Desert Hawks Players
IF NOT EXISTS (SELECT 1 FROM Players WHERE Id = 6401)
INSERT INTO Players (Id, DateOfBirth, Nationality, PreferredFoot, WeakFootRating, PlayStyleTag, ArchetypePlayerName, ArchetypeText, AvailabilityStatus)
VALUES
(6401, '2008-03-10', 'Egyptian', 1, 3, 'Goalkeeper', NULL, NULL, 1),
(6402, '2008-06-25', 'Egyptian', 1, 2, 'Defender', NULL, NULL, 1),
(6403, '2008-09-14', 'Egyptian', 2, 3, 'Fullback', NULL, NULL, 1),
(6404, '2008-12-01', 'Egyptian', 1, 3, 'Center Back', NULL, NULL, 1),
(6405, '2008-02-08', 'Egyptian', 1, 2, 'Midfielder', NULL, NULL, 1),
(6406, '2008-05-19', 'Egyptian', 2, 3, 'Winger', NULL, NULL, 1),
(6407, '2008-08-30', 'Egyptian', 1, 3, 'Playmaker', NULL, NULL, 1),
(6408, '2008-11-11', 'Egyptian', 1, 2, 'Striker', NULL, NULL, 1),
(6409, '2008-01-28', 'Egyptian', 2, 3, 'Creator', NULL, NULL, 1),
(6410, '2008-04-16', 'Egyptian', 1, 3, 'Finisher', NULL, NULL, 1),
(6411, '2008-07-07', 'Egyptian', 1, 3, 'Midfielder', NULL, NULL, 1);

-- Medina United Players
IF NOT EXISTS (SELECT 1 FROM Players WHERE Id = 6501)
INSERT INTO Players (Id, DateOfBirth, Nationality, PreferredFoot, WeakFootRating, PlayStyleTag, ArchetypePlayerName, ArchetypeText, AvailabilityStatus)
VALUES
(6501, '2008-02-05', 'Egyptian', 1, 3, 'Goalkeeper', NULL, NULL, 1),
(6502, '2008-05-18', 'Egyptian', 1, 2, 'Defender', NULL, NULL, 1),
(6503, '2008-08-21', 'Egyptian', 2, 3, 'Fullback', NULL, NULL, 1),
(6504, '2008-11-09', 'Egyptian', 1, 3, 'Center Back', NULL, NULL, 1),
(6505, '2008-01-15', 'Egyptian', 1, 2, 'Midfielder', NULL, NULL, 1),
(6506, '2008-04-28', 'Egyptian', 2, 3, 'Winger', NULL, NULL, 1),
(6507, '2008-07-10', 'Egyptian', 1, 3, 'Playmaker', NULL, NULL, 1),
(6508, '2008-10-22', 'Egyptian', 1, 2, 'Striker', NULL, NULL, 1),
(6509, '2008-03-14', 'Egyptian', 2, 3, 'Creator', NULL, NULL, 1),
(6510, '2008-06-06', 'Egyptian', 1, 3, 'Finisher', NULL, NULL, 1),
(6511, '2008-09-19', 'Egyptian', 1, 3, 'Midfielder', NULL, NULL, 1);

-- Delta Force Players
IF NOT EXISTS (SELECT 1 FROM Players WHERE Id = 6601)
INSERT INTO Players (Id, DateOfBirth, Nationality, PreferredFoot, WeakFootRating, PlayStyleTag, ArchetypePlayerName, ArchetypeText, AvailabilityStatus)
VALUES
(6601, '2008-01-30', 'Egyptian', 1, 3, 'Goalkeeper', NULL, NULL, 1),
(6602, '2008-04-15', 'Egyptian', 1, 2, 'Defender', NULL, NULL, 1),
(6603, '2008-07-08', 'Egyptian', 2, 3, 'Fullback', NULL, NULL, 1),
(6604, '2008-10-20', 'Egyptian', 1, 3, 'Center Back', NULL, NULL, 1),
(6605, '2008-02-12', 'Egyptian', 1, 2, 'Midfielder', NULL, NULL, 1),
(6606, '2008-05-25', 'Egyptian', 2, 3, 'Winger', NULL, NULL, 1),
(6607, '2008-08-05', 'Egyptian', 1, 3, 'Playmaker', NULL, NULL, 1),
(6608, '2008-11-15', 'Egyptian', 1, 2, 'Striker', NULL, NULL, 1),
(6609, '2008-03-22', 'Egyptian', 2, 3, 'Creator', NULL, NULL, 1),
(6610, '2008-06-10', 'Egyptian', 1, 3, 'Finisher', NULL, NULL, 1),
(6611, '2008-09-28', 'Egyptian', 1, 3, 'Midfielder', NULL, NULL, 1);

-- Pharaohs Soccer Players
IF NOT EXISTS (SELECT 1 FROM Players WHERE Id = 6701)
INSERT INTO Players (Id, DateOfBirth, Nationality, PreferredFoot, WeakFootRating, PlayStyleTag, ArchetypePlayerName, ArchetypeText, AvailabilityStatus)
VALUES
(6701, '2008-02-18', 'Egyptian', 1, 3, 'Goalkeeper', NULL, NULL, 1),
(6702, '2008-05-30', 'Egyptian', 1, 2, 'Defender', NULL, NULL, 1),
(6703, '2008-08-12', 'Egyptian', 2, 3, 'Fullback', NULL, NULL, 1),
(6704, '2008-11-25', 'Egyptian', 1, 3, 'Center Back', NULL, NULL, 1),
(6705, '2008-01-08', 'Egyptian', 1, 2, 'Midfielder', NULL, NULL, 1),
(6706, '2008-04-20', 'Egyptian', 2, 3, 'Winger', NULL, NULL, 1),
(6707, '2008-07-02', 'Egyptian', 1, 3, 'Playmaker', NULL, NULL, 1),
(6708, '2008-09-14', 'Egyptian', 1, 2, 'Striker', NULL, NULL, 1),
(6709, '2008-12-05', 'Egyptian', 2, 3, 'Creator', NULL, NULL, 1),
(6710, '2008-03-28', 'Egyptian', 1, 3, 'Finisher', NULL, NULL, 1),
(6711, '2008-06-16', 'Egyptian', 1, 3, 'Midfielder', NULL, NULL, 1);

-- Player Teams (assign all players to their respective teams)
SET IDENTITY_INSERT PlayerTeams ON;

-- Elite Strikers (team 5001)
IF NOT EXISTS (SELECT 1 FROM PlayerTeams WHERE Id = 7001)
INSERT INTO PlayerTeams (Id, PlayerId, TeamId, JoinedAt, LeftAt, IsDeleted, CreatedAt, CreatedById)
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

-- Future Legends (team 5002)
IF NOT EXISTS (SELECT 1 FROM PlayerTeams WHERE Id = 7012)
INSERT INTO PlayerTeams (Id, PlayerId, TeamId, JoinedAt, LeftAt, IsDeleted, CreatedAt, CreatedById)
VALUES
(7012, 6101, 5002, @Now, NULL, 0, @Now, NULL),
(7013, 6102, 5002, @Now, NULL, 0, @Now, NULL),
(7014, 6103, 5002, @Now, NULL, 0, @Now, NULL),
(7015, 6104, 5002, @Now, NULL, 0, @Now, NULL),
(7016, 6105, 5002, @Now, NULL, 0, @Now, NULL),
(7017, 6106, 5002, @Now, NULL, 0, @Now, NULL),
(7018, 6107, 5002, @Now, NULL, 0, @Now, NULL),
(7019, 6108, 5002, @Now, NULL, 0, @Now, NULL),
(7020, 6109, 5002, @Now, NULL, 0, @Now, NULL),
(7021, 6110, 5002, @Now, NULL, 0, @Now, NULL),
(7022, 6111, 5002, @Now, NULL, 0, @Now, NULL);

-- Golden Boot (team 5003)
IF NOT EXISTS (SELECT 1 FROM PlayerTeams WHERE Id = 7023)
INSERT INTO PlayerTeams (Id, PlayerId, TeamId, JoinedAt, LeftAt, IsDeleted, CreatedAt, CreatedById)
VALUES
(7023, 6201, 5003, @Now, NULL, 0, @Now, NULL),
(7024, 6202, 5003, @Now, NULL, 0, @Now, NULL),
(7025, 6203, 5003, @Now, NULL, 0, @Now, NULL),
(7026, 6204, 5003, @Now, NULL, 0, @Now, NULL),
(7027, 6205, 5003, @Now, NULL, 0, @Now, NULL),
(7028, 6206, 5003, @Now, NULL, 0, @Now, NULL),
(7029, 6207, 5003, @Now, NULL, 0, @Now, NULL),
(7030, 6208, 5003, @Now, NULL, 0, @Now, NULL),
(7031, 6209, 5003, @Now, NULL, 0, @Now, NULL),
(7032, 6210, 5003, @Now, NULL, 0, @Now, NULL),
(7033, 6211, 5003, @Now, NULL, 0, @Now, NULL);

-- Nile Stars (team 5004)
IF NOT EXISTS (SELECT 1 FROM PlayerTeams WHERE Id = 7034)
INSERT INTO PlayerTeams (Id, PlayerId, TeamId, JoinedAt, LeftAt, IsDeleted, CreatedAt, CreatedById)
VALUES
(7034, 6301, 5004, @Now, NULL, 0, @Now, NULL),
(7035, 6302, 5004, @Now, NULL, 0, @Now, NULL),
(7036, 6303, 5004, @Now, NULL, 0, @Now, NULL),
(7037, 6304, 5004, @Now, NULL, 0, @Now, NULL),
(7038, 6305, 5004, @Now, NULL, 0, @Now, NULL),
(7039, 6306, 5004, @Now, NULL, 0, @Now, NULL),
(7040, 6307, 5004, @Now, NULL, 0, @Now, NULL),
(7041, 6308, 5004, @Now, NULL, 0, @Now, NULL),
(7042, 6309, 5004, @Now, NULL, 0, @Now, NULL),
(7043, 6310, 5004, @Now, NULL, 0, @Now, NULL),
(7044, 6311, 5004, @Now, NULL, 0, @Now, NULL);

-- Desert Hawks (team 5005)
IF NOT EXISTS (SELECT 1 FROM PlayerTeams WHERE Id = 7045)
INSERT INTO PlayerTeams (Id, PlayerId, TeamId, JoinedAt, LeftAt, IsDeleted, CreatedAt, CreatedById)
VALUES
(7045, 6401, 5005, @Now, NULL, 0, @Now, NULL),
(7046, 6402, 5005, @Now, NULL, 0, @Now, NULL),
(7047, 6403, 5005, @Now, NULL, 0, @Now, NULL),
(7048, 6404, 5005, @Now, NULL, 0, @Now, NULL),
(7049, 6405, 5005, @Now, NULL, 0, @Now, NULL),
(7050, 6406, 5005, @Now, NULL, 0, @Now, NULL),
(7051, 6407, 5005, @Now, NULL, 0, @Now, NULL),
(7052, 6408, 5005, @Now, NULL, 0, @Now, NULL),
(7053, 6409, 5005, @Now, NULL, 0, @Now, NULL),
(7054, 6410, 5005, @Now, NULL, 0, @Now, NULL),
(7055, 6411, 5005, @Now, NULL, 0, @Now, NULL);

-- Medina United (team 5006)
IF NOT EXISTS (SELECT 1 FROM PlayerTeams WHERE Id = 7056)
INSERT INTO PlayerTeams (Id, PlayerId, TeamId, JoinedAt, LeftAt, IsDeleted, CreatedAt, CreatedById)
VALUES
(7056, 6501, 5006, @Now, NULL, 0, @Now, NULL),
(7057, 6502, 5006, @Now, NULL, 0, @Now, NULL),
(7058, 6503, 5006, @Now, NULL, 0, @Now, NULL),
(7059, 6504, 5006, @Now, NULL, 0, @Now, NULL),
(7060, 6505, 5006, @Now, NULL, 0, @Now, NULL),
(7061, 6506, 5006, @Now, NULL, 0, @Now, NULL),
(7062, 6507, 5006, @Now, NULL, 0, @Now, NULL),
(7063, 6508, 5006, @Now, NULL, 0, @Now, NULL),
(7064, 6509, 5006, @Now, NULL, 0, @Now, NULL),
(7065, 6510, 5006, @Now, NULL, 0, @Now, NULL),
(7066, 6511, 5006, @Now, NULL, 0, @Now, NULL);

-- Delta Force (team 5007)
IF NOT EXISTS (SELECT 1 FROM PlayerTeams WHERE Id = 7067)
INSERT INTO PlayerTeams (Id, PlayerId, TeamId, JoinedAt, LeftAt, IsDeleted, CreatedAt, CreatedById)
VALUES
(7067, 6601, 5007, @Now, NULL, 0, @Now, NULL),
(7068, 6602, 5007, @Now, NULL, 0, @Now, NULL),
(7069, 6603, 5007, @Now, NULL, 0, @Now, NULL),
(7070, 6604, 5007, @Now, NULL, 0, @Now, NULL),
(7071, 6605, 5007, @Now, NULL, 0, @Now, NULL),
(7072, 6606, 5007, @Now, NULL, 0, @Now, NULL),
(7073, 6607, 5007, @Now, NULL, 0, @Now, NULL),
(7074, 6608, 5007, @Now, NULL, 0, @Now, NULL),
(7075, 6609, 5007, @Now, NULL, 0, @Now, NULL),
(7076, 6610, 5007, @Now, NULL, 0, @Now, NULL),
(7077, 6611, 5007, @Now, NULL, 0, @Now, NULL);

-- Pharaohs Soccer (team 5008)
IF NOT EXISTS (SELECT 1 FROM PlayerTeams WHERE Id = 7078)
INSERT INTO PlayerTeams (Id, PlayerId, TeamId, JoinedAt, LeftAt, IsDeleted, CreatedAt, CreatedById)
VALUES
(7078, 6701, 5008, @Now, NULL, 0, @Now, NULL),
(7079, 6702, 5008, @Now, NULL, 0, @Now, NULL),
(7080, 6703, 5008, @Now, NULL, 0, @Now, NULL),
(7081, 6704, 5008, @Now, NULL, 0, @Now, NULL),
(7082, 6705, 5008, @Now, NULL, 0, @Now, NULL),
(7083, 6706, 5008, @Now, NULL, 0, @Now, NULL),
(7084, 6707, 5008, @Now, NULL, 0, @Now, NULL),
(7085, 6708, 5008, @Now, NULL, 0, @Now, NULL),
(7086, 6709, 5008, @Now, NULL, 0, @Now, NULL),
(7087, 6710, 5008, @Now, NULL, 0, @Now, NULL),
(7088, 6711, 5008, @Now, NULL, 0, @Now, NULL);

SET IDENTITY_INSERT PlayerTeams OFF;

-- Player Positions
IF OBJECT_ID('PlayerPositions') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT PlayerPositions ON;

    IF NOT EXISTS (SELECT 1 FROM PlayerPositions WHERE Id = 7101)
    INSERT INTO PlayerPositions (Id, PlayerId, Position, IsPrimary, IsDeleted, CreatedAt, CreatedById)
    VALUES
    -- Elite Strikers
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
    (7111, 6011, 'CM', 1, 0, @Now, NULL),
    -- Future Legends
    (7112, 6101, 'GK', 1, 0, @Now, NULL),
    (7113, 6102, 'CB', 1, 0, @Now, NULL),
    (7114, 6103, 'LB', 1, 0, @Now, NULL),
    (7115, 6104, 'CM', 1, 0, @Now, NULL),
    (7116, 6105, 'CB', 1, 0, @Now, NULL),
    (7117, 6106, 'RW', 1, 0, @Now, NULL),
    (7118, 6107, 'AM', 1, 0, @Now, NULL),
    (7119, 6108, 'ST', 1, 0, @Now, NULL),
    (7120, 6109, 'CM', 1, 0, @Now, NULL),
    (7121, 6110, 'ST', 1, 0, @Now, NULL),
    (7122, 6111, 'DM', 1, 0, @Now, NULL),
    -- Golden Boot
    (7123, 6201, 'GK', 1, 0, @Now, NULL),
    (7124, 6202, 'CB', 1, 0, @Now, NULL),
    (7125, 6203, 'RB', 1, 0, @Now, NULL),
    (7126, 6204, 'CB', 1, 0, @Now, NULL),
    (7127, 6205, 'DM', 1, 0, @Now, NULL),
    (7128, 6206, 'LW', 1, 0, @Now, NULL),
    (7129, 6207, 'AM', 1, 0, @Now, NULL),
    (7130, 6208, 'ST', 1, 0, @Now, NULL),
    (7131, 6209, 'CM', 1, 0, @Now, NULL),
    (7132, 6210, 'ST', 1, 0, @Now, NULL),
    (7133, 6211, 'CM', 1, 0, @Now, NULL),
    -- Nile Stars
    (7134, 6301, 'GK', 1, 0, @Now, NULL),
    (7135, 6302, 'CB', 1, 0, @Now, NULL),
    (7136, 6303, 'LB', 1, 0, @Now, NULL),
    (7137, 6304, 'CB', 1, 0, @Now, NULL),
    (7138, 6305, 'CM', 1, 0, @Now, NULL),
    (7139, 6306, 'RW', 1, 0, @Now, NULL),
    (7140, 6307, 'AM', 1, 0, @Now, NULL),
    (7141, 6308, 'ST', 1, 0, @Now, NULL),
    (7142, 6309, 'CM', 1, 0, @Now, NULL),
    (7143, 6310, 'ST', 1, 0, @Now, NULL),
    (7144, 6311, 'DM', 1, 0, @Now, NULL),
    -- Desert Hawks
    (7145, 6401, 'GK', 1, 0, @Now, NULL),
    (7146, 6402, 'CB', 1, 0, @Now, NULL),
    (7147, 6403, 'RB', 1, 0, @Now, NULL),
    (7148, 6404, 'CB', 1, 0, @Now, NULL),
    (7149, 6405, 'CM', 1, 0, @Now, NULL),
    (7150, 6406, 'LW', 1, 0, @Now, NULL),
    (7151, 6407, 'AM', 1, 0, @Now, NULL),
    (7152, 6408, 'ST', 1, 0, @Now, NULL),
    (7153, 6409, 'CM', 1, 0, @Now, NULL),
    (7154, 6410, 'ST', 1, 0, @Now, NULL),
    (7155, 6411, 'DM', 1, 0, @Now, NULL),
    -- Medina United
    (7156, 6501, 'GK', 1, 0, @Now, NULL),
    (7157, 6502, 'CB', 1, 0, @Now, NULL),
    (7158, 6503, 'LB', 1, 0, @Now, NULL),
    (7159, 6504, 'CB', 1, 0, @Now, NULL),
    (7160, 6505, 'DM', 1, 0, @Now, NULL),
    (7161, 6506, 'RW', 1, 0, @Now, NULL),
    (7162, 6507, 'AM', 1, 0, @Now, NULL),
    (7163, 6508, 'ST', 1, 0, @Now, NULL),
    (7164, 6509, 'CM', 1, 0, @Now, NULL),
    (7165, 6510, 'ST', 1, 0, @Now, NULL),
    (7166, 6511, 'CM', 1, 0, @Now, NULL),
    -- Delta Force
    (7167, 6601, 'GK', 1, 0, @Now, NULL),
    (7168, 6602, 'CB', 1, 0, @Now, NULL),
    (7169, 6603, 'RB', 1, 0, @Now, NULL),
    (7170, 6604, 'CB', 1, 0, @Now, NULL),
    (7171, 6605, 'CM', 1, 0, @Now, NULL),
    (7172, 6606, 'LW', 1, 0, @Now, NULL),
    (7173, 6607, 'AM', 1, 0, @Now, NULL),
    (7174, 6608, 'ST', 1, 0, @Now, NULL),
    (7175, 6609, 'CM', 1, 0, @Now, NULL),
    (7176, 6610, 'ST', 1, 0, @Now, NULL),
    (7177, 6611, 'DM', 1, 0, @Now, NULL),
    -- Pharaohs Soccer
    (7178, 6701, 'GK', 1, 0, @Now, NULL),
    (7179, 6702, 'CB', 1, 0, @Now, NULL),
    (7180, 6703, 'LB', 1, 0, @Now, NULL),
    (7181, 6704, 'CB', 1, 0, @Now, NULL),
    (7182, 6705, 'CM', 1, 0, @Now, NULL),
    (7183, 6706, 'RW', 1, 0, @Now, NULL),
    (7184, 6707, 'AM', 1, 0, @Now, NULL),
    (7185, 6708, 'ST', 1, 0, @Now, NULL),
    (7186, 6709, 'CM', 1, 0, @Now, NULL),
    (7187, 6710, 'ST', 1, 0, @Now, NULL),
    (7188, 6711, 'DM', 1, 0, @Now, NULL);

    SET IDENTITY_INSERT PlayerPositions OFF;
END

PRINT '=== Section 0 Complete: All base data seeded ===';

/* =============================================================
   SECTION 1 — TOURNAMENT 8003: 8-TEAM KNOCKOUT (Draft)
   Purpose: Test creating a knockout tournament,
   inviting teams, registering squads, seeding, draw,
   advancing rounds, and completing
   ============================================================= */

PRINT '=== Section 1: Tournament 8003 - 8-Team Knockout ===';

SET IDENTITY_INSERT Tournaments ON;

IF NOT EXISTS (SELECT 1 FROM Tournaments WHERE Id = 8003)
INSERT INTO Tournaments (Id, Name, Format, Structure, AgeGroupId, HasTwoLegs, StartDate, EndDate, Status, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
(8003, 'Koralytics Knockout Challenge', 11, 1, 3001, 0, '2026-08-01', '2026-08-10', 1, 0, @Now, @Now, NULL, NULL);

SET IDENTITY_INSERT Tournaments OFF;

-- Tournament Teams (8 teams, invited)
SET IDENTITY_INSERT TournamentTeams ON;

IF NOT EXISTS (SELECT 1 FROM TournamentTeams WHERE Id = 8301)
INSERT INTO TournamentTeams (Id, TournamentId, TeamId, SeedNumber, Status, RegisteredAt, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
-- Elite Strikers, Future Legends, Golden Boot, Nile Stars, Desert Hawks, Medina United, Delta Force, Pharaohs
(8301, 8003, 5001, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL),
(8302, 8003, 5002, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL),
(8303, 8003, 5003, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL),
(8304, 8003, 5004, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL),
(8305, 8003, 5005, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL),
(8306, 8003, 5006, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL),
(8307, 8003, 5007, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL),
(8308, 8003, 5008, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL);

SET IDENTITY_INSERT TournamentTeams OFF;

-- Squads for Elite Strikers (team 5001) -> tournament 8003
SET IDENTITY_INSERT TournamentSquads ON;

IF NOT EXISTS (SELECT 1 FROM TournamentSquads WHERE Id = 8801)
INSERT INTO TournamentSquads (Id, TournamentId, TeamId, PlayerId, RegisteredAt, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
(8801, 8003, 5001, 6001, @Now, 0, @Now, @Now, NULL, NULL),
(8802, 8003, 5001, 6002, @Now, 0, @Now, @Now, NULL, NULL),
(8803, 8003, 5001, 6003, @Now, 0, @Now, @Now, NULL, NULL),
(8804, 8003, 5001, 6004, @Now, 0, @Now, @Now, NULL, NULL),
(8805, 8003, 5001, 6005, @Now, 0, @Now, @Now, NULL, NULL),
(8806, 8003, 5001, 6006, @Now, 0, @Now, @Now, NULL, NULL),
(8807, 8003, 5001, 6007, @Now, 0, @Now, @Now, NULL, NULL),
(8808, 8003, 5001, 6008, @Now, 0, @Now, @Now, NULL, NULL),
(8809, 8003, 5001, 6009, @Now, 0, @Now, @Now, NULL, NULL),
(8810, 8003, 5001, 6010, @Now, 0, @Now, @Now, NULL, NULL),
(8811, 8003, 5001, 6011, @Now, 0, @Now, @Now, NULL, NULL);

SET IDENTITY_INSERT TournamentSquads OFF;

PRINT '=== Section 1 Complete: Tournament 8003 created in Draft status ===';
PRINT '  → Run: POST /api/Tournament/8003/invite/{academyId} to invite academies 2001-2008';
PRINT '  → Run: PUT /api/Tournament/8003/accept/{academyId} to accept invitations';
PRINT '  → Run: POST /api/Tournament/8003/squad/5001 with [6001..6011] for Elite Strikers';
PRINT '  → Run: POST /api/Tournament/8003/seeding to generate seeding';
PRINT '  → Run: POST /api/Tournament/8003/draw to generate knockout draw';
PRINT '  → Run: GET /api/Tournament/8003/bracket to see bracket';

/* =============================================================
   SECTION 2 — TOURNAMENT 8004: 8-TEAM GROUP + KNOCKOUT (Draft)
   Purpose: Test creating a group+knockout tournament,
   full flow from creation to completion
   ============================================================= */

PRINT '=== Section 2: Tournament 8004 - 8-Team Group & Knockout ===';

SET IDENTITY_INSERT Tournaments ON;

IF NOT EXISTS (SELECT 1 FROM Tournaments WHERE Id = 8004)
INSERT INTO Tournaments (Id, Name, Format, Structure, AgeGroupId, HasTwoLegs, StartDate, EndDate, Status, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
(8004, 'Koralytics Group Cup', 11, 2, 3001, 0, '2026-08-15', '2026-08-30', 1, 0, @Now, @Now, NULL, NULL);

SET IDENTITY_INSERT Tournaments OFF;

-- Tournament Teams (8 teams)
SET IDENTITY_INSERT TournamentTeams ON;

IF NOT EXISTS (SELECT 1 FROM TournamentTeams WHERE Id = 8311)
INSERT INTO TournamentTeams (Id, TournamentId, TeamId, SeedNumber, Status, RegisteredAt, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
(8311, 8004, 5001, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL),
(8312, 8004, 5002, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL),
(8313, 8004, 5003, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL),
(8314, 8004, 5004, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL),
(8315, 8004, 5005, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL),
(8316, 8004, 5006, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL),
(8317, 8004, 5007, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL),
(8318, 8004, 5008, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL);

SET IDENTITY_INSERT TournamentTeams OFF;

PRINT '=== Section 2 Complete: Tournament 8004 created in Draft status ===';
PRINT '  → Run: POST /api/Tournament/8004/invite/{academyId} to invite academies 2001-2008';
PRINT '  → Run: PUT /api/Tournament/8004/accept/{academyId} to accept';
PRINT '  → Run: POST /api/Tournament/8004/seeding';
PRINT '  → Run: POST /api/Tournament/8004/draw (creates 2 groups + round-robin fixtures)';
PRINT '  → Run: GET /api/Tournament/8004/bracket to see groups';

/* =============================================================
   SECTION 3 — TOURNAMENT 8005: 4-TEAM LEAGUE (Draft)
   Purpose: Test league format (round-robin, no knockout)
   ============================================================= */

PRINT '=== Section 3: Tournament 8005 - 4-Team League ===';

SET IDENTITY_INSERT Tournaments ON;

IF NOT EXISTS (SELECT 1 FROM Tournaments WHERE Id = 8005)
INSERT INTO Tournaments (Id, Name, Format, Structure, AgeGroupId, HasTwoLegs, StartDate, EndDate, Status, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
(8005, 'Koralytics League Championship', 11, 3, 3001, 0, '2026-09-01', '2026-09-20', 1, 0, @Now, @Now, NULL, NULL);

SET IDENTITY_INSERT Tournaments OFF;

-- Tournament Teams (4 teams)
SET IDENTITY_INSERT TournamentTeams ON;

IF NOT EXISTS (SELECT 1 FROM TournamentTeams WHERE Id = 8321)
INSERT INTO TournamentTeams (Id, TournamentId, TeamId, SeedNumber, Status, RegisteredAt, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
(8321, 8005, 5001, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL),
(8322, 8005, 5002, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL),
(8323, 8005, 5003, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL),
(8324, 8005, 5004, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL);

SET IDENTITY_INSERT TournamentTeams OFF;

PRINT '=== Section 3 Complete: Tournament 8005 created in Draft with League dummy group ===';
PRINT '  → Note: League format creates a dummy group automatically on creation';
PRINT '  → Run: POST /api/Tournament/8005/invite/{academyId} for academies 2001-2004';
PRINT '  → Run: PUT /api/Tournament/8005/accept/{academyId}';
PRINT '  → Run: POST /api/Tournament/8005/seeding';
PRINT '  → Run: POST /api/Tournament/8005/draw (generates round-robin)';
PRINT '  → Run: GET /api/Tournament/8005/bracket';

/* =============================================================
   SECTION 4 — TOURNAMENT 8006: TWO-LEG KNOCKOUT (Draft)
   Purpose: Test HasTwoLegs=true, both legs generation,
   advancing with aggregate scores
   ============================================================= */

PRINT '=== Section 4: Tournament 8006 - 4-Team Two-Leg Knockout ===';

SET IDENTITY_INSERT Tournaments ON;

IF NOT EXISTS (SELECT 1 FROM Tournaments WHERE Id = 8006)
INSERT INTO Tournaments (Id, Name, Format, Structure, AgeGroupId, HasTwoLegs, StartDate, EndDate, Status, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
(8006, 'Koralytics Two-Leg Cup', 11, 1, 3001, 1, '2026-09-10', '2026-09-25', 1, 0, @Now, @Now, NULL, NULL);

SET IDENTITY_INSERT Tournaments OFF;

-- Tournament Teams (4 teams)
SET IDENTITY_INSERT TournamentTeams ON;

IF NOT EXISTS (SELECT 1 FROM TournamentTeams WHERE Id = 8331)
INSERT INTO TournamentTeams (Id, TournamentId, TeamId, SeedNumber, Status, RegisteredAt, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
(8331, 8006, 5001, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL),
(8332, 8006, 5003, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL),
(8333, 8006, 5005, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL),
(8334, 8006, 5007, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL);

SET IDENTITY_INSERT TournamentTeams OFF;

PRINT '=== Section 4 Complete: Tournament 8006 created in Draft status ===';
PRINT '  → HasTwoLegs=true — draw will create 2 fixtures per pairing (leg 1 & 2)';
PRINT '  → Run: invite -> accept -> seeding -> draw';
PRINT '  → After completing fixtures, run advance knockout';

/* =============================================================
   SECTION 5 — TOURNAMENT 8001: COMPLETED (enhanced data)
   The existing Tournament 8001 is already set up as a
   completed tournament with groups, standings, fixtures,
   squads, and Hall of Fame. Add more data if needed.
   This is the main tournament for GET endpoints testing.
   ============================================================= */

PRINT '=== Section 5: Tournament 8001 - Already completed (from existing seed) ===';
PRINT '  → Run: GET /api/Tournament';
PRINT '  → Run: GET /api/Tournament/8001';
PRINT '  → Run: GET /api/Tournament/8001/bracket';
PRINT '  → Run: GET /api/Tournament/8001/teams';
PRINT '  → Run: GET /api/Tournament/8001/hall-of-fame';

/* =============================================================
   SECTION 6 — TOURNAMENT 8002: REGISTRATION (ready for testing)
   The existing Tournament 8002 is in Registration status
   with 1 team invited (Elite Strikers - team 5001).
   Perfect for testing draw/seeding.
   ============================================================= */

PRINT '=== Section 6: Tournament 8002 - Registration (from existing seed) ===';
PRINT '  → Already has 1 team (Elite Strikers) invited';
PRINT '  → Run: POST /api/Tournament/8002/invite/2002 (add Future Legends)';
PRINT '  → Run: POST /api/Tournament/8002/invite/2003 (add Golden Boot)';
PRINT '  → Run: POST /api/Tournament/8002/invite/2004 (add Nile Stars)';
PRINT '  → Run: PUT /api/Tournament/8002/accept/2001 (accept Elite Strikers)';
PRINT '  → Run: ... accept other academies ...';
PRINT '  → Run: POST /api/Tournament/8002/squad/5001 with [6001..6011]';
PRINT '  → Run: POST /api/Tournament/8002/seeding';
PRINT '  → Run: POST /api/Tournament/8002/draw';
PRINT '  → Run: GET /api/Tournament/8002/bracket';

/* =============================================================
   SECTION 7 — TOURNAMENT 8007: 7-SIDE KNOCKOUT (Draft)
   Purpose: Test 7-side format (different format enum)
   ============================================================= */

PRINT '=== Section 7: Tournament 8007 - 7-Side Knockout ===';

SET IDENTITY_INSERT Tournaments ON;

IF NOT EXISTS (SELECT 1 FROM Tournaments WHERE Id = 8007)
INSERT INTO Tournaments (Id, Name, Format, Structure, AgeGroupId, HasTwoLegs, StartDate, EndDate, Status, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
(8007, 'Koralytics 7-Side Showdown', 7, 1, 3001, 0, '2026-10-01', '2026-10-05', 1, 0, @Now, @Now, NULL, NULL);

SET IDENTITY_INSERT Tournaments OFF;

SET IDENTITY_INSERT TournamentTeams ON;

IF NOT EXISTS (SELECT 1 FROM TournamentTeams WHERE Id = 8341)
INSERT INTO TournamentTeams (Id, TournamentId, TeamId, SeedNumber, Status, RegisteredAt, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
(8341, 8007, 5001, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL),
(8342, 8007, 5002, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL),
(8343, 8007, 5003, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL),
(8344, 8007, 5004, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL);

SET IDENTITY_INSERT TournamentTeams OFF;

PRINT '=== Section 7 Complete: Tournament 8007 (7-side) created in Draft ===';
PRINT '  → Format=7, Structure=1 (Knockout), 4 teams';
PRINT '  → Run: invite -> accept -> squad (min 7 players) -> seeding -> draw';

/* =============================================================
   SECTION 8 — TOURNAMENT 8008: GROUP+KO WITH TWO LEGS (Draft)
   Purpose: Test Group+Knockout with two-leg fixtures
   ============================================================= */

PRINT '=== Section 8: Tournament 8008 - Group+KO with Two Legs ===';

SET IDENTITY_INSERT Tournaments ON;

IF NOT EXISTS (SELECT 1 FROM Tournaments WHERE Id = 8008)
INSERT INTO Tournaments (Id, Name, Format, Structure, AgeGroupId, HasTwoLegs, StartDate, EndDate, Status, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
(8008, 'Koralytics Two-Leg Group Cup', 11, 2, 3001, 1, '2026-10-10', '2026-10-30', 1, 0, @Now, @Now, NULL, NULL);

SET IDENTITY_INSERT Tournaments OFF;

SET IDENTITY_INSERT TournamentTeams ON;

IF NOT EXISTS (SELECT 1 FROM TournamentTeams WHERE Id = 8351)
INSERT INTO TournamentTeams (Id, TournamentId, TeamId, SeedNumber, Status, RegisteredAt, IsDeleted, CreatedAt, UpdatedAt, CreatedById, UpdatedById)
VALUES
(8351, 8008, 5001, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL),
(8352, 8008, 5002, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL),
(8353, 8008, 5003, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL),
(8354, 8008, 5004, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL),
(8355, 8008, 5005, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL),
(8356, 8008, 5006, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL),
(8357, 8008, 5007, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL),
(8358, 8008, 5008, NULL, 1, @Now, 0, @Now, @Now, NULL, NULL);

SET IDENTITY_INSERT TournamentTeams OFF;

PRINT '=== Section 8 Complete: Tournament 8008 (Group+KO + Two Legs) in Draft ===';
PRINT '  → HasTwoLegs=true — group stage fixtures have 2 legs';
PRINT '  → Run: invite -> accept -> seeding -> draw -> bracket';

/* =============================================================
   API ENDPOINT SUMMARY
   ============================================================= */

PRINT '╔══════════════════════════════════════════════════════════════╗';
PRINT '║                  API ENDPOINT SUMMARY                       ║';
PRINT '╠══════════════════════════════════════════════════════════════╣';
PRINT '║                                                              ║';
PRINT '║  TOURNAMENT CRUD                                            ║';
PRINT '║  GET    /api/Tournament                                      ║';
PRINT '║  GET    /api/Tournament/{id}                                 ║';
PRINT '║  POST   /api/Tournament                                     ║';
PRINT '║  PUT    /api/Tournament/{id}/status                         ║';
PRINT '║                                                              ║';
PRINT '║  INVITATIONS                                                ║';
PRINT '║  POST   /api/Tournament/{id}/invite/{academyId}             ║';
PRINT '║  PUT    /api/Tournament/{id}/accept/{academyId}             ║';
PRINT '║  GET    /api/Tournament/{id}/teams                          ║';
PRINT '║                                                              ║';
PRINT '║  SQUAD REGISTRATION                                         ║';
PRINT '║  POST   /api/Tournament/{id}/squad/{teamId}                ║';
PRINT '║  GET    /api/Tournament/{id}/squad/{teamId}/players        ║';
PRINT '║                                                              ║';
PRINT '║  SEEDING & DRAW                                             ║';
PRINT '║  POST   /api/Tournament/{id}/seeding                       ║';
PRINT '║  POST   /api/Tournament/{id}/draw                          ║';
PRINT '║                                                              ║';
PRINT '║  BRACKET & REPORTS                                          ║';
PRINT '║  GET    /api/Tournament/{id}/bracket                       ║';
PRINT '║  GET    /api/Tournament/{id}/teams                         ║';
PRINT '║  GET    /api/Tournament/{id}/hall-of-fame                  ║';
PRINT '║                                                              ║';
PRINT '║  FIXTURES & STANDINGS                                       ║';
PRINT '║  PUT    /api/Tournament/groups/{groupId}/standings/{matchId}║';
PRINT '║  POST   /api/Tournament/{id}/rounds/{roundId}/advance      ║';
PRINT '║                                                              ║';
PRINT '║  TOURNAMENT COMPLETION                                      ║';
PRINT '║  POST   /api/Tournament/{id}/complete                      ║';
PRINT '║                                                              ║';
PRINT '╚══════════════════════════════════════════════════════════════╝';

/* =============================================================
   RECOMMENDED TESTING SEQUENCE
   ============================================================= */

PRINT '╔══════════════════════════════════════════════════════════════╗';
PRINT '║              RECOMMENDED TEST SEQUENCE                       ║';
PRINT '╠══════════════════════════════════════════════════════════════╣';
PRINT '║                                                              ║';
PRINT '║  ▶ TEST 1: COMPLETED TOURNAMENT (read-only checks)          ║';
PRINT '║     1. GET /api/Tournament                                   ║';
PRINT '║     2. GET /api/Tournament/8001                             ║';
PRINT '║     3. GET /api/Tournament/8001/bracket                    ║';
PRINT '║     4. GET /api/Tournament/8001/teams                      ║';
PRINT '║     5. GET /api/Tournament/8001/hall-of-fame               ║';
PRINT '║                                                              ║';
PRINT '║  ▶ TEST 2: CREATE NEW TOURNAMENT                            ║';
PRINT '║     POST /api/Tournament                                    ║';
PRINT '║     Body: {"name":"Test Cup","format":11,"structure":1,     ║';
PRINT '║            "ageGroupId":3001,"hasTwoLegs":false,            ║';
PRINT '║            "startDate":"2026-08-01","endDate":"2026-08-10"} ║';
PRINT '║                                                              ║';
PRINT '║  ▶ TEST 3: INVITE & ACCEPT                                  ║';
PRINT '║     POST /api/Tournament/8002/invite/2002                  ║';
PRINT '║     PUT /api/Tournament/8002/accept/2001                   ║';
PRINT '║     GET /api/Tournament/8002/teams                         ║';
PRINT '║                                                              ║';
PRINT '║  ▶ TEST 4: SQUAD REGISTRATION                               ║';
PRINT '║     POST /api/Tournament/8002/squad/5001                   ║';
PRINT '║     Body: [6001,6002,6003,6004,6005,6006,6007,6008,        ║';
PRINT '║            6009,6010,6011]                                  ║';
PRINT '║     GET /api/Tournament/8002/squad/5001/players            ║';
PRINT '║                                                              ║';
PRINT '║  ▶ TEST 5: SEEDING & DRAW                                   ║';
PRINT '║     POST /api/Tournament/8002/seeding                      ║';
PRINT '║     POST /api/Tournament/8002/draw                         ║';
PRINT '║     GET /api/Tournament/8002/bracket                       ║';
PRINT '║     GET /api/Tournament/8002/teams (check seed numbers)   ║';
PRINT '║                                                              ║';
PRINT '║  ▶ TEST 6: GROUP+KNOCKOUT (8004)                            ║';
PRINT '║     Repeat invite, accept, squad, seeding, draw for 8004   ║';
PRINT '║     GET /api/Tournament/8004/bracket                       ║';
PRINT '║                                                              ║';
PRINT '║  ▶ TEST 7: LEAGUE (8005)                                    ║';
PRINT '║     Repeat invite, accept, squad, seeding, draw for 8005   ║';
PRINT '║     GET /api/Tournament/8005/bracket                       ║';
PRINT '║                                                              ║';
PRINT '║  ▶ TEST 8: TWO-LEG (8006)                                   ║';
PRINT '║     Repeat invite, accept, squad, seeding, draw for 8006   ║';
PRINT '║     GET /api/Tournament/8006/bracket (note 2 legs)        ║';
PRINT '║                                                              ║';
PRINT '║  ▶ TEST 9: STATUS UPDATES                                   ║';
PRINT '║     PUT /api/Tournament/{id}/status body: 2 (Registration) ║';
PRINT '║     PUT /api/Tournament/{id}/status body: 3 (InProgress)   ║';
PRINT '║     PUT /api/Tournament/{id}/status body: 4 (Completed)    ║';
PRINT '║                                                              ║';
PRINT '║  ▶ TEST 10: 7-SIDE FORMAT (8007)                            ║';
PRINT '║     Repeat flow for 8007 (squad: min 7 players only)       ║';
PRINT '║                                                              ║';
PRINT '╚══════════════════════════════════════════════════════════════╝';

COMMIT TRANSACTION;

PRINT 'Koralytics FULL tournament seed completed successfully.';
PRINT 'Total tournaments seeded: 8001 (completed) + 8002 (registration) + 8003-8008 (draft) = 8 tournaments.';
PRINT '';
PRINT 'Run the API endpoints above to test all tournament features!';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Seed failed.';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH;
GO

