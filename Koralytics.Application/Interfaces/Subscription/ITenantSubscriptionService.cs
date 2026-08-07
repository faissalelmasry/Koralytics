using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Enums;
using Koralytics.Domain.ValueObjects;

namespace Koralytics.Application.Interfaces.Subscription
{
    /// <summary>
    /// Service for reading the active SaaS tier of an academy and counting
    /// current usage against capacity limits (Phases 2–6).
    /// </summary>
    public interface ITenantSubscriptionService
    {
        // ── Tier resolution ──────────────────────────────────────────────────────

        /// <summary>Returns the active <see cref="TenantSubscription"/> for the academy, or null.</summary>
        Task<TenantSubscription?> GetActiveSubscriptionAsync(int academyId, CancellationToken ct = default);

        /// <summary>
        /// Returns the <see cref="TierLimits"/> for the academy's current active subscription.
        /// Falls back to Starter limits when no active subscription is found.
        /// </summary>
        Task<TierLimits> GetLimitsAsync(int academyId, CancellationToken ct = default);

        /// <summary>
        /// Returns the <see cref="SubscriptionTier"/> for the academy.
        /// Falls back to <see cref="SubscriptionTier.Starter"/> when no active subscription is found.
        /// </summary>
        Task<SubscriptionTier> GetTierAsync(int academyId, CancellationToken ct = default);

        // ── Capacity counters (Phase 2) ──────────────────────────────────────────

        /// <summary>Returns the number of active (non-deleted) locations for the academy.</summary>
        Task<int> CountLocationsAsync(int academyId, CancellationToken ct = default);

        /// <summary>Returns the number of active players currently enrolled in the academy.</summary>
        Task<int> CountPlayersAsync(int academyId, CancellationToken ct = default);

        /// <summary>
        /// Returns the total number of staff seats consumed (Coaches + extra Admins).
        /// The founding AcademyAdmin is not counted against the seat limit.
        /// </summary>
        Task<int> CountSeatsAsync(int academyId, CancellationToken ct = default);

        // ── Drill counters (Phase 3) ─────────────────────────────────────────────

        /// <summary>Returns the number of custom (academy-owned) drill templates.</summary>
        Task<int> CountCustomDrillTemplatesAsync(int academyId, CancellationToken ct = default);
    }
}
