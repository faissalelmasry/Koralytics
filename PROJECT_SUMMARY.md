# Koralytics - Ultimate Project Knowledge Base & Summary

> **CRITICAL DIRECTIVE FOR AI/DEVELOPERS:** This document is the definitive source of truth for the entire Koralytics platform. It contains an exhaustive breakdown of the architecture, database schema, domain models, services, and configuration. **This file MUST be updated with every architectural change, new entity, or new core service.**

---

## 1. Project Overview & Architecture
**Koralytics** is an enterprise-grade football/sports management and analytics platform.
It follows **Clean Architecture** and **Domain-Driven Design (DDD)** concepts, separated into four main layers:

- **`Koralytics.API`**: ASP.NET Core Presentation layer. Responsible for routing, HTTP requests, middlewares (Global Error Handling), JWT Authentication, Serilog logging, and Swagger documentation.
- **`Koralytics.Application`**: Business Logic. Contains DTOs, Application Services, Interfaces, AutoMapper Profiles, and FluentValidation validators.
- **`Koralytics.Domain`**: Core domain logic. Contains enterprise models (Entities), Enums, specific Exceptions, and core interfaces (`IRepository`, `IUnitOfWork`).
- **`Koralytics.Infrastructure`**: Data access and infrastructure implementation. Contains the `ApplicationDbContext`, Migrations, Generic Repository implementation, UnitOfWork implementation, and Db Seeding logic.

---

## 2. Deep Dive: Domain Entities & Database Schema (ApplicationDbContext)
The database context (`ApplicationDbContext`) inherits from `IdentityDbContext<User, Role, int>`. It is heavily partitioned into domain areas, managed by different team members. Global query filters are applied in `OnModelCreating`, along with explicit table mapping overrides (e.g., `SystemAdminUser` maps to `"SystemAdmins"`).

### Common & Identity Entities
- **Common Base**: `AuditableEntity`, `BaseEntity`
- **Identity**: `User`, `Role`

### Core Match & Player (Faissal's Entities)
- **Players**: `Player` (Mapped to "Players"), `PlayerAcademy`, `PlayerTeam`, `PlayerCard`, `PlayerCategoryRating` *(Player now has `PlayerRatings` nav → `MatchPlayerRating`)*
- **Matches**: `Match`, `MatchEvent`, `MatchLineup`, `MatchPlayerRating`, `MatchPlayerCategoryRating`
- **Tournaments (Core)**: `Tournament`, `TournamentFixture`, `TournamentGroup`

### Tournament Logic & Core Academy (Adham's Entities)
- **Tournament Extensions**: `TournamentTeam`, `TournamentGroupTeam`, `TournamentStanding`, `TournamentRound`, `TournamentSquad`, `TournamentHallOfFame`
- **Academy Basics**: `Academy`, `AgeGroup`

### Academy Settings & Super Admin (Aly's Entities)
- **Academy Management**: `AcademyAdmin` (Mapped to "AcademyAdmins"), `Team`, `AcademyLocation`, `AcademyAnnouncement`, `AcademyBadge`, `AcademyRequest`
- **Administration**: `SystemAdminUser` (Mapped to "SystemAdmins"), `RoleAuditLog`
- **Parents**: `Parent` (Mapped to "Parents"), `ParentPlayer`

### Drills & Platform Settings (Bishoy's Entities)
- **Drills Management**: `DrillCategory`, `DrillTemplate`, `DrillSession`, `Drill`, `SessionAttendance`, `DrillResult`
- **Platform Management**: `PlatformSettings`, `PlatformAuditLog`

### Player Progression & AI (Rawan's Entities)
- **Player Metrics**: `PlayerSubscription`, `PlayerGoal`, `PlayerAchievement`
- **Artificial Intelligence**: `AIReport`

### Staff, Scouting & Media (Youssef's Entities)
- **Coach Management**: `Coach` (Mapped to "Coaches"), `CoachAcademy`, `CoachTeam`, `CoachNote`, `CoachTempAccess`
- **Scouting**: `Scouter` (Mapped to "Scouters"), `ScouterShortlist`, `ScouterFollow`, `ScouterReport`, `ScouterView`
- **Player Media/Details**: `PlayerHighlight`, `PlayerPosition`

