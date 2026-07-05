using Koralytics.Domain.Entities.Identity;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Models.BaseModels;

namespace Koralytics.Domain.Entities.Academy
{
    public class Academy : AuditableEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string? PrimaryColor { get; set; }
        public string? SecondaryColor { get; set; }
        public DateTime FoundedAt { get; set; }
        public AcademyStatus Status { get; set; }
        public int AdminUserId { get; set; }

        // Navigation Properties
        public User Admin { get; set; } = null!;

        public ICollection<AgeGroup> AgeGroups { get; set; } = [];
        public ICollection<AcademyLocation> AcademyLocations { get; set; } = [];
        public ICollection<AcademyAnnouncement> AcademyAnnouncements { get; set; } = [];
        public ICollection<AcademyBadge> AcademyBadges { get; set; } = [];
        public ICollection<RoleAuditLog> RoleAuditLogs { get; set; } = [];

    }
}