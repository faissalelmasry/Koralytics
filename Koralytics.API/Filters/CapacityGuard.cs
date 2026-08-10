using Koralytics.Application.DTOs.Subscription;
using Koralytics.Application.Interfaces.Subscription;
using Koralytics.Domain.Enums;
using Koralytics.Domain.ValueObjects;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Koralytics.API.Filters
{
    /// <summary>
    /// Reusable action filter for Phase 2 capacity checks.
    /// Acts as the SaaS Bouncer to enforce subscription tier limits.
    /// </summary>
    public static class CapacityGuard
    {
        // ─────────────────────────────────────────────────────────────────────────
        // Helper: resolve AcademyId from JWT
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
        // 🟢 OPTIMIZATION: Centralized Limit Evaluator
        // Eliminates code duplication and calculates upgrade strings exactly once.
        // ─────────────────────────────────────────────────────────────────────────
        private static ObjectResult? EvaluateLimit(
            int currentCount,
            int maxLimit,
            string featureName,
            SubscriptionTier currentTier)
        {
            // If unlimited, or under the limit, let them pass
            if (maxLimit == int.MaxValue || currentCount < maxLimit)
                return null;

            string currentPlanName = currentTier.ToString();
            string nextPlanName = currentTier == SubscriptionTier.Starter ? "Pro" : "Elite";

            // Adjust the verb for better grammar in the UI message
            string actionVerb = featureName == "Staff Seats" ? "invite" : featureName == "Custom Drill Templates" ? "create" : "add";

            return Forbidden(
                feature: featureName,
                limit: maxLimit,
                current: currentCount,
                currentPlan: currentPlanName,
                requiredPlan: nextPlanName,
                upgradeMessage: $"Upgrade to {nextPlanName} to {actionVerb} more {featureName.ToLower()}."
            );
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Capacity Checks
        // ─────────────────────────────────────────────────────────────────────────

        public static async Task<ObjectResult?> CheckLocationLimitAsync(
            ITenantSubscriptionService svc, int academyId, int currentCount, CancellationToken ct = default)
        {
            // 🟢 OPTIMIZATION: Await the tier once, get the limits synchronously.
            var tier = await svc.GetTierAsync(academyId, ct);
            var limits = SubscriptionTierPolicy.GetLimits(tier);

            return EvaluateLimit(currentCount, limits.MaxLocations, "Locations", tier);
        }

        public static async Task<ObjectResult?> CheckPlayerLimitAsync(
            ITenantSubscriptionService svc, int academyId, int currentCount, CancellationToken ct = default)
        {
            var tier = await svc.GetTierAsync(academyId, ct);
            var limits = SubscriptionTierPolicy.GetLimits(tier);

            return EvaluateLimit(currentCount, limits.MaxPlayers, "Players", tier);
        }

        public static async Task<ObjectResult?> CheckSeatLimitAsync(
            ITenantSubscriptionService svc, int academyId, int currentCount, CancellationToken ct = default)
        {
            var tier = await svc.GetTierAsync(academyId, ct);
            var limits = SubscriptionTierPolicy.GetLimits(tier);

            return EvaluateLimit(currentCount, limits.MaxSeats, "Staff Seats", tier);
        }

        public static async Task<ObjectResult?> CheckCustomDrillTemplateLimitAsync(
            ITenantSubscriptionService svc, int academyId, int currentCount, CancellationToken ct = default)
        {
            var tier = await svc.GetTierAsync(academyId, ct);
            var limits = SubscriptionTierPolicy.GetLimits(tier);

            return EvaluateLimit(currentCount, limits.MaxCustomDrillTemplates, "Custom Drill Templates", tier);
        }
    }
}