---

## 3. Application Layer — Full Service Map (by Team Member)

---

### BISHOY — Auth & Registration (Must finish FIRST — unblocks everyone)

#### Auth/
**`IAuthService` / `AuthService`**
- `LoginAsync(dto)` → validate credentials via `SignInManager` → generate JWT with claims (UserId, Role, AcademyId) → return token + expiry
- `RefreshTokenAsync(token)` → validate refresh token → generate new JWT
- `ChangePasswordAsync(userId, dto)` → validate old password → update via `UserManager`

**`IRegistrationService` / `RegistrationService`**
- `RegisterPlayerAsync(dto)` → create `ApplicationUser` → create `Player` record (TPT) → assign "Player" role → create `PlayerAcademy` + `PlayerSubscription` (Status = Unpaid)
- `RegisterCoachAsync(dto)` → create `ApplicationUser` → assign "Coach" role → create `Coach` marker + `CoachAcademy` record
- `RegisterScouterAsync(dto)` → create `ApplicationUser` → assign "Scouter" role → create `Scouter` record with `IsVerified = false`
- `RegisterParentAsync(dto)` → create `ApplicationUser` → assign "Parent" role → create `Parent` marker + `ParentPlayer` linking to child

---

### FAISSAL — Player, Match & AI (AI services must finish second — unblocks Rawan, Adham, Youssef)

#### Player/
**`IPlayerProfileService` / `PlayerProfileService`**
- `GetPlayerProfileAsync(playerId)` → fetch player + all positions + current academy + current teams → call `PlayerCardService.GetPlayerCardAsync()` → return full `PlayerProfileDto`
- `GetPlayerTimelineAsync(playerId)` → fetch all drill sessions + match ratings + achievements ordered by date
- `GetPlayerVsAcademyAverageAsync(playerId, academyId)` → calculate player's category averages → calculate academy average per category for same age group → return comparison dto
- `GetScouterViewsCountAsync(playerId, month)` → count `ScouterView` rows for this player this month

**`IPlayerCardService` / `PlayerCardService`** *(refactored — now uses `IMapper` + `PlayerCardCalculator` static helper)*
- `GetPlayerCardAsync(playerId)` → aggregate drill scores per category (weighted by difficulty via `PlayerCardCalculator`) → factor in match rating average → calculate overall rating → return `PlayerCardDto`
- `RecalculatePlayerCardAsync(playerId)` *(renamed from `RecalculateCategoryRatingAsync`)* → triggered after new `DrillResult` or `MatchPlayerRating` saved → recalculate all category ratings using `PlayerCardCalculator`
- `GetDrillToMatchTransferRateAsync(playerId)` → compare overall drill avg vs overall match rating avg → classify as Elite/Trainable/Natural/NeedsWork → return `TransferRateDto` (mapped via `PlayerProfile`)

**`PlayerCardCalculator`** *(static helper class, `Application/Services/Player/Helpers/`)*
- Pure static calculation logic extracted from `PlayerCardService` for testability
- `GetDifficultyWeight(level)` → Beginner=1.0, Intermediate=1.5, Advanced=2.0
- `CalculateWeightedDrillAvg(drills)` → weighted average of drill scores by difficulty
- `CalculateTrainingCombined(drillAvg, ...)` → blends drill + training match scores
- `RatingLookups` / `CategoryAggregate` — sealed records used internally for aggregation

**`IPlayerTransferService` / `PlayerTransferService`**
- `TransferPlayerAsync(playerId, newAcademyId)` → set current `PlayerAcademy.LeftAt = now` → create new `PlayerAcademy` with Status = Active → historical data stays linked to old academy
- `LoanPlayerAsync(playerId, academyId)` → same as transfer but Status = Loaned
- `UpdateAvailabilityAsync(playerId, status)` → update `AvailabilityStatus` on `Player`

**`IPlayerArchetypeService` / `PlayerArchetypeService`** *(depends on AI services)*
- `UpdateArchetypeAsync(playerId)` → collect player stats (position, foot, weak foot, top categories) → call `AIArchetypeService.GenerateArchetypeAsync()` → save `ArchetypePlayerName` + `ArchetypeText` on `Player`

