using Koralytics.Application.DTOs.Subscription;
using Koralytics.Application.Interfaces.Subscription;
using Koralytics.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Koralytics.API.Filters
{
    /// <summary>
    /// Action filter that resolves the user's AcademyId, looks up their subscription tier limits,
    /// and blocks the request with a 403 Forbidden (PlanLimitExceededDto) if the required
    /// boolean feature flag is disabled for their tier.
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
                // Or we can safely block if we assume these endpoints ONLY apply to academy users.
                // We'll let it pass here and assume standard auth catches it if they shouldn't be here.
                await next();
                return;
            }

            var limits = await _tenantSubscriptionService.GetLimitsAsync(academyId);
            var tier = await _tenantSubscriptionService.GetTierAsync(academyId);

            bool isAllowed = RequiredFeature switch
            {
                TierFeature.ProgressionAnalytics => limits.AllowProgressionAnalytics,
                TierFeature.SquadWeakness        => limits.AllowSquadWeakness,
                TierFeature.TransferRate         => limits.AllowTransferRate,
                TierFeature.FullAnalyticsSuite   => limits.AllowFullAnalyticsSuite,
                TierFeature.AIInsights           => limits.AllowAIInsights,
                TierFeature.StripePayments       => limits.AllowStripe,
                TierFeature.AcademyComparison    => limits.AllowAcademyComparison,
                TierFeature.ArchetypeReveal      => limits.AllowArchetypeReveal,
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
                TierFeature.ArchetypeReveal => "Pro",
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
