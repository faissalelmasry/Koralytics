namespace Koralytics.Application.DTOs.Subscription
{
    /// <summary>
    /// Returned as the body of HTTP 403 responses when a tenant exceeds a
    /// capacity limit or tries to access a feature locked to a higher tier.
    /// Angular reads this to render the appropriate upgrade prompt.
    /// </summary>
    public record PlanLimitExceededDto(
        /// <summary>Human-readable name of the feature/resource being blocked (e.g. "Locations").</summary>
        string Feature,

        /// <summary>The maximum the current plan allows (-1 means unlimited).</summary>
        int Limit,

        /// <summary>How many the academy currently has.</summary>
        int Current,

        /// <summary>The academy's current tier name (e.g. "Starter").</summary>
        string CurrentPlan,

        /// <summary>The minimum tier that unlocks this feature (e.g. "Pro").</summary>
        string RequiredPlan,

        /// <summary>Human-readable upgrade call-to-action shown in the Angular modal.</summary>
        string UpgradeMessage
    );
}