#### Match/
**`IMatchService` / `MatchService`**
- `CreateFriendlyMatchAsync(dto)` → validate both teams exist and belong to different academies → create `Match` with Type = Friendly, Status = Scheduled
- `CreateTournamentMatchAsync(dto)` → validate `TournamentFixture` exists and `MatchId` is null → validate both teams registered in `TournamentSquad` → create `Match` with Type = Tournament → update `TournamentFixture.MatchId`
- `CreateSessionMatchAsync(dto)` → validate `DrillSession` exists → validate players belong to the session → create `Match` with Type = Session, `SessionId` filled
- `StartMatchAsync(matchId)` → validate match Status = Scheduled → set `Match.Status = Live`
- `GetFormGuideAsync(teamId, format)` → fetch last 5 matches for team filtered by format → return W/D/L sequence

**`IMatchEventService` / `MatchEventService`**
- `LogMatchEventAsync(matchId, dto)` → validate match Status = Live → validate player is in `MatchLineup` → validate `TeamId` = HomeTeamId or AwayTeamId → create `MatchEvent` → if Goal → update `Match.HomeScore` or `AwayScore` based on `TeamId`
- `GetMatchTimelineAsync(matchId)` → fetch all `MatchEvents` ordered by Minute → return timeline dto

**`IMatchRatingService` / `MatchRatingService`**
- `SubmitLineupAsync(matchId, dto)` → validate player count matches format (5, 7, or 11) → validate all players belong to the team → create `MatchLineup` records
- `SubmitMatchRatingsAsync(matchId, ratings)` → validate match Status = Live → validate exactly one MOTM per match → auto-fill Goals + Assists from `MatchEvent` per player → create `MatchPlayerRating` per player → set `Match.Status = Completed` → set `Match.WinningTeamId` based on scores → lock `MatchEvents` (no more edits) → trigger `PlayerCardService.RecalculateCategoryRatingAsync()` → trigger `AIReportService.GenerateMatchReportAsync()`

**`IMatchAnalyticsService` / `MatchAnalyticsService`** *(shared: Faissal + Youssef)*
- `GetHeadToHeadAsync(teamAId, teamBId)` → fetch all matches between two teams → return results, top scorers, MOTM count
- `GetPostMatchAnalysisAsync(teamId)` → fetch last 10 matches → detect patterns → call `AIQueryService.GeneratePostMatchAnalysisAsync()` → return pattern explanation
- `GetPlayerReadinessAsync(coachId, matchId)` *(Youssef)* → for each player in lineup → fetch last 3 `DrillSession` scores → check `AvailabilityStatus` → calculate readiness % → return readiness per player

#### AI/
**`IAIReportService` / `AIReportService`**
- `GenerateMatchReportAsync(matchId)` → fetch all match stats (events, ratings, lineup) → build Claude prompt → call Claude API → save `AIReport` with ReportType = Match
- `GenerateTournamentReportAsync(tournamentId)` → fetch standings, top scorers, MOTM counts → build Claude prompt → call Claude API → save `AIReport` with ReportType = Tournament
- `GenerateSeasonReportAsync(academyId)` → fetch all season data for academy → build Claude prompt → call Claude API → save `AIReport` with ReportType = Season

**`IAIPreviewService` / `AIPreviewService`**
- `GenerateMatchPreviewAsync(matchId)` → fetch both teams recent form + head to head → build Claude prompt → call Claude API → return preview string (not stored)
- `GenerateTournamentPreviewAsync(tournamentId)` → fetch all participating academies current form → build Claude prompt → call Claude API → return preview string (not stored)

**`IAIQueryService` / `AIQueryService`**
- `GenerateScouterQueryAsync(query, academyId?)` → build system prompt with full DB schema + tenant scope → send to Claude → returns SQL → validate and sanitize SQL → execute against DB → send results back to Claude for NL response → return final answer
- `GenerateRoleQueryAsync(role, query, userId)` → same as above scoped to role: Coach (his players), Player (own data only), AcademyAdmin (his academy), Scouter (all academies)
- `GeneratePostMatchAnalysisAsync(teamId)` → fetch last 10 matches → detect patterns → build Claude prompt → return pattern explanation

