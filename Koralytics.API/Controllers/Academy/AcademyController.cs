using Koralytics.API.Controllers.BaseController;
using Koralytics.Application.DTOs.Academies;
using Koralytics.Application.DTOs.Academy;
using Koralytics.Application.DTOs.SystemAdmin;
using Koralytics.Application.Services.Academy.AcademyService;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;

namespace Koralytics.API.Controllers.Academies
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class AcademyController : ApiBaseController
    {
        private readonly IAcademyService _academyService;

        public AcademyController(IAcademyService academyService)
        {
            _academyService = academyService;
        }

        [HttpPost("approve")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> ApproveAcademy([FromBody] CreateAcademyDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _academyService.ApproveAcademyAsync(dto, userId);
            
            return CreatedResponse(
                result, 
                nameof(GetAcademy), 
                new { academyId = result.Id }, 
                "Academy created successfully.");
        }

        [HttpGet]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> GetAllAcademies([FromQuery] AcademyListRequestDto request)
        {
            var result = await _academyService.GetAllAcademiesAsync(request);
            return OkResponse(result, "Academies retrieved successfully.");
        }


        [HttpGet("{academyId}")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> GetAcademy(int academyId)
        {
            var result = await _academyService.GetAcademyAsync(academyId);
            return OkResponse(result, "Academy retrieved successfully.");
        }

        [HttpPut("{academyId}")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> UpdateAcademy(int academyId, [FromBody] UpdateAcademyDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _academyService.UpdateAcademyAsync(academyId, dto, userId);
            return OkResponse(result, "Academy updated successfully.");
        }

        [HttpPost("{academyId}/locations")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> AddLocation(int academyId, [FromBody] AddLocationDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _academyService.AddLocationAsync(academyId, dto, userId);
            return OkResponse(result, "Location added successfully.");
        }

        [HttpGet("{academyId}/locations")]
        [Authorize(Roles = "Admin,SystemAdmin,Coach")]
        public async Task<IActionResult> GetLocations(int academyId)
        {
            var result = await _academyService.GetLocationsAsync(academyId);
            return OkResponse(result, "Locations retrieved successfully.");
        }

        [HttpPut("{academyId}/locations/{locationId}/set-main")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> SetMainLocation(int academyId, int locationId)
        {
            var userId = GetCurrentUserId();
            await _academyService.SetMainLocationAsync(academyId, locationId, userId);
            return NoContentResponse("Location set as main successfully.");
        }

        // ─── Academy Requests ──────────────────────────────────────────────────

        [HttpPost("requests")]
        // [Authorize]  (Assuming any authenticated user can request an academy)
        public async Task<IActionResult> RequestAcademy([FromBody] CreateAcademyRequestDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _academyService.RequestAcademyAsync(dto, userId);
            
            return OkResponse(
                result, 
                "Academy request submitted successfully.");
        }

        [HttpGet("requests/pending")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var result = await _academyService.GetPendingRequestsAsync();
            return OkResponse(result, "Pending academy requests retrieved successfully.");
        }

        [HttpPut("requests/{requestId}/reject")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> RejectAcademyRequest(int requestId, [FromBody] RejectAcademyRequestDto dto)
        {
            var userId = GetCurrentUserId();
            await _academyService.RejectAcademyRequestAsync(requestId, dto, userId);
            return NoContentResponse("Academy request rejected successfully.");
        }

        // ─── Member Registration ───────────────────────────────────────────────

        // ─── Player Join Requests ──────────────────────────────────────────────

        [HttpGet("{academyId}/search-players")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> SearchPlayers(int academyId, [FromQuery] string? name)
        {
            var result = await _academyService.SearchAvailablePlayersAsync(name, academyId);
            return OkResponse(result, "Available players retrieved successfully.");
        }

        [HttpPost("{academyId}/Send-player-join-request")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> SendPlayerJoinRequest(int academyId, [FromQuery] int playerId)
        {
            var adminId = GetCurrentUserId();
            await _academyService.SendPlayerJoinRequestAsync(academyId, playerId, adminId);
            return OkResponse<object>(null, "Join request sent to player successfully.");
        }

        [HttpGet("{academyId}/Pending-player-requests")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> GetPendingPlayerJoinRequestsForAcademy(int academyId)
        {
            var result = await _academyService.GetPendingPlayerRequestsForAcademyAsync(academyId);
            return OkResponse(result, "Pending player join requests retrieved successfully.");
        }

        [HttpPut("player-join-requests/{requestId}/respond")]
        [Authorize(Roles = "Player")]
        public async Task<IActionResult> RespondToPlayerJoinRequest(int requestId, [FromBody] RespondJoinRequestDto dto)
        {
            var playerId = GetCurrentUserId();
            await _academyService.RespondToPlayerJoinRequestAsync(requestId, dto.Status, playerId);
            return OkResponse<object>(null, "Responded to join request successfully.");
        }

        [HttpPatch("player-join-requests/{requestId}/cancel")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> CancelPlayerJoinRequest(int requestId)
        {
            var adminId = GetCurrentUserId();
            await _academyService.CancelPlayerJoinRequestAsync(requestId, adminId);
            return OkResponse<object>(null, "Join request cancelled successfully.");
        }

        [HttpGet("player-join-requests/my-requests")]
        [Authorize(Roles = "Player")]
        public async Task<IActionResult> GetMyPendingPlayerRequests()
        {
            var playerId = GetCurrentUserId();
            var result = await _academyService.GetPendingPlayerRequestsForUserAsync(playerId);
            return OkResponse(result, "Your pending join requests retrieved successfully.");
        }

        // ─── Coach Join Requests ───────────────────────────────────────────────

        [HttpGet("{academyId}/search-coaches")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> SearchCoaches(int academyId, [FromQuery] string? name)
        {
            var result = await _academyService.SearchCoachesAsync(name, academyId);
            return OkResponse(result, "Available coaches retrieved successfully.");
        }

        [HttpPost("{academyId}/Send-coach-join-request")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> SendCoachJoinRequest(int academyId, [FromQuery] int coachId)
        {
            var adminId = GetCurrentUserId();
            await _academyService.SendCoachJoinRequestAsync(academyId, coachId, adminId);
            return OkResponse<object>(null, "Join request sent to coach successfully.");
        }

        [HttpGet("{academyId}/Pending-coach-requests")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> GetPendingCoachJoinRequestsForAcademy(int academyId)
        {
            var result = await _academyService.GetPendingCoachRequestsForAcademyAsync(academyId);
            return OkResponse(result, "Pending coach join requests retrieved successfully.");
        }

        [HttpPut("coach-join-requests/{requestId}/Respond")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> RespondToCoachJoinRequest(int requestId, [FromBody] RespondJoinRequestDto dto)
        {
            var coachId = GetCurrentUserId();
            await _academyService.RespondToCoachJoinRequestAsync(requestId, dto.Status, coachId);
            return OkResponse<object>(null, "Responded to join request successfully.");
        }

        [HttpPatch("coach-join-requests/{requestId}/cancel")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> CancelCoachJoinRequest(int requestId)
        {
            var adminId = GetCurrentUserId();
            await _academyService.CancelCoachJoinRequestAsync(requestId, adminId);
            return OkResponse<object>(null, "Join request cancelled successfully.");
        }

        [HttpGet("coach-join-requests/my-requests")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetMyPendingCoachRequests()
        {
            var coachId = GetCurrentUserId();
            var result = await _academyService.GetPendingCoachRequestsForUserAsync(coachId);
            return OkResponse(result, "Your pending join requests retrieved successfully.");
        }

        [HttpPost("{academyId}/admins/{adminId}")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> AssignAdmin(int academyId, int adminId)
        {
            var userId = GetCurrentUserId();
            await _academyService.AssignAdminToAcademyAsync(academyId, adminId, userId);
            return NoContentResponse("Admin assigned to academy successfully.");
        }

        [HttpDelete("{academyId}/admins/{adminId}")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> RemoveAdmin(int academyId, int adminId)
        {
            var userId = GetCurrentUserId();
            await _academyService.RemoveAdminFromAcademyAsync(academyId, adminId, userId);
            return NoContentResponse("Admin removed from academy successfully.");
        }

        [HttpDelete("{academyId}/coaches/{coachId}")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> RemoveCoach(int academyId, int coachId)
        {
            var userId = GetCurrentUserId();
            await _academyService.RemoveCoachFromAcademyAsync(academyId, coachId, userId);
            return NoContentResponse("Coach removed from academy successfully.");
        }

        [HttpDelete("{academyId}/players/{playerId}")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> RemovePlayer(int academyId, int playerId)
        {
            var userId = GetCurrentUserId();
            await _academyService.RemovePlayerFromAcademyAsync(academyId, playerId, userId);
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
