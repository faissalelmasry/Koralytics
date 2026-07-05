using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.AI;
using Koralytics.Domain.Entities.Coach;
using Koralytics.Domain.Entities.Drill;
using Koralytics.Domain.Entities.Match;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Entities.Scouter;
using Koralytics.Domain.Entities.SystemAdmin;
using Koralytics.Domain.Entities.Tournamet;
using Koralytics.Domain.Entities;
using Koralytics.Infrastructure.Extensions; 
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Koralytics.Domain.Entities.Identity;
using Koralytics.Domain.Entities.Parents;

namespace Koralytics.Infrastructure.Context
{
    public class ApplicationDbContext : IdentityDbContext<User, Role, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        #region Faissal's Entities (Core Player, Match, Tournament)
        public DbSet<Player> Players { get; set; }
        public DbSet<PlayerAcademy> PlayerAcademies { get; set; }
        public DbSet<PlayerTeam> PlayerTeams { get; set; }

        public DbSet<Match> Matches { get; set; }
        public DbSet<MatchEvent> MatchEvents { get; set; }
        public DbSet<MatchLineup> MatchLineups { get; set; }
        public DbSet<MatchPlayerRating> MatchPlayerRatings { get; set; }

        public DbSet<Tournament> Tournaments { get; set; }
        public DbSet<TournamentFixture> TournamentFixtures { get; set; }
        public DbSet<TournamentGroup> TournamentGroups { get; set; }
        #endregion

        #region Adham's Entities (Tournament Logic & Core Academy)
        public DbSet<TournamentTeam> TournamentTeams { get; set; }
        public DbSet<TournamentGroupTeam> TournamentGroupTeams { get; set; }
        public DbSet<TournamentStanding> TournamentStandings { get; set; }
        public DbSet<TournamentRound> TournamentRounds { get; set; }
        public DbSet<TournamentSquad> TournamentSquads { get; set; }
        public DbSet<TournamentHallOfFame> TournamentHallOfFames { get; set; }

        public DbSet<Academy> Academies { get; set; }
        public DbSet<AgeGroup> AgeGroups { get; set; }
        #endregion

        #region Aly's Entities (Academy Settings & SuperAdmin)
        public DbSet<AcademyAdmin> AcademyAdmins { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<AcademyLocation> AcademyLocations { get; set; }
        public DbSet<AcademyAnnouncement> AcademyAnnouncements { get; set; }
        public DbSet<AcademyBadge> AcademyBadges { get; set; }
        public DbSet<RoleAuditLog> RoleAuditLogs { get; set; }

        public DbSet<SystemAdminUser> SuperAdmins { get; set; }
        public DbSet<AcademyRequest> AcademyRequests { get; set; }
        
        public DbSet<Parent> Parents { get; set; }
        public DbSet<ParentPlayer> ParentPlayers { get; set; }
        #endregion

        #region Bishoy's Entities (Drills & Platform Settings)
        public DbSet<DrillCategory> DrillCategories { get; set; }
        public DbSet<DrillTemplate> DrillTemplates { get; set; }
        public DbSet<DrillSession> DrillSessions { get; set; }
        public DbSet<Drill> Drills { get; set; }
        public DbSet<SessionAttendance> SessionAttendances { get; set; }
        public DbSet<DrillResult> DrillResults { get; set; }

        public DbSet<PlatformSettings> PlatformSettings { get; set; }
        public DbSet<PlatformAuditLog> PlatformAuditLogs { get; set; }
        #endregion

        #region Rawan's Entities (Player Progression & AI)
        public DbSet<PlayerSubscription> PlayerSubscriptions { get; set; }
        public DbSet<PlayerGoal> PlayerGoals { get; set; }
        public DbSet<PlayerAchievement> PlayerAchievements { get; set; }

        public DbSet<AIReport> AIReports { get; set; }
        #endregion

        #region Youssef's Entities (Coach, Scouter & Player Media)
        public DbSet<Coach> Coaches { get; set; }
        public DbSet<CoachAcademy> CoachAcademies { get; set; }
        public DbSet<CoachTeam> CoachTeams { get; set; }
        public DbSet<CoachNote> CoachNotes { get; set; }
        public DbSet<CoachTempAccess> CoachTempAccesses { get; set; }

        public DbSet<Scouter> Scouters { get; set; }
        public DbSet<ScouterShortlist> ScouterShortlists { get; set; }
        public DbSet<ScouterFollow> ScouterFollows { get; set; }
        public DbSet<ScouterReport> ScouterReports { get; set; }

        public DbSet<PlayerHighlight> PlayerHighlights { get; set; }
        public DbSet<PlayerPosition> PlayerPositions { get; set; }
        public DbSet<ScouterView> ScouterViews { get; set; }
        #endregion


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
            builder.Entity<SystemAdminUser>().ToTable("SystemAdmins");
            builder.Entity<Player>().ToTable("Players");
            builder.Entity<Coach>().ToTable("Coaches");
            builder.Entity<Scouter>().ToTable("Scouters");
            builder.Entity<Parent>().ToTable("Parents");
            builder.Entity<AcademyAdmin>().ToTable("AcademyAdmins");
            builder.ApplyGlobalQueryFilters();
        }
    }
}