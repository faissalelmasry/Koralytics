using System;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Models.BaseModels;

namespace Koralytics.Domain.Entities.Parents
{
    public class ParentPlayerJoinRequest : AuditableEntity
    {
        public int ParentId { get; set; }
        public int PlayerId { get; set; }
        public JoinRequestStatus Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? RespondedAt { get; set; }

        // Navigation Properties
        public Parent Parent { get; set; } = null!;
        public Player.Player Player { get; set; } = null!;
    }
}
