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
- **Identity**: `User`, `Role`, `RefreshToken`

### Core Match & Player (Faissal's Entities)
- **Players**: `Player` (Mapped to "Players"), `PlayerAcademy`, `PlayerTeam`, `PlayerCard`, `PlayerCategoryRating` *(Player now has `PlayerRatings` nav → `MatchPlayerRating`)*
- **Matches**: `Match`, `MatchEvent`, `MatchLineup`, `MatchPlayerRating`, `MatchPlayerCategoryRating`, `MatchRequest`
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
- **Player Metrics**: `PlayerSubscription`, `PlayerGoal`, `PlayerAchievement`, `PlayerCard`
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
- `LoginAsync(dto)` → validate credentials via `SignInManager` → generate JWT via `TokenService` → return token pair
- `RefreshTokenAsync(token)` → validate refresh token → generate new token pair via `TokenService`
- `ChangePasswordAsync(userId, dto)` → validate old password → update via `UserManager`
- `OAuthLoginOrRegisterAsync(request)` → handle Google OAuth login/registration
- `CompleteOAuthProfileAs[Role]Async` → complete registration for OAuth users by role

**`ITokenService` / `TokenService`**
- `GenerateTokenPairAsync(...)` → generates JWT and Refresh Token
- `RefreshTokensAsync(...)` → handles token refresh logic

**OAuth & Cookie Services**
- **`IOAuthProviderFactory` / `OAuthProviderFactory`**: resolves correct provider (e.g., `GoogleOAuthProvider`)
- **`ICookieService` / `CookieService`**: sets/gets refresh token cookies

**`IRegistrationService` / `RegistrationService`**
- `RegisterPlayerAsync(dto)` → create `ApplicationUser` via UserManager → create `Player` record → assign "Player" role → create `PlayerAcademy` + `PlayerSubscription` (Status = Unpaid) → return `AuthResultDto`
- `RegisterCoachAsync(dto)` → create `ApplicationUser` → assign "Coach" role → create `Coach` marker + `CoachAcademy` record → return `AuthResultDto`
- `RegisterScouterAsync(dto)` → create `ApplicationUser` → assign "Scouter" role → create `Scouter` record with `IsVerified = false` → return `AuthResultDto`
- `RegisterParentAsync(dto)` → create `ApplicationUser` → assign "Parent" role → create `Parent` marker + `ParentPlayer` linking to child → return `AuthResultDto`
- `RegisterAcademyAdminAsync(dto)` ⚡ *(added beyond original plan)* → create `ApplicationUser` → assign "AcademyAdmin" role → return `AuthResultDto`

---

### FAISSAL — Player, Match & AI (AI services must finish second — unblocks Rawan, Adham, Youssef)

#### Player/
**`IPlayerProfileService` / `PlayerProfileService`**
- `GetPlayerProfileAsync(playerId)` → fetch player + all positions + current academy + current teams → call `PlayerCardService.GetPlayerCardAsync()` → return full `PlayerProfileDto`
- `GetDrillTimelineAsync(playerId, page, pageSize)` ⚡ *(original plan had one `GetPlayerTimelineAsync`; split into 3 paginated methods)* → paginated drill session history
- `GetMatchTimelineAsync(playerId, page, pageSize)` → paginated match rating history
- `GetAchievementTimelineAsync(playerId, page, pageSize)` → paginated achievement history
- `GetPlayerVsAcademyAverageAsync(playerId, academyId)` → calculate player's category averages vs academy average for same age group → return `PlayerVsAcademyAverageDto`
- `GetScouterViewsCountAsync(playerId, year, month)` ⚡ *(signature changed: now takes `year` + `month`)* → count `ScouterView` rows for this player → return `ScouterViewsCountDto`

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

**`CardInvalidationList`** *(background hosted service, `Application/Services/Player/Helpers/`)*
- Registered as **Singleton + `IHostedService`** in `Program.cs`
- Uses a `ConcurrentDictionary<int, bool>` as an in-memory dirty-list of player IDs whose `PlayerCard` needs recalculation
- `Invalidate(playerId)` → adds player to dirty set; `TryConsume(playerId)` → removes and returns true if present
- **`StartAsync`**: on app startup, queries DB for all `PlayerCard` records where `NeedsRecalculation = true` and pre-populates the in-memory set (crash-safe restore)
- **`StopAsync`**: on graceful shutdown, writes all remaining dirty IDs back to DB (`NeedsRecalculation = true`) so no pending recalculations are lost
- Injected into scouter services (`ScouterSearchService`, `ScouterShortlistService`, `ScouterFollowService`) which call `TryConsume()` before returning player cards to ensure stale cards are recalculated on-demand

