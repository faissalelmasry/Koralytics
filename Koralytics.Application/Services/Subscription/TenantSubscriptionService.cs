using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Subscription;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Coach;
using Koralytics.Domain.Entities.Drill;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Enums;
using Koralytics.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Koralytics.Application.Services.Subscription
{
    /// <summary>
    /// Reads the active <see cref="TenantSubscription"/> for an academy and exposes
    /// tier limits via <see cref="SubscriptionTierPolicy"/>.
    ///
    /// All Phase 2–6 action filters inject this service to gate feature access.
    /// Falls back to <see cref="SubscriptionTier.Starter"/> when no subscription row exists,
    /// so a misconfigured academy is always in the most restrictive state.
    /// </summary>
    public class TenantSubscriptionService : ITenantSubscriptionService
    {
        private readonly IUnitOfWork _uow;

        public TenantSubscriptionService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ── Tier resolution ───────────────────────────────────────────────────────

        /// <inheritdoc />
        public async Task<TenantSubscription?> GetActiveSubscriptionAsync(
            int academyId, CancellationToken ct = default)
        {
            return await _uow
                .Repository<TenantSubscription>()
                .GetQueryableAsNoTracking()
                .FirstOrDefaultAsync(s => s.AcademyId == academyId, ct);
        }

        /// <inheritdoc />
        public async Task<TierLimits> GetLimitsAsync(
            int academyId, CancellationToken ct = default)
        {
            var tier = await GetTierAsync(academyId, ct);
            return SubscriptionTierPolicy.GetLimits(tier);
        }

        /// <inheritdoc />
        public async Task<SubscriptionTier> GetTierAsync(
            int academyId, CancellationToken ct = default)
        {
            var subscription = await GetActiveSubscriptionAsync(academyId, ct);

            // Graceful fallback — treat missing or expired subscriptions as Starter
            if (subscription is null || !subscription.IsActive)
                return SubscriptionTier.Starter;

            return subscription.Tier;
        }

        // ── Capacity counters (Phase 2) ───────────────────────────────────────────

        /// <inheritdoc />
        public async Task<int> CountLocationsAsync(int academyId, CancellationToken ct = default)
        {
            return await _uow
                .Repository<AcademyLocation>()
                .GetQueryableAsNoTracking()
                .CountAsync(l => l.AcademyId == academyId, ct);
        }

        /// <inheritdoc />
        public async Task<int> CountPlayersAsync(int academyId, CancellationToken ct = default)
        {
            // Active players = those with no LeftAt date in the PlayerAcademy junction table
            return await _uow
                .Repository<PlayerAcademy>()
                .GetQueryableAsNoTracking()
                .CountAsync(pa => pa.AcademyId == academyId && pa.LeftAt == null, ct);
        }

        /// <inheritdoc />
        public async Task<int> CountSeatsAsync(int academyId, CancellationToken ct = default)
        {
            // Seats = active coaches + extra admins (AcademyAdmin junction rows)
            // The founding AcademyAdmin (AdminUserId on Academy) is NOT counted against the seat limit.
            var coachCount = await _uow
                .Repository<CoachAcademy>()
                .GetQueryableAsNoTracking()
                .CountAsync(ca => ca.AcademyId == academyId && ca.LeftAt == null, ct);

            var adminCount = await _uow
                .Repository<AcademyAdmin>()
                .GetQueryableAsNoTracking()
                .CountAsync(a => a.AcademyId == academyId, ct);

            return coachCount + adminCount;
        }

        // ── Drill counters (Phase 3) ──────────────────────────────────────────────

        /// <inheritdoc />
        public async Task<int> CountCustomDrillTemplatesAsync(int academyId, CancellationToken ct = default)
        {
            // Custom = academy-owned (AcademyId != null); Global/system templates have AcademyId == null
            return await _uow
                .Repository<DrillTemplate>()
                .GetQueryableAsNoTracking()
                .CountAsync(t => t.AcademyId == academyId, ct);
        }
    }
}
