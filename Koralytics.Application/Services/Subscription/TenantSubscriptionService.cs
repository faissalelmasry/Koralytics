using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Subscription;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Coach;
using Koralytics.Domain.Entities.Drill;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Enums;
using Koralytics.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Koralytics.Application.Services.Subscription
{
    /// <summary>
    /// Reads the active <see cref="TenantSubscription"/> for an academy and exposes
    /// tier limits via <see cref="SubscriptionTierPolicy"/>.
    /// </summary>
    public class TenantSubscriptionService : ITenantSubscriptionService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMemoryCache _cache; // 🟢 OPTIMIZATION: Injected memory cache

        public TenantSubscriptionService(IUnitOfWork uow, IMemoryCache cache)
        {
            _uow = uow;
            _cache = cache;
        }

        // ── Tier resolution ───────────────────────────────────────────────────────

        /// <inheritdoc />
        public async Task<TenantSubscription?> GetActiveSubscriptionAsync(
            int academyId, CancellationToken ct = default)
        {
            // 🟢 FIX: Order by ExpiresAt descending so we always get the latest/most recent
            // subscription row. Without this, an old expired row could be returned first,
            // causing tier resolution to fall back to Starter and incorrectly blocking
            // Pro/Elite users from adding coaches, locations, etc.
            return await _uow
                .Repository<TenantSubscription>()
                .GetQueryableAsNoTracking()
                .Where(s => s.AcademyId == academyId)
                .OrderByDescending(s => s.ExpiresAt)
                .FirstOrDefaultAsync(ct);
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
            // 🟢 OPTIMIZATION: Check RAM first. Prevents spamming the SQL database 
            // on every single API request that passes through the Action Filters.
            string cacheKey = $"AcademyTier_{academyId}";

            if (_cache.TryGetValue(cacheKey, out SubscriptionTier cachedTier))
            {
                return cachedTier;
            }

            var subscription = await GetActiveSubscriptionAsync(academyId, ct);

            // Graceful fallback — treat missing or expired subscriptions as Starter
            SubscriptionTier resolvedTier = (subscription is null || !subscription.IsActive)
                ? SubscriptionTier.Starter
                : subscription.Tier;

            // Save to cache for 10 minutes
            _cache.Set(cacheKey, resolvedTier, TimeSpan.FromMinutes(10));

            return resolvedTier;
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