**`IPlayerTransferService` / `PlayerTransferService`**
- `TransferPlayerAsync(playerId, newAcademyId, requesterAcademyId)` ⚡ *(extra `requesterAcademyId` param for authorization)* → set current `PlayerAcademy.LeftAt = now` → create new `PlayerAcademy` with Status = Active
- `LoanPlayerAsync(playerId, academyId, requesterAcademyId)` → same as transfer but Status = Loaned
- `UpdateAvailabilityAsync(playerId, status, requesterAcademyId, requesterRole)` ⚡ *(extra auth params)* → update `AvailabilityStatus` on `Player`

**`IPlayerArchetypeService` / `PlayerArchetypeService`** — **⚠️ NOT IMPLEMENTED** *(no service file exists; planned but blocked on AI services)*

**`IPlayerGoalService` / `PlayerGoalService`**
- `CreatePlayerGoalAsync(playerId, dto)` → creates a goal for a player
- `UpdatePlayerGoalAsync(goalId, dto)` → updates an existing player goal

#### Match/
**`IMatchService` / `MatchService`**
- `CreateFriendlyMatchAsync(dto)` / `CreateTournamentMatchAsync(dto)` / `CreateSessionMatchAsync(dto)` → creates matches
- `GetMatchAsync(matchId)` / `EndMatchAsync(matchId)`
- `GetFormGuideAsync(teamId, format)` / `GetMatchesByDateAsync(date, page, pageSize)` / `GetTeamMatchesByStatusAsync(...)`

**`IMatchRequestService` / `MatchRequestService`**
- `RequestFriendlyMatchAsync(coachId, dto)` → requests a friendly match
- `AcceptMatchRequestAsync(requestId, coachId)` → creates actual Match upon acceptance
- `GetPendingRequestsAsync(teamId)` / `GetSentRequestsAsync(teamId)`

**`IMatchRatingService` / `MatchRatingService`**
- `GetLineupAsync(matchId)` / `GetMatchRatingsAsync(matchId)`

**`IMatchEventService` / `MatchEventService`**
- `LogMatchEventAsync(matchId, dto)` / `LogSessionMatchEventAsync(matchId, dto)`
- `GetMatchTimelineAsync(matchId)`

**`IMatchAnalyticsService` / `MatchAnalyticsService`**
- `GetHeadToHeadAsync(teamAId, teamBId)`
- `GetPostMatchAnalysisAsync(teamId)`
- `GetPlayerReadinessAsync(playerId)` → calculates player readiness based on AvailabilityStatus and recent match load

#### AI/ — **⚠️ NOT IMPLEMENTED**
> All AI services (`AIReportService`, `AIPreviewService`, `AIQueryService`, `AIArchetypeService`) were **planned but not yet created**. No service files exist. This is the primary blocker for: `ScouterReportService.GenerateScoutingReportAsync`, `PlayerArchetypeService`, `CompleteTournamentAsync` AI trigger, and `GetPostMatchAnalysisAsync`.

---

### ADHAM — Tournaments (depends on AI services for CompleteTournamentAsync)

