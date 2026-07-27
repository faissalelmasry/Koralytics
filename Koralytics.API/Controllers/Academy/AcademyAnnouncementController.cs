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
    [Route("api/Academy")]
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
        [Authorize(Roles = "AcademyAdmin,Coach")]
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
        [Authorize(Roles = "AcademyAdmin,Coach,Player,Parent")]
        public async Task<IActionResult> GetAnnouncements(int academyId, [FromQuery] Koralytics.Application.DTOs.Common.PaginationRequestDto request)
        {
            var result = await _academyAnnouncementService.GetAnnouncementsAsync(academyId, request);
            return OkResponse(result, "Announcements retrieved successfully.");
        }

    }
}
