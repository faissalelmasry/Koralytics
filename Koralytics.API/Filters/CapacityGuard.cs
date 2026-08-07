using Koralytics.Application.DTOs.Subscription;
using Koralytics.Application.Interfaces.Subscription;
using Koralytics.Domain.ValueObjects;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using System.Security.Claims;

namespace Koralytics.API.Filters
{
    /// <summary>
    /// Reusable action filter for Phase 2 capacity checks.
    /// Inject via constructor — caller sets the check delegate via <see cref="CapacityCheck"/>.
    /// Usage in a controller action:
    /// <code>
    ///   var guard = new CapacityGuard(_tenantSvc, context.HttpContext, academyId);
    ///   if (await guard.CheckLocationLimitAsync()) return guard.Result!;
    /// </code>
    /// Alternatively, use the static helper methods directly from controllers.
    /// </summary>
    public static class CapacityGuard
    {
        // ─────────────────────────────────────────────────────────────────────────
        // Helper: resolve AcademyId from JWT (same as DrillsController pattern)
        // ─────────────────────────────────────────────────────────────────────────
        public static int? ResolveAcademyId(ClaimsPrincipal user)
        {
            var str = user.FindFirstValue("AcademyId") ?? user.FindFirstValue("academyId");
            return int.TryParse(str, out var id) ? id : null;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Build the standardised 403 ObjectResult
        // ─────────────────────────────────────────────────────────────────────────
        public static ObjectResult Forbidden(
            string feature,
            int limit,
            int current,
            string currentPlan,
            string requiredPlan,
            string upgradeMessage)
        {
            var dto = new PlanLimitExceededDto(
                Feature: feature,
                Limit: limit,
                Current: current,
                CurrentPlan: currentPlan,
                RequiredPlan: requiredPlan,
                UpgradeMessage: upgradeMessage
            );

            return new ObjectResult(dto) { StatusCode = StatusCodes.Status403Forbidden };
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Capacity: Locations
        // ─────────────────────────────────────────────────────────────────────────
        public static async Task<ObjectResult?> CheckLocationLimitAsync(
            ITenantSubscriptionService svc,
            int academyId,
            int currentCount,
            CancellationToken ct = default)
        {
            var limits    = await svc.GetLimitsAsync(academyId, ct);
            var tier      = await svc.GetTierAsync(academyId, ct);

            if (limits.MaxLocations == int.MaxValue || currentCount < limits.MaxLocations)
                return null; // allowed

            return Forbidden(
                feature       : "Locations",
                limit         : limits.MaxLocations,
                current       : currentCount,
                currentPlan   : tier.ToString(),
                requiredPlan  : tier == Domain.Enums.SubscriptionTier.Starter ? "Pro" : "Elite",
                upgradeMessage: $"Upgrade to {(tier == Domain.Enums.SubscriptionTier.Starter ? "Pro" : "Elite")} to add more locations."
            );
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Capacity: Players
        // ─────────────────────────────────────────────────────────────────────────
        public static async Task<ObjectResult?> CheckPlayerLimitAsync(
            ITenantSubscriptionService svc,
            int academyId,
            int currentCount,
            CancellationToken ct = default)
        {
            var limits = await svc.GetLimitsAsync(academyId, ct);
            var tier   = await svc.GetTierAsync(academyId, ct);

            if (limits.MaxPlayers == int.MaxValue || currentCount < limits.MaxPlayers)
                return null;

            return Forbidden(
                feature       : "Players",
                limit         : limits.MaxPlayers,
                current       : currentCount,
                currentPlan   : tier.ToString(),
                requiredPlan  : tier == Domain.Enums.SubscriptionTier.Starter ? "Pro" : "Elite",
                upgradeMessage: $"Upgrade to {(tier == Domain.Enums.SubscriptionTier.Starter ? "Pro" : "Elite")} to add more players."
            );
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Capacity: Seats (Coaches + Admins combined)
        // ─────────────────────────────────────────────────────────────────────────
        public static async Task<ObjectResult?> CheckSeatLimitAsync(
            ITenantSubscriptionService svc,
            int academyId,
            int currentCount,
            CancellationToken ct = default)
        {
            var limits = await svc.GetLimitsAsync(academyId, ct);
            var tier   = await svc.GetTierAsync(academyId, ct);

            if (limits.MaxSeats == int.MaxValue || currentCount < limits.MaxSeats)
                return null;

            return Forbidden(
                feature       : "Staff Seats",
                limit         : limits.MaxSeats,
                current       : currentCount,
                currentPlan   : tier.ToString(),
                requiredPlan  : tier == Domain.Enums.SubscriptionTier.Starter ? "Pro" : "Elite",
                upgradeMessage: $"Upgrade to {(tier == Domain.Enums.SubscriptionTier.Starter ? "Pro" : "Elite")} to invite more coaches and admins."
            );
        }
        // ─────────────────────────────────────────────────────────────────────────
        // Capacity: Custom Drill Templates
        // ─────────────────────────────────────────────────────────────────────────
        public static async Task<ObjectResult?> CheckCustomDrillTemplateLimitAsync(
            ITenantSubscriptionService svc,
            int academyId,
            int currentCount,
            CancellationToken ct = default)
        {
            var limits = await svc.GetLimitsAsync(academyId, ct);
            var tier   = await svc.GetTierAsync(academyId, ct);

            if (limits.MaxCustomDrillTemplates == int.MaxValue || currentCount < limits.MaxCustomDrillTemplates)
                return null;

            return Forbidden(
                feature       : "Custom Drill Templates",
                limit         : limits.MaxCustomDrillTemplates,
                current       : currentCount,
                currentPlan   : tier.ToString(),
                requiredPlan  : tier == Domain.Enums.SubscriptionTier.Starter ? "Pro" : "Elite",
                upgradeMessage: $"Upgrade to {(tier == Domain.Enums.SubscriptionTier.Starter ? "Pro" : "Elite")} to create more custom drill templates."
            );
        }
    }
}