#### Tournament/
**`ITournamentService` / `TournamentService`**
- `CreateTournamentAsync(dto, requestingUserId)` ⚡ *(extra `requestingUserId` param)* → validate `AgeGroup` exists → create `Tournament` record → if Structure = League → auto-create dummy `TournamentGroup`
- `InviteAcademyAsync(tournamentId, academyId)` → validate tournament + academy exist → create `TournamentTeam` with Status = Invited
- `AcceptInvitationAsync(tournamentId, academyId)` → update `TournamentTeam.Status = Accepted`
- `RegisterSquadAsync(tournamentId, teamId, playerIds)` → validate player count within tournament rules → create `TournamentSquad` records for each player
- `UpdateStatusAsync(tournamentId, status)` ⚡ *(added beyond original plan)* → update `Tournament.Status`

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
- `CreateSessionAsync(dto, currentCoachId, currentAcademyId)` ⚡ *(context params added)* → validate team + coach linked via `CoachTeam` → create `DrillSession` → create `SessionAttendance` for each player → return `DrillSessionDto`
- `AddDrillToSessionAsync(sessionId, dto, currentCoachId)` → validate session ownership → validate `DrillTemplate` exists → create `Drill` record → return `DrillDto`
- `GetCoachSessionsAsync(currentCoachId, currentAcademyId, filter)` ⚡ *(renamed from `GetSessionHistoryAsync`; filterable via `SessionFilterDto`)* → return `IEnumerable<DrillSessionDto>`
- `GetSessionByIdAsync(sessionId, currentCoachId)` ⚡ *(new)* → return `DrillSessionDetailsDto` (includes drills + attendance)
- `UpdateSessionAsync(sessionId, dto, currentCoachId)` ⚡ *(new)* → update session metadata → return `DrillSessionDto`
- `RemoveDrillFromSessionAsync(sessionId, drillId, currentCoachId)` ⚡ *(new)* → validate ownership → remove drill
- `DeleteSessionAsync(sessionId, currentCoachId)` ⚡ *(new)* → soft-delete session
- `CompleteSessionAsync(sessionId, currentCoachId)` ⚡ *(new)* → set session status to Completed

**`IDrillResultService` / `DrillResultService`**
- `SubmitResultsAsync(sessionId, drillId, dto, currentCoachId)` ⚡ *(renamed from `LogBulkDrillResultsAsync`; takes sessionId + drillId)* → validate coach owns session → Mode 1/2 score calculation → bulk create `DrillResult` records → trigger `PlayerCardService.RecalculatePlayerCardAsync()`
- `MarkAttendanceAsync(sessionId, dto, currentCoachId)` ⚡ *(new)* → bulk update `SessionAttendance.IsPresent` flags
- `GetPlayerDrillProgressionAsync(playerId, categoryId, currentAcademyId)` ⚡ *(takes `categoryId` int, not category name string)* → return `PlayerProgressionDto`
- `GetDrillResultsAsync(sessionId, drillId, currentCoachId)` ⚡ *(new)* → return `IEnumerable<DrillResultDto>` for a specific drill
- `GetSessionAttendanceAsync(sessionId, currentCoachId)` ⚡ *(new)* → return `IEnumerable<PlayerAttendanceDto>`

**`IDrillAnalyticsService` / `DrillAnalyticsService`**
- `GetSquadWeakCategoriesAsync(teamId)` → return `IEnumerable<CategoryPerformanceDto>` ranked weakest to strongest
- `DetectCoachBiasAsync(coachUserId, academyId)` → return `CoachBiasReportDto`

**`IDrillTemplateService` / `DrillTemplateService`**
- `CreateTemplateAsync(dto, currentUserId, currentUserRole, currentUserAcademyId?)` ⚡ *(context params added)* → validate category → set `AcademyId` based on role → return `DrillTemplateDto`
- `GetTemplatesAsync(academyId, currentUserId, filter)` ⚡ *(context params added; filter via `TemplateFilterDto`)* → return `IEnumerable<DrillTemplateDto>`
- `GetTemplatesByCategoryAsync(categoryId, academyId, currentUserId, filter)` → return `IEnumerable<DrillTemplateDto>`
- `GetTemplateByIdAsync(id, currentUserId, currentUserAcademyId?)` ⚡ *(new)* → return single `DrillTemplateDto`
- `ShareTemplateAsync(templateId, currentUserId, currentUserRole, currentUserAcademyId?)` ⚡ *(context params added)* → validate ownership → set `IsShared = true`
- `UpdateTemplateAsync(id, dto, currentUserId, currentUserRole, currentUserAcademyId?)` ⚡ *(new)* → update template fields → return `DrillTemplateDto`
- `DeleteTemplateAsync(id, currentUserId, currentUserRole, currentUserAcademyId?)` ⚡ *(new)* → validate ownership → soft-delete