**`IAIArchetypeService` / `AIArchetypeService`**
- `GenerateArchetypeAsync(playerStatsDto)` → build Claude prompt with position, foot, weak foot, top categories → call Claude API → return `ArchetypePlayerName` + `ArchetypeText`

---

### ADHAM — Tournaments (depends on AI services for CompleteTournamentAsync)

#### Tournament/
**`ITournamentService` / `TournamentService`**
- `CreateTournamentAsync(dto)` → validate Super Admin role → create `Tournament` record → if Structure = League → auto-create dummy `TournamentGroup`
- `InviteAcademyAsync(tournamentId, academyId)` → create `TournamentTeam` with Status = Invited
- `AcceptInvitationAsync(tournamentId, academyId)` → update `TournamentTeam.Status = Accepted`
- `RegisterSquadAsync(tournamentId, teamId, playerIds)` → validate player count within tournament rules → create `TournamentSquad` records for each player

**`ITournamentDrawService` / `TournamentDrawService`**
- `GenerateSeedingAsync(tournamentId)` → fetch all accepted `TournamentTeams` → calculate seed score per academy (win rate + average player rating + previous tournament results) → assign `SeedNumber` ordered by score
- `GenerateDrawAsync(tournamentId)`:
  - Knockout: create `TournamentRound` (Round 1) → randomly assign teams to fixtures respecting seeds
  - GroupAndKnockout: create `TournamentGroups` → randomly assign teams respecting seeds → create fixtures per group (`LegNumber` if `HasTwoLegs`)
  - League: use existing dummy group → create all fixtures (every team vs every other) → if `HasTwoLegs` → home and away fixtures

**`ITournamentFixtureService` / `TournamentFixtureService`**
- `UpdateStandingsAsync(groupId, matchId)` → fetch Match result → update `TournamentStanding` for both teams (Played++, Won/Drawn/Lost, GoalsFor/Against, Points 3/1/0)
- `AdvanceKnockoutAsync(tournamentId, roundId)` → fetch all fixtures in current round → validate all matches completed → collect winners from `Match.WinningTeamId` → create next `TournamentRound` → create new fixtures pairing winners

**`ITournamentReportService` / `TournamentReportService`**
- `CompleteTournamentAsync(tournamentId)` → validate all fixtures completed → determine winner from final fixture → create `TournamentHallOfFame` records (top scorer, most assists, most MOTM, best goalkeeper) → update `Tournament.Status = Completed` → trigger `AIReportService.GenerateTournamentReportAsync()`
- `GetBracketAsync(tournamentId)` → fetch all `TournamentRounds` + `TournamentFixtures` + `TournamentGroups` + `TournamentStandings` → return full bracket dto

---

### ALY — Drills, Academy & Announcements (DrillResultService unblocks Faissal's RecalculateCategoryRatingAsync)

#### Drill/
**`IDrillSessionService` / `DrillSessionService`**
- `CreateSessionAsync(dto)` → validate team + coach exist and are linked via `CoachTeam` → create `DrillSession` → create `SessionAttendance` for each player in `dto.PlayerIds`
- `AddDrillToSessionAsync(sessionId, dto)` → validate session exists and belongs to coach's team → validate `DrillTemplate` exists → create `SessionDrill`
- `GetSessionHistoryAsync(coachId, filters)` → fetch all sessions for coach → filterable by date range, team, category → return paginated list

**`IDrillResultService` / `DrillResultService`**
- `LogBulkDrillResultsAsync(sessionDrillId, results)` → validate each player attended the session → Mode 1: `FinalScore = ManualScore`; Mode 2: `FinalScore = DoneCount / (DoneCount + MissedCount) * 10` → bulk create `DrillResult` records → trigger `PlayerCardService.RecalculateCategoryRatingAsync()`
- `GetPlayerDrillProgressionAsync(playerId, category)` → fetch all `DrillResults` for player in category ordered by date → return progression over time

