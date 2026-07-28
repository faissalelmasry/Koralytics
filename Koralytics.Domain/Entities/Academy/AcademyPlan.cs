
using Koralytics.Domain.Enums;
using Koralytics.Domain.Models.BaseModels;

namespace Koralytics.Domain.Entities
{
    public class AcademyPlan : AuditableEntity
    {
        public string Name { get; set; } = "Standard Monthly Plan"; // e.g., "Monthly Basic", "Pro Annual"

        public decimal Amount { get; set; } = 1500.00m; // Fee in EGP

        public SubscriptionDuration Duration { get; set; } = SubscriptionDuration.OneMonth;

        public int GracePeriodDays { get; set; } = 7; // Safety window after due date

        public bool IsDefault { get; set; } = true; // Used for Auto-Subscription on registration!

        // Foreign Key to Academy
        public int AcademyId { get; set; }

        public Koralytics.Domain.Entities.Academy.Academy Academy { get; set; } = null!;
    }
}