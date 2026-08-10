using System;
using Koralytics.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection; // 🟢 Required for GetRequiredService

namespace Koralytics.API.Filters
{
    /// <summary>
    /// Decorates an endpoint to mandate that the requesting academy's active
    /// subscription plan includes the specified feature. Used with <see cref="PlanFeatureFilter"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequiresPlanFeatureAttribute : Attribute, IFilterFactory
    {
        public TierFeature Feature { get; }

        // 🟢 ARCHITECTURAL GUARDRAIL: Must remain 'false'.
        // If true, the filter becomes a Singleton and creates a "Captive Dependency", 
        // trapping the Scoped IUnitOfWork in memory and crashing the application.
        public bool IsReusable => false;

        public RequiresPlanFeatureAttribute(TierFeature feature)
        {
            Feature = feature;
        }

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            var filter = serviceProvider.GetRequiredService<PlanFeatureFilter>();
            filter.RequiredFeature = Feature;

            return filter;
        }
    }
}