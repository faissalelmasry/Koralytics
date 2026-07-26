using Koralytics.Domain.Enums;
using Koralytics.Domain.Models.BaseModels;

namespace Koralytics.Domain.Entities.Academy
{
    public class AcademyAdminJoinRequest : AuditableEntity
    {
        public int AcademyId { get; set; }
        public int AdminId { get; set; }
        public JoinRequestStatus Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? RespondedAt { get; set; }

        // Navigation Properties
        public Academy Academy { get; set; } = null!;
        public AcademyAdmin Admin { get; set; } = null!;
    }
}