#### Academy/
**`IAcademyService` / `AcademyService`**
- `CreateAcademyAsync(dto, performedByUserId)` → validate `AcademyRequest` exists and is Pending → validate name uniqueness → wrapped in **DB transaction**: create `Academy` (Status = Active) → mark `AcademyRequest.RequestStatus = Approved` → create `RoleAuditLog` entry for the AcademyAdmin assignment → commit; rollback on failure
- `UpdateAcademyAsync(academyId, dto, performedByUserId)` → update name (unique check), LogoUrl, PrimaryColor, SecondaryColor → return `AcademyResponseDto`
- `AddLocationAsync(academyId, dto, performedByUserId)` → validate academy exists → check duplicate location name → if first location → `IsMain = true` automatically → return `AcademyLocationResponseDto`
- `GetAcademyAsync(academyId)` → return single `AcademyResponseDto` (includes Admin nav)
- `GetAllAcademiesAsync()` → return all academies as `IEnumerable<AcademyResponseDto>`
- `GetLocationsAsync(academyId)` → return all `AcademyLocationResponseDto` for the academy
- `SetMainLocationAsync(academyId, locationId, performedByUserId)` → clears existing `IsMain` flag → sets target location as main

**`IAcademyTeamService` / `AcademyTeamService`**
- `CreateAgeGroupAsync(academyId, dto, performedByUserId)` ⚡ *(extra auth param)* → validate age range non-overlapping → create `AgeGroup` → return `AgeGroupResponseDto`
- `CreateTeamAsync(academyId, dto, performedByUserId)` → validate `AgeGroup` belongs to academy → create `Team` → return `TeamResponseDto`
- `AssignCoachToTeamAsync(coachUserId, teamId, performedByUserId)` ⚡ *(renamed param)* → validate coach belongs to same academy → create `CoachTeam` → return `CoachTeamAssignmentDto`
- `RemoveCoachFromTeamAsync(coachUserId, teamId, performedByUserId)` → set `CoachTeam.RemovedAt = now`
- `GetTeamsByAcademyAsync(academyId)` ⚡ *(new)* → return `IEnumerable<TeamResponseDto>`
- `GetAgeGroupsByAcademyAsync(academyId)` ⚡ *(new)* → return `IEnumerable<AgeGroupResponseDto>`

**`IAcademyAnalyticsService` / `AcademyAnalyticsService`**
- `GetCoachPerformanceDashboardAsync(academyId)` → return `IEnumerable<CoachPerformanceDto>` ranked by performance
- `GetSubscriptionStatusAsync(academyId)` → return `SubscriptionStatusSummaryDto`

**`IAcademyAnnouncementService` / `AcademyAnnouncementService`**
- `SendAnnouncementAsync(academyId, dto, sentByUserId)` ⚡ *(extra auth param)* → validate TargetType → create `AcademyAnnouncement` → return `AnnouncementResponseDto`
- `GetAnnouncementsAsync(academyId)` ⚡ *(new)* → return `IEnumerable<AnnouncementResponseDto>`
- `RemovePlayerAsync(academyId, playerId, coachUserId, reason)` ⚡ *(extra `reason` string param)* → validate subscription + grace period → validate coach owns player's team → set `PlayerAcademy.LeftAt = now` → log to `RoleAuditLog`

---

### RAWAN — Scouting & Notifications (depends on AI services for NL search)

#### Scouter/ *(services in namespace `ScouterServices.*`, implemented)*
**`IScouterSearchService` / `ScouterSearchService`**
- `SearchPlayersAsync(filters: PlayerSearchFiltersDto)` → applies optional filters (MinAge, MaxAge, PreferredFoot, Positions, AcademyId, Format, MinRating, MaxRating) → paginates results (`PageNumber`, `PageSize`) → for each page: checks `CardInvalidationList.TryConsume()` + `NeedsRecalculation` flag and recalculates stale cards on-demand → returns `PaginatedResult<PlayerCardDto>` *(NL/AI search is planned but not yet wired)*

**`IScouterShortlistService` / `ScouterShortlistService`**
- `AddToShortlistAsync(scouterId, playerId)` → validate scouter + player exist → idempotent (returns existing if already shortlisted) → create `ScouterShortlist` record → return `ScouterShortlistDto`
- `RemoveFromShortlistAsync(scouterId, playerId)` → soft-delete `ScouterShortlist` record → return `bool`
- `GetShortlistAsync(scouterId)` → fetch shortlisted player IDs → recalculate stale cards via `CardInvalidationList.TryConsume()` → return `List<PlayerCardDto>` ordered by shortlist insertion (newest-first)

