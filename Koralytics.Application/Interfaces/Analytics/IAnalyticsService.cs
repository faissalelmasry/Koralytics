using Koralytics.Application.DTOs.Analytics;

namespace Koralytics.Application.Interfaces.Analytics
{
    /// <summary>
    /// Contract for analytics operations, including AI-powered natural language player search.
    /// </summary>
    public interface IAnalyticsService
    {
        /// <summary>
        /// Sends a natural-language query to the Langflow AI pipeline and returns
        /// a human-readable answer with player data extracted from the database.
        /// </summary>
        /// <param name="request">The search query from the scouter.</param>
        /// <returns>A conversational response with the query results.</returns>
        Task<AiSearchResponseDto> AiPlayerSearchAsync(AiSearchRequestDto request);
    }
}
