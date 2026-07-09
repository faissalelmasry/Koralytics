using Koralytics.API.Controllers.BaseController;
using Koralytics.Application.Services.Academy.AcademyAnalyticsService;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koralytics.API.Controllers.Academies
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class AcademyAnalyticsController : ApiBaseController
    {
        private readonly IAcademyAnalyticsService _academyAnalyticsService;

        public AcademyAnalyticsController(IAcademyAnalyticsService academyAnalyticsService)
        {
            _academyAnalyticsService = academyAnalyticsService;
        }

        [HttpGet("{academyId}/coaches")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetCoachPerformance(int academyId)
        {
            var result = await _academyAnalyticsService.GetCoachPerformanceDashboardAsync(academyId);
            return OkResponse(result, "Coach performance dashboard retrieved successfully.");
        }

        [HttpGet("{academyId}/subscriptions")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSubscriptionStatus(int academyId)
        {
            var result = await _academyAnalyticsService.GetSubscriptionStatusAsync(academyId);
            return OkResponse(result, "Subscription status summary retrieved successfully.");
        }
    }
}
