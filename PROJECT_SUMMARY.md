# Koralytics - Ultimate Project Knowledge Base & Summary

> **CRITICAL DIRECTIVE FOR AI/DEVELOPERS:** This document is the definitive source of truth for the entire Koralytics platform. It contains an exhaustive breakdown of the architecture, database schema, domain models, services, and configuration. **This file MUST be updated with every architectural change, new entity, or new core service.**

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

Here is the exhaustive list of DbSets and Domain Entities currently mapped in the system:

### 🏆 Core Match & Player (Faissal's Entities)
- **Players**: `Player` (Mapped to "Players"), `PlayerAcademy`, `PlayerTeam`
- **Matches**: `Match`, `MatchEvent`, `MatchLineup`, `MatchPlayerRating`
- **Tournaments (Core)**: `Tournament`, `TournamentFixture`, `TournamentGroup`

### 🏟️ Tournament Logic & Core Academy (Adham's Entities)
- **Tournament Extensions**: `TournamentTeam`, `TournamentGroupTeam`, `TournamentStanding`, `TournamentRound`, `TournamentSquad`, `TournamentHallOfFame`
- **Academy Basics**: `Academy`, `AgeGroup`

### ⚙️ Academy Settings & Super Admin (Aly's Entities)
- **Academy Management**: `AcademyAdmin` (Mapped to "AcademyAdmins"), `Team`, `AcademyLocation`, `AcademyAnnouncement`, `AcademyBadge`, `AcademyRequest`
- **Administration**: `SystemAdminUser` (Mapped to "SystemAdmins"), `RoleAuditLog`
- **Parents**: `Parent` (Mapped to "Parents"), `ParentPlayer`

### 🏋️ Drills & Platform Settings (Bishoy's Entities)
- **Drills Management**: `DrillCategory`, `DrillTemplate`, `DrillSession`, `Drill`, `SessionAttendance`, `DrillResult`
- **Platform Management**: `PlatformSettings`, `PlatformAuditLog`

### 📈 Player Progression & AI (Rawan's Entities)
- **Player Metrics**: `PlayerSubscription`, `PlayerGoal`, `PlayerAchievement`
- **Artificial Intelligence**: `AIReport`

### 🕵️‍♂️ Staff, Scouting & Media (Youssef's Entities)
- **Coach Management**: `Coach` (Mapped to "Coaches"), `CoachAcademy`, `CoachTeam`, `CoachNote`, `CoachTempAccess`
- **Scouting**: `Scouter` (Mapped to "Scouters"), `ScouterShortlist`, `ScouterFollow`, `ScouterReport`, `ScouterView`
- **Player Media/Details**: `PlayerHighlight`, `PlayerPosition`

---

## 3. Application Layer (Services, DTOs, Validation)
The application layer serves as the orchestrator.

### Core Services Implemented:
* **`IAuthService` / `AuthService`**: Handles User login, JWT token generation, and authentication logic.
* **`IRegistrationService` / `RegistrationService`**: Handles the creation of new users across different roles.
* **`IPlayerTransferService` / `PlayerTransferService`**: Domain logic for transferring a player between academies/teams.
* **Player Profile Services**: Specific logics handling the player's profile operations.

### Validation (`FluentValidation`)
Validation is automatically enforced using `.AddFluentValidationAutoValidation()`.
* `LoginRequestValidator`
* `BaseRegisterationRequestValidator`
* `ChangePasswordValidator`
* `UserBusinessValidator` (Custom business rule enforcement injected directly in workflows)

### Mapping (`AutoMapper`)
* Profiles are registered globally (e.g., `RegisterProfile` for mapping Registration DTOs to Domain Entities).

---

## 4. API & Infrastructure Setup
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

---

## 5. Active Controllers
* **`LoginController`**: Handles user login and token delivery.
* **`RegisterController`**: Handles varied role registrations.
* **`PlayerController`**: Main interface for player operations (like transfers and profiling).

---

## 📝 Rules for Updating This Document
For any AI agent or Developer working on this codebase:
1. **Adding a Table/Entity:** You MUST append the entity name to the **Deep Dive: Domain Entities** section under the appropriate category.
2. **Adding an API/Controller:** Add it to the **Active Controllers** list.
3. **Adding a Service:** Document its purpose under **Application Layer -> Core Services Implemented**.
4. **Altering Configuration:** If you change JWT config, Logging, or Error Handling in `Program.cs`, update the **API & Infrastructure Setup** section.