**`IDrillAnalyticsService` / `DrillAnalyticsService`**
- `GetSquadWeakCategoriesAsync(teamId)` → calculate average score per category for all players in team → return categories ranked weakest to strongest
- `DetectCoachBiasAsync(coachUserId, academyId)` → fetch all `DrillResults` logged by this coach (Mode 1 only) → fetch `MatchPlayerRatings` for same players → compare drill scores vs match performance → calculate bias score → update `CoachAcademy.BiasScore` + `BiasLastCalculatedAt`

**`IDrillTemplateService` / `DrillTemplateService`**
- `CreateTemplateAsync(dto)` → validate category exists → AcademyAdmin/Coach: `AcademyId` filled; SuperAdmin: `AcademyId = null` (system-wide) → create `DrillTemplate`
- `GetTemplatesAsync(academyId)` → fetch system-wide (`AcademyId = null`) + academy-specific → return combined list
- `ShareTemplateAsync(templateId)` → validate requester owns the template → set `DrillTemplate.IsShared = true`
- `GetTemplatesByCategoryAsync(categoryId, academyId)` → filter by category → return system-wide + academy-specific

#### Academy/
**`IAcademyService` / `AcademyService`**
- `CreateAcademyAsync(dto)` → called after SuperAdmin approves `AcademyRequest` → create `Academy` record → update `AcademyRequest.AcademyId` + Status = Approved
- `UpdateAcademyAsync(academyId, dto)` → update name, logo, colors
- `AddLocationAsync(academyId, dto)` → create `AcademyLocation` → if first location → set `IsMain = true`

**`IAcademyTeamService` / `AcademyTeamService`**
- `CreateAgeGroupAsync(academyId, dto)` → validate min/max age range doesn't overlap existing groups → create `AgeGroup`
- `CreateTeamAsync(academyId, dto)` → validate `AgeGroup` belongs to academy → create `Team`
- `AssignCoachToTeamAsync(coachId, teamId)` → validate coach belongs to same academy → create `CoachTeam` record
- `RemoveCoachFromTeamAsync(coachId, teamId)` → set `CoachTeam.RemovedAt = now`

**`IAcademyAnalyticsService` / `AcademyAnalyticsService`**
- `GetCoachPerformanceDashboardAsync(academyId)` → for each coach: fetch all players in his teams → calculate average player improvement rate → fetch `BiasScore` from `CoachAcademy` → return ranked coach performance list
- `GetSubscriptionStatusAsync(academyId)` → fetch all `PlayerSubscriptions` for academy → group by Status (Paid, Unpaid, Grace) → return summary + list of unpaid players

**`IAcademyAnnouncementService` / `AcademyAnnouncementService`**
- `SendAnnouncementAsync(academyId, dto)` → validate TargetType (All, Team, AgeGroup, Role) → create `AcademyAnnouncement` → trigger `NotificationService.SendAnnouncementAsync()`
- `RemovePlayerAsync(academyId, playerId, coachId)` → validate subscription unpaid and grace period expired → validate requester is coach of player's team → set `PlayerAcademy.LeftAt = now` → log to `RoleAuditLog`

---

### RAWAN — Scouting & Notifications (depends on AI services for NL search)

#### Scouter/
**`IScouterSearchService` / `ScouterSearchService`**
- `SearchPlayersNLAsync(query, scouterId)` → call `AIQueryService.GenerateScouterQueryAsync()` → return player results
- `SearchPlayersFilteredAsync(filters)` → filter by position, age, format, rating range, foot, academy → return paginated `PlayerCardDto` list

**`IScouterShortlistService` / `ScouterShortlistService`**
- `AddToShortlistAsync(scouterId, playerId)` → validate player exists → create `ScouterShortlist` record
- `RemoveFromShortlistAsync(scouterId, playerId)` → soft delete `ScouterShortlist` record
- `GetShortlistAsync(scouterId)` → fetch all shortlisted players → return list of `PlayerCardDto`

