using Koralytics.API.Controllers.BaseController;
using Koralytics.Application.DTOs.Academies;
using Koralytics.Application.DTOs.Notification;
using Koralytics.Application.Services.Academy.AcademyAnnouncementService;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;

namespace Koralytics.API.Controllers.Academies
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class AcademyAnnouncementController : ApiBaseController
    {
        private readonly IAcademyAnnouncementService _academyAnnouncementService;

        public AcademyAnnouncementController(IAcademyAnnouncementService academyAnnouncementService)
        {
            _academyAnnouncementService = academyAnnouncementService;
        }

        [HttpPost("{academyId}/announcements")]
        //[Authorize(Roles = "Admin,Coach")]
        public async Task<IActionResult> SendAnnouncement(int academyId, [FromBody] CreateAnnouncementDto dto)
        {
            var userId = GetCurrentUserId();

            var isSystemAdmin = User.IsInRole("SystemAdmin");

            await _academyAnnouncementService.SendAnnouncementAsync(
                academyId,
                dto,
                 userId,
                isSystemAdmin: isSystemAdmin
            );

            return OkResponse("Announcement sent successfully.");
        }

        [HttpGet("{academyId}/announcements")]
        //[Authorize(Roles = "Admin,Coach,Player")]
        public async Task<IActionResult> GetAnnouncements(int academyId)
        {
            var result = await _academyAnnouncementService.GetAnnouncementsAsync(academyId);
            return OkResponse(result, "Announcements retrieved successfully.");
        }

        [HttpDelete("{academyId}/players/{playerId}")]
        //[Authorize(Roles = "Coach")]
        public async Task<IActionResult> RemovePlayer(int academyId, int playerId, [FromQuery] string reason = "Unpaid subscription")
        {
            var userId = GetCurrentUserId();
            await _academyAnnouncementService.RemovePlayerAsync(academyId, playerId, userId, reason);
            return NoContentResponse("Player removed from academy successfully.");
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user token");
            }
            return userId;
        }
    }
}
