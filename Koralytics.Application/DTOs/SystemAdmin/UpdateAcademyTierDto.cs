using Koralytics.Domain.Enums;

namespace Koralytics.Application.DTOs.SystemAdmin
{
    public class UpdateAcademyTierDto
    {
        public SubscriptionTier Tier { get; set; }
        public SubscriptionStatus? Status { get; set; }
    }
}