**`IScouterFollowService` / `ScouterFollowService`**
- `FollowPlayerAsync(scouterId, playerId)` → create `ScouterFollow` record
- `UnfollowPlayerAsync(scouterId, playerId)` → soft delete `ScouterFollow` record
- `GetFollowedPlayersAsync(scouterId)` → fetch all followed players with latest stats
- `LogProfileViewAsync(scouterId, playerId)` → create `ScouterView` record → called automatically when scouter opens any player profile

**`IScouterReportService` / `ScouterReportService`**
- `GenerateScoutingReportAsync(scouterId, playerId)` → fetch full player data → call `AIReportService.GenerateScoutingReportAsync()` → create `ScouterReport` record → return report text
- `GetScoutingReportAsync(scouterId, playerId)` → fetch existing `ScouterReport` if exists, else call `GenerateScoutingReportAsync`
- `VerifyScouterAsync(scouterId)` → SuperAdmin only → set `Scouter.IsVerified = true` + `VerifiedAt = now`

#### Notification/
**`IAnnouncementNotificationService` / `AnnouncementNotificationService`**
- `SendAnnouncementAsync(announcement)` → based on TargetType: All (all academy users), Team (players + parents), AgeGroup (all players in age group), Role (all users with that role) → via SignalR for real-time

**`IPlayerNotificationService` / `PlayerNotificationService`**
- `NotifyPlayerMilestoneAsync(playerId, achievementType)` → send notification to player when milestone reached
- `NotifyParentAsync(playerId, eventType)` → fetch `ParentPlayer` records → notify each parent via SignalR
- `NotifySubscriptionGraceAsync(playerId, academyId)` → notify player + parent that subscription in grace period → triggered by scheduled job

**`IScouterNotificationService` / `ScouterNotificationService`**
- `NotifyScouterFollowersAsync(playerId, eventType)` → when player posts highlight, wins MOTM, or rating improves → fetch all `ScouterFollow` records for this player → notify each scouter via SignalR

---

### YOUSSEF — Coach, Storage & Match Readiness

#### Coach/
**`ICoachSquadService` / `CoachSquadService`**
- `GetSquadAsync(coachId, teamId)` → fetch all active `PlayerTeam` records for team → include each player's FIFA card rating + availability → return squad overview dto
- `SplitTrainingTeamsAsync(sessionId)` → fetch all players attending session → sort by overall rating → alternate assignment Team A/B → return two balanced groups
- `GetSquadComparisonAsync(playerAId, playerBId)` → fetch both players category ratings + match stats → return side-by-side comparison dto

**`ICoachNoteService` / `CoachNoteService`**
- `WriteNoteAsync(coachId, dto)` → validate player belongs to coach's team → create `CoachNote` (`SessionId` or `MatchId` nullable depending on context)
- `GetPlayerNotesAsync(coachId, playerId)` → fetch all `CoachNotes` for this player by this coach

**`ICoachAccessService` / `CoachAccessService`**
- `GrantTempAccessAsync(coachId, dto)` → validate grantee exists → create `CoachTempAccess` with Status = Active
- `RevokeTempAccessAsync(coachId, accessId)` → validate coach owns this access record → set `CoachTempAccess.Status = Revoked`

#### Storage/ (Cloudflare R2)
**`IStorageService` / `StorageService`**
- `UploadHighlightAsync(playerId, academyId, file)` → validate file size and format (video only) → generate unique file name → upload to Cloudflare R2 → create `PlayerHighlight` record with `VideoUrl` → return `PlayerHighlight` dto
- `DeleteHighlightAsync(highlightId, playerId)` → validate player owns the highlight → delete from Cloudflare R2 → soft delete `PlayerHighlight` record
- `PinHighlightAsync(highlightId, playerId)` → unpin any existing pinned highlight for this player → set `PlayerHighlight.IsPinned = true`
- `GetHighlightsAsync(playerId)` → fetch all `PlayerHighlight` records → return ordered (pinned first, then by date)

#### Match/ (Youssef's contribution to shared MatchAnalyticsService)
- `GetPlayerReadinessAsync(coachId, matchId)` → for each player in lineup: fetch last 3 `DrillSession` scores → check `AvailabilityStatus` → calculate readiness % → return readiness per player

---

## 4. Service Dependency Order

Bishoy (RegistrationService) must finish FIRST — unblocks everyone (users must exist)

