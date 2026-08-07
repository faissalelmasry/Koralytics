namespace Koralytics.Domain.Enums
{
    /// <summary>
    /// Represents all boolean-gated features in the SaaS platform.
    /// Used by the [RequiresPlanFeature] attribute (Phases 4–6).
    /// </summary>
    public enum TierFeature
    {
        // ── Phase 4: Analytics ──
        ProgressionAnalytics,
        SquadWeakness,
        TransferRate,
        FullAnalyticsSuite,

        // ── Phase 5: AI Insights ──
        AIInsights,
        ArchetypeReveal,

        // ── Phase 6: Payments ──
        StripePayments,

        // ── Other Features ──
        AcademyComparison
    }
}
