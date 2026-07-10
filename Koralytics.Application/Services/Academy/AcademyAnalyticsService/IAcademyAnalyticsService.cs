using Koralytics.Application.DTOs.Academies;

namespace Koralytics.Application.Services.Academy.AcademyAnalyticsService
{
    public interface IAcademyAnalyticsService
    {
        /// <summary>
        /// Returns a ranked list of coaches in the academy sorted by player improvement rate (desc).
        /// For each coach:
        ///   - Fetches all active players in their teams.
        ///   - Calculates average player improvement rate from PlayerCard data.
        ///   - Fetches BiasScore from CoachAcademy record.
        ///     (BiasScore population is owned by the AI/Analytics Module — TODO)
        /// </summary>
        Task<IEnumerable<CoachPerformanceDto>> GetCoachPerformanceDashboardAsync(int academyId);

        /// <summary>
        /// Returns subscription status summary for all players enrolled in the academy.
        /// Groups by Status (Paid, Unpaid, Grace).
        /// Includes the list of unpaid/grace players with grace expiry info.
        /// </summary>
        Task<SubscriptionStatusSummaryDto> GetSubscriptionStatusAsync(int academyId);
    }
}