Faissal (AI services) must finish SECOND
- unblocks Rawan (SearchPlayersNLAsync)
- unblocks Adham (CompleteTournamentAsync)
- unblocks Faissal (UpdateArchetypeAsync)
- unblocks Youssef (GetPostMatchAnalysisAsync)

Aly (DrillResultService) must finish before Faissal (RecalculateCategoryRatingAsync)

---

## 5. API & Infrastructure Setup
The presentation and infrastructure are wired together in `Program.cs`.

### Security & Identity
* **ASP.NET Core Identity**: Configured with custom `User` and `Role` entities. Key type is `int`.
* **JWT Bearer Authentication**:
  * Validates Issuer, Audience, Lifetime, and Signing Key.
  * Clock Skew is configurable via `Jwt:ClockSkewMinutes` (defaults to 1).

### Infrastructure Patterns
* **Generic Repository (`Repository<T>`)**: Implements `IRepository<T>` for standard CRUD operations.
* **Unit of Work (`UnitOfWork`)**: Implements `IUnitOfWork` for transactional boundary control over DbContext.
* **Database Seeding**: Triggered on application startup via `DbInitializer.SeedAsync`.

### Cross-Cutting Concerns
* **Global Error Handling**: Uses ASP.NET Core 8+ `IExceptionHandler` (`GlobalExceptionHandler`) to catch unhandled exceptions and return standardized `ProblemDetails` responses.
* **Logging**: Uses **Serilog**. Configured to write to Console and rolling log files at `Logs/log-.txt` (RollingInterval.Day).
* **API Documentation**: Swagger is configured with a Security Definition for JWT Bearer Tokens, allowing authenticated requests directly from the Swagger UI.
* **Real-Time**: **SignalR** used for push notifications (announcements, player milestones, scouter alerts).
* **External Storage**: **Cloudflare R2** for player highlight video uploads.
* **AI Provider**: **Claude API** used for all AI report generation, NL querying, and player archetype generation.

### AutoMapper Profiles Currently Registered
* `RegisterProfile` — Registration DTOs to Domain Entities
* `TournamentProfile` — Tournament DTOs to Domain Entities
* `PlayerProfile` — `PlayerCard` → `TransferRateDto` (maps `PlayerName`, `TransferGap`, `Classification`)

### FluentValidation Validators Currently Registered
* `LoginRequestValidator`
* `BaseRegisterationRequestValidator`
* `ChangePasswordValidator`
* `CreateTournamentValidator`
* `RegisterSquadValidator`
* `UserBusinessValidator` (injected directly in workflows)

### Unit Test Project
* **`Koralytics.Application.UnitTests`**: xUnit test project covering Application layer logic.
  * `PlayerCardCalculatorTests` — unit tests for all `PlayerCardCalculator` static methods
  * `PlayerCardServiceTests` — integration-style tests for `PlayerCardService` using mocked `IUnitOfWork`

---

## 6. Active Controllers
* **`ApiBaseController`**: Base controller providing shared API logic and standardized responses.
* **`LoginController`**: Handles user login and token delivery.
* **`RegisterController`**: Handles varied role registrations (Player, Coach, Scouter, Parent).
* **`PlayerController`**: Main interface for player operations (transfers, profiling, card).
* **`TournamentController`**: Interface for tournament-related endpoints (create, draw, fixtures, bracket).
* **`CoachController`**: Interface for coach squad operations (squad overview, training team split, player comparison).

---

## 7. Rules for Updating This Document
For any AI agent or Developer working on this codebase:
1. **Adding a Table/Entity:** You MUST append the entity name to the **Deep Dive: Domain Entities** section under the appropriate category.
2. **Adding an API/Controller:** Add it to the **Active Controllers** list.
3. **Adding a Service:** Document its purpose and key methods under the relevant team member section in **Section 3**.
4. **Altering Configuration:** If you change JWT config, Logging, Error Handling, AutoMapper profiles, or FluentValidation in `Program.cs`, update the **API & Infrastructure Setup** section.
5. **New External Dependency:** Document it under **Cross-Cutting Concerns** in the API & Infrastructure Setup section.
