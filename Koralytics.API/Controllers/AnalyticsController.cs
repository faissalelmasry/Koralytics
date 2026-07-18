using Koralytics.API.Controllers.BaseController;
using Koralytics.Application.DTOs.Analytics;
using Koralytics.Application.Interfaces.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koralytics.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticsController : ApiBaseController
    {
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        /// <summary>
        /// AI Player Search — sends a natural-language query to the Langflow pipeline
        /// and returns a human-readable answer with player data from the database.
        /// </summary>
        /// <param name="request">The search query (e.g. "show me left-footed strikers with a rating above 7").</param>
        /// <returns>A conversational AI-generated response.</returns>
        [HttpPost("ai-search")]
        [Authorize(Roles = "Scouter,SystemAdmin")]
        public async Task<IActionResult> AiPlayerSearch([FromBody] AiSearchRequestDto request)
        {
            var result = await _analyticsService.AiPlayerSearchAsync(request);
            return OkResponse(result, "AI search completed successfully.");
        }
    }
}
