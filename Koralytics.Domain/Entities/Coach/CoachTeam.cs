using System;
using Koralytics.Domain.Models.BaseModels;
using Koralytics.Domain.Entities.Academy;

namespace Koralytics.Domain.Entities.Coach
{
    public class CoachTeam : BaseEntity
    {
        public int CoachUserId { get; set; }
        public int TeamId { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? RemovedAt { get; set; }

        public Coach Coach { get; set; } = null!;
        public Team Team { get; set; } = null!;
    }
}