**`IScouterFollowService` / `ScouterFollowService`**
- `FollowPlayerAsync(scouterId, playerId)` → validate scouter + player exist → idempotent (silently returns if already following) → create `ScouterFollow` record
- `UnfollowPlayerAsync(playerId, scouterId)` *(note: parameter order is playerId first)* → soft-delete `ScouterFollow` record → throws `NotFoundException` if not currently following
- `GetFollowedPlayersAsync(scouterId)` → fetch followed player IDs (excluding soft-deleted) → recalculate stale cards via `CardInvalidationList.TryConsume()` → return `List<PlayerCardDto>`
- `LogProfileViewAsync(scouterId, playerId)` → validate scouter + player exist → create `ScouterView` record *(TODO comment: should move to fire-and-forget background worker)*

**`IScouterReportService` / `ScouterReportService`**
- `GenerateScoutingReportAsync(scouterId, playerId)` → **⚠️ NOT IMPLEMENTED** (throws `NotImplementedException` — blocked waiting on AI service integration)
- `GetScoutingReportAsync(scouterId, playerId)` → return existing `ScouterReport` if found; else calls `GenerateScoutingReportAsync` (currently throws until AI is wired) → returns `ScouterReport` entity
- `VerifyScouterAsync(scouterId)` → validate scouter exists → set `Scouter.IsVerified = true` + `VerifiedAt = DateTime.UtcNow` → return `bool`

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
**`ICoachSquadService` / `CoachSquadService`** *(implemented)*
- `GetSquadAsync(coachId, teamId)` → verify coach is assigned to team (via `CoachTeam`) → load all active `PlayerTeam` records → load `PlayerCard` per player → return `SquadOverviewDto`
- `SplitTrainingTeamsAsync(coachId, sessionId)` → verify coach owns session → load attending players (`IsPresent = true`) → sort by `OverallRating` → snake-draft into Team A / Team B → return `TrainingTeamSplitDto`
- `GetSquadComparisonAsync(playerAId, playerBId)` → load both players + their `PlayerCard` → return `SquadComparisonDto`

**`ICoachNoteService` / `CoachNoteService`** *(implemented)*
- `WriteNoteAsync(coachId, academyId, dto)` ⚡ *(original plan had `(coachId, dto)` only; `academyId` added)* → validate player belongs to one of coach's active teams → create `CoachNote` (optionally linked to `SessionId` / `MatchId`) → return `CoachNoteDto`
- `GetPlayerNotesAsync(coachId, playerId)` → fetch all `CoachNotes` by this coach for this player → return `IEnumerable<CoachNoteDto>` newest-first

**`ICoachAccessService` / `CoachAccessService`** *(implemented)*
- `GrantTempAccessAsync(coachId, dto)` → validate future expiry + non-empty `AccessLevel` + grantee exists → prevent self-grant → create `CoachTempAccess` (Status stored as string `"Active"`) → return `TempAccessDto`
- `RevokeTempAccessAsync(coachId, accessId)` → validate coach owns record → validate Status ≠ `"Revoked"` → set Status = `"Revoked"` → return `TempAccessDto`
- `GetActiveGrantsAsync(coachId)` → fetch grants where Status = `"Active"` and `ExpiresAt > now` → return `IEnumerable<TempAccessDto>` newest-first

#### Storage/ (Cloudflare R2) — **✅ IMPLEMENTED**
- `UploadHighlightAsync(playerId, academyId, file, title)` → validate file size (<100MB) and format (video only) → generate unique file name → upload to Cloudflare R2 via `IAmazonS3` → create `PlayerHighlight` record with `VideoUrl` → return `PlayerHighlightDto`
- `DeleteHighlightAsync(highlightId, playerId)` → validate player owns highlight → delete from Cloudflare R2 → soft delete `PlayerHighlight` record
- `PinHighlightAsync(highlightId, playerId)` → unpin any existing pinned highlight for player → set `PlayerHighlight.IsPinned = true`
- `GetHighlightsAsync(playerId)` → fetch player highlights ordered pinned-first then newest-first

