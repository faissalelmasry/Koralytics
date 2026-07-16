using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Models.BaseModels;

namespace Koralytics.Domain.Entities.Academy
{
    public class AcademyPlayerJoinRequest : AuditableEntity
    {
        public int AcademyId { get; set; }
        public int PlayerId { get; set; }
        public JoinRequestStatus Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? RespondedAt { get; set; }

        // Navigation Properties
        public Academy Academy { get; set; } = null!;
        public Player.Player Player { get; set; } = null!;
    }
}
