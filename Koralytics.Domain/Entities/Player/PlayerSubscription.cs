using Koralytics.Domain.Entities.Identity;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Models.BaseModels;
using System.ComponentModel.DataAnnotations.Schema;

namespace Koralytics.Domain.Entities.Player
{
    public class PlayerSubscription : BaseEntity
    {
        public int PlayerId { get; set; }
        public int AcademyId { get; set; }
        public int? PaidByUserId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public SubscriptionStatus Status { get; set; }
        public SubscriptionDuration Duration { get; set; } = SubscriptionDuration.OneMonth;

        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime DueDate { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? GraceUntil { get; set; }

        public virtual Player Player { get; set; } = null!;
        public virtual Academy.Academy Academy { get; set; } = null!;
        public virtual User? PaidByUser { get; set; }

        // 🟢 Helper method on the Domain Entity itself
        public void SetBillingCycle(DateTime startDate, SubscriptionDuration duration, int graceDays = 5)
        {
            StartDate = startDate;
            Duration = duration;
            DueDate = duration switch
            {
                SubscriptionDuration.OneMonth => startDate.AddMonths(1),
                SubscriptionDuration.ThreeMonths => startDate.AddMonths(3),
                SubscriptionDuration.SixMonths => startDate.AddMonths(6),
                SubscriptionDuration.OneYear => startDate.AddYears(1),
                _ => startDate.AddMonths(1)
            };
            GraceUntil = DueDate.AddDays(graceDays);
        }
    }
}