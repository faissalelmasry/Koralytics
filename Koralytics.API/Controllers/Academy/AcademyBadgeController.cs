using Koralytics.API.Controllers.BaseController;
using Koralytics.Application.DTOs.Academies;
using Koralytics.Application.Services.Academy.AcademyBadgeService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Koralytics.API.Controllers.Academies
{
    [ApiController]
    [Route("api/Academy/{academyId}/badges")]
    [Authorize]
    [Produces("application/json")]
    public class AcademyBadgeController : ApiBaseController
    {
        private readonly IAcademyBadgeService _badgeService;

        public AcademyBadgeController(IAcademyBadgeService badgeService)
        {
            _badgeService = badgeService;
        }

        [HttpPost]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> CreateBadge(int academyId, [FromBody] CreateAcademyBadgeDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _badgeService.CreateBadgeAsync(academyId, dto, userId);
            
            return CreatedResponse(
                result, 
                nameof(GetBadges), 
                new { academyId }, 
                "Badge created successfully.");
        }

        [HttpGet]
        [Authorize(Roles = "AcademyAdmin,SystemAdmin,Coach,Player")]
        public async Task<IActionResult> GetBadges(int academyId)
        {
            var result = await _badgeService.GetBadgesByAcademyAsync(academyId);
            return OkResponse(result, "Badges retrieved successfully.");
        }

        [HttpDelete("{badgeId}")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> DeleteBadge(int academyId, int badgeId)
        {
            var userId = GetCurrentUserId();
            await _badgeService.DeleteBadgeAsync(academyId, badgeId, userId);
            return NoContentResponse("Badge deleted successfully.");
        }

    }
}
