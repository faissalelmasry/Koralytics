using System;
using Koralytics.Domain.Models.BaseModels;

namespace Koralytics.Domain.Entities.Coach
{
    public class CoachAcademy : BaseEntity
    {
        public int CoachUserId { get; set; }
        public int AcademyId { get; set; }
        public DateTime JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }
        public decimal? BiasScore { get; set; }
        public DateTime? BiasLastCalculatedAt { get; set; }

        public Coach Coach { get; set; } = null!;
        public Academy.Academy Academy { get; set; } = null!;
    }
}
