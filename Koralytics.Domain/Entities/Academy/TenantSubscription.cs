using Koralytics.Domain.Entities.Identity;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Models.BaseModels;

namespace Koralytics.Domain.Entities.Academy
{
    /// <summary>
    /// Represents the active SaaS subscription tier for an academy.
    /// One active row per academy (enforced by a unique index on AcademyId in EF config).
    /// Phases 2–6 read the Tier from this entity to enforce capacity and feature gates.
    /// </summary>
    public class TenantSubscription : AuditableEntity
    {
        // ── Foreign Key ──────────────────────────────────────────────────────────
        public int AcademyId { get; set; }
        public Academy Academy { get; set; } = null!;

        // ── Tier & Billing Status ────────────────────────────────────────────────
        /// <summary>Starter | Pro | Elite</summary>
        public SubscriptionTier Tier { get; set; } = SubscriptionTier.Starter;

        /// <summary>Paid | Unpaid | Grace — controls whether feature gates are active.</summary>
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Paid;

        // ── Date Range ───────────────────────────────────────────────────────────
        public DateTime StartsAt  { get; set; }
        public DateTime ExpiresAt { get; set; }

        // ── Computed Helper (no DB column) ───────────────────────────────────────
        /// <summary>
        /// True when the subscription is paid AND within its active date window.
        /// Gates should check this before enforcing tier limits.
        /// </summary>
        public bool IsActive =>
            Status == SubscriptionStatus.Paid &&
            DateTime.UtcNow >= StartsAt &&
            DateTime.UtcNow <= ExpiresAt;
    }
}