#### Match/ (Youssef's contribution) — **✅ IMPLEMENTED**
> `GetPlayerReadinessAsync` has been successfully implemented under `MatchAnalyticsService`.

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
* **Authentication Providers**: **Google OAuth** integrated for third-party sign-ins, managed via `OAuthProviderFactory`.
* **Background Services**: **`CardInvalidationList`** registered as `Singleton` + `IHostedService`. Uses `ConcurrentDictionary` as in-memory dirty-list. On startup it restores pending IDs from DB (`NeedsRecalculation = true`); on shutdown it persists the dirty set back to DB. Card recalculation is triggered **on-demand** by scouter services (not by the hosted service loop itself).

### AutoMapper Profiles Currently Registered
* `RegisterProfile` — Registration DTOs → Domain Entities
* `TournamentProfile` — Tournament DTOs → Domain Entities
* `PlayerProfile` — `PlayerCard` → `TransferRateDto` (maps `PlayerName`, `TransferGap`, `Classification`)
* `AcademyProfile` — Academy DTOs → Domain Entities (`Academy`, `AgeGroup`, `Team`, `AcademyLocation`, `AcademyAnnouncement`)
* `DrillMappingProfile` — Drill DTOs → Domain Entities (`DrillTemplate`, `DrillSession`, `DrillResult`, etc.)
* `ScouterProfile` — Scouter DTOs → Domain Entities (`ScouterShortlist`, `ScouterFollow`, `ScouterReport`)

### FluentValidation Validators Currently Registered
* `LoginRequestValidator`
* `BaseRegisterationRequestValidator`
* `ChangePasswordValidator`
* `CreateTournamentValidator`
* `RegisterSquadValidator`
* `CreateAcademyValidator`
* `UpdateAcademyValidator`
* `AddLocationValidator`
* `CreateAgeGroupValidator`
* `CreateTeamValidator`
* `SendAnnouncementValidator`
* `UserBusinessValidator` (injected directly in workflows via `IUserBusinessValidator`)

### Unit Test Projects
* **`Koralytics.Application.UnitTests`**: xUnit test project covering Application layer logic.
  * `PlayerCardCalculatorTests` — unit tests for all `PlayerCardCalculator` static methods
  * `PlayerCardServiceTests` — integration-style tests for `PlayerCardService` using mocked `IUnitOfWork`
  * `PlayerProfileServiceTests` — comprehensive tests for `PlayerProfileService` using mocked dependencies
* **`Koralytics.API.UnitTests`**: xUnit test project for API layer (currently a placeholder — `Class1.cs` only).

---

## 6. Active Controllers
* **`ApiBaseController`**: Base controller providing shared API logic and standardized responses.
* **`AuthController`**: Handles login (including Google OAuth), registration for varied roles, profile completion, and token delivery. *(Replaced LoginController and RegisterController)*
* **`PlayerController`**: Main interface for player operations (transfers, profiling, card).
* **`TournamentController`**: Interface for tournament-related endpoints (create, draw, fixtures, bracket).
* **`CoachController`**: Interface for coach operations — squad overview, training team split, player comparison, writing/fetching player notes, and granting/revoking/listing temporary squad access.
* **`ScouterController`**: Interface for scouter operations — filtered player search, shortlist management (add/remove/get), follow/unfollow/log-view, and AI scouting report generation & verification.
* **`DrillsController`**: Interface for all drill operations — template CRUD, session management, bulk result logging, drill analytics (squad weak categories, coach bias). JWT claims extracted from token per request.
* **`AcademyController`**: Manages academy creation, update, and location management.
* **`AcademyTeamController`**: Manages age group and team creation, coach assignment/removal.
* **`AcademyAnnouncementController`**: Sends announcements and removes players from academy.
* **`AcademyAnalyticsController`**: Returns coach performance dashboard and subscription status.

---

## 7. Rules for Updating This Document
For any AI agent or Developer working on this codebase:
1. **Adding a Table/Entity:** You MUST append the entity name to the **Deep Dive: Domain Entities** section under the appropriate category.
2. **Adding an API/Controller:** Add it to the **Active Controllers** list.
3. **Adding a Service:** Document its purpose and key methods under the relevant team member section in **Section 3**.
4. **Altering Configuration:** If you change JWT config, Logging, Error Handling, AutoMapper profiles, or FluentValidation in `Program.cs`, update the **API & Infrastructure Setup** section.
5. **New External Dependency:** Document it under **Cross-Cutting Concerns** in the API & Infrastructure Setup section.
