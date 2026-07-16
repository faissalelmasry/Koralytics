using Koralytics.Domain.Entities.Coach;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Models.BaseModels;

namespace Koralytics.Domain.Entities.Academy
{
    public class AcademyCoachJoinRequest : AuditableEntity
    {
        public int AcademyId { get; set; }
        public int CoachId { get; set; }
        public JoinRequestStatus Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? RespondedAt { get; set; }

        // Navigation Properties
        public Academy Academy { get; set; } = null!;
        public Coach.Coach Coach { get; set; } = null!;
    }
}
