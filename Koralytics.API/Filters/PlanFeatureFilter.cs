using Koralytics.Application.DTOs.Subscription;
using Koralytics.Application.Interfaces.Subscription;
using Koralytics.Domain.Enums;
using Koralytics.Domain.ValueObjects; // Required for SubscriptionTierPolicy
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Koralytics.API.Filters
{
    /// <summary>
    /// Action filter that resolves the user's AcademyId, looks up their subscription tier limits,
    /// and blocks the request with a 403 Forbidden if the required feature flag is disabled.
    /// </summary>
    public class PlanFeatureFilter : IAsyncActionFilter
    {
        private readonly ITenantSubscriptionService _tenantSubscriptionService;
        public TierFeature RequiredFeature { get; set; } // Set by the Attribute factory

        public PlanFeatureFilter(ITenantSubscriptionService tenantSubscriptionService)
        {
            _tenantSubscriptionService = tenantSubscriptionService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            var academyIdStr = user.FindFirstValue("AcademyId") ?? user.FindFirstValue("academyId");

            if (!int.TryParse(academyIdStr, out var academyId))
            {
                // Unauthenticated or not tied to an academy. Let the endpoint handle standard logic or 401s.
                await next();
                return;
            }

            // 🟢 OPTIMIZATION: Grab the request's cancellation token
            var ct = context.HttpContext.RequestAborted;

            // 🟢 OPTIMIZATION: Await once (which hits our fast MemoryCache), resolve limits synchronously.
            var tier = await _tenantSubscriptionService.GetTierAsync(academyId, ct);
            var limits = SubscriptionTierPolicy.GetLimits(tier);

            bool isAllowed = RequiredFeature switch
            {
                TierFeature.ProgressionAnalytics => limits.AllowProgressionAnalytics,
                TierFeature.SquadWeakness => limits.AllowSquadWeakness,
                TierFeature.TransferRate => limits.AllowTransferRate,
                TierFeature.FullAnalyticsSuite => limits.AllowFullAnalyticsSuite,
                TierFeature.AIInsights => limits.AllowAIInsights,
                TierFeature.StripePayments => limits.AllowStripe,
                TierFeature.AcademyComparison => limits.AllowAcademyComparison,
                TierFeature.ArchetypeReveal => limits.AllowArchetypeReveal,
                _ => false
            };

            if (!isAllowed)
            {
                // Construct standard 403 payload
                var requiredTierName = GetRequiredTierNameForFeature(RequiredFeature);
                var humanReadableFeatureName = GetHumanReadableFeatureName(RequiredFeature);

                var dto = new PlanLimitExceededDto(
                    Feature: humanReadableFeatureName,
                    Limit: 1, // Boolean features use 1 for "True"
                    Current: 0, // 0 for "False"
                    CurrentPlan: tier.ToString(),
                    RequiredPlan: requiredTierName,
                    UpgradeMessage: $"Upgrade to {requiredTierName} to unlock {humanReadableFeatureName}."
                );

                context.Result = new ObjectResult(dto) { StatusCode = StatusCodes.Status403Forbidden };
                return;
            }

            await next();
        }

        private static string GetRequiredTierNameForFeature(TierFeature feature)
        {
            return feature switch
            {
                TierFeature.ProgressionAnalytics => "Pro",
                TierFeature.SquadWeakness => "Pro",
                TierFeature.TransferRate => "Pro",
                TierFeature.FullAnalyticsSuite => "Elite",
                TierFeature.AIInsights => "Elite",
                TierFeature.StripePayments => "Pro",
                TierFeature.AcademyComparison => "Pro",
                TierFeature.ArchetypeReveal => "Elite",
                _ => "Elite"
            };
        }

        private static string GetHumanReadableFeatureName(TierFeature feature)
        {
            return feature switch
            {
                TierFeature.ProgressionAnalytics => "Player Progression Analytics",
                TierFeature.SquadWeakness => "Squad Weakness Analytics",
                TierFeature.TransferRate => "Drill to Match Transfer Rate",
                TierFeature.FullAnalyticsSuite => "Advanced Analytics Suite",
                TierFeature.AIInsights => "AI Insights & Reports",
                TierFeature.StripePayments => "Stripe Payment Processing",
                TierFeature.AcademyComparison => "Academy Comparison",
                TierFeature.ArchetypeReveal => "Player Archetype Reveal",
                _ => feature.ToString()
            };
        }
    }
}