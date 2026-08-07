using Koralytics.Domain.Enums;

namespace Koralytics.Domain.ValueObjects
{
    /// <summary>
    /// Immutable record capturing every numeric and boolean limit for a subscription tier.
    /// Used by Phases 2–6 filters — no magic numbers scattered across the codebase.
    /// </summary>
    public record TierLimits(
        // ── Capacity ────────────────────────────────────────────────────────────
        int MaxLocations,
        int MaxPlayers,
        int MaxSeats,

        // ── Drills ──────────────────────────────────────────────────────────────
        int MaxCustomDrillTemplates,

        // ── Payments ────────────────────────────────────────────────────────────
        bool AllowStripe,

        // ── AI Insights ─────────────────────────────────────────────────────────
        bool AllowAIInsights,

        // ── Analytics ───────────────────────────────────────────────────────────
        bool AllowProgressionAnalytics,
        bool AllowSquadWeakness,
        bool AllowTransferRate,
        bool AllowFullAnalyticsSuite,
        bool AllowAcademyComparison,
        bool AllowArchetypeReveal
    );

    /// <summary>
    /// Static policy table that maps each <see cref="SubscriptionTier"/> to its
    /// <see cref="TierLimits"/>. This is the single source of truth for the
    /// subscription spec matrix — update here and all gates update automatically.
    /// </summary>
    public static class SubscriptionTierPolicy
    {
        /// <summary>
        /// Returns the feature limits for the given tier.
        /// Throws <see cref="ArgumentOutOfRangeException"/> for unknown tier values.
        /// </summary>
        public static TierLimits GetLimits(SubscriptionTier tier) => tier switch
        {
            SubscriptionTier.Starter => new TierLimits(
                MaxLocations              : 1,
                MaxPlayers                : 50,
                MaxSeats                  : 3,
                MaxCustomDrillTemplates   : 7,
                AllowStripe               : false,
                AllowAIInsights           : false,
                AllowProgressionAnalytics : false,
                AllowSquadWeakness        : false,
                AllowTransferRate         : false,
                AllowFullAnalyticsSuite   : false,
                AllowAcademyComparison    : false,
                AllowArchetypeReveal      : false
            ),

            SubscriptionTier.Pro => new TierLimits(
                MaxLocations              : 3,
                MaxPlayers                : 200,
                MaxSeats                  : 10,
                MaxCustomDrillTemplates   : 30,
                AllowStripe               : true,
                AllowAIInsights           : false,
                AllowProgressionAnalytics : true,
                AllowSquadWeakness        : true,
                AllowTransferRate         : true,
                AllowFullAnalyticsSuite   : false,
                AllowAcademyComparison    : true,
                AllowArchetypeReveal      : true
            ),

            SubscriptionTier.Elite => new TierLimits(
                MaxLocations              : int.MaxValue,
                MaxPlayers                : int.MaxValue,
                MaxSeats                  : int.MaxValue,
                MaxCustomDrillTemplates   : int.MaxValue,
                AllowStripe               : true,
                AllowAIInsights           : true,
                AllowProgressionAnalytics : true,
                AllowSquadWeakness        : true,
                AllowTransferRate         : true,
                AllowFullAnalyticsSuite   : true,
                AllowAcademyComparison    : true,
                AllowArchetypeReveal      : true
            ),

            _ => throw new ArgumentOutOfRangeException(nameof(tier), tier,
                     $"No policy defined for SubscriptionTier '{tier}'.")
        };
    }
}
