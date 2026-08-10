using System.Threading.Tasks;
using Koralytics.API.Controllers.BaseController;
using Koralytics.Application.DTOs.Parent;
using Koralytics.Application.Services.Parent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koralytics.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ParentController : ApiBaseController
    {
        private readonly IParentService _parentService;

        public ParentController(IParentService parentService)
        {
            _parentService = parentService;
        }

        [HttpGet("my-children")]
        [Authorize(Roles = "Parent,SystemAdmin")]
        public async Task<IActionResult> GetMyChildren()
        {
            var parentUserId = GetCurrentUserId();
            var children = await _parentService.GetMyChildrenAsync(parentUserId);
            return OkResponse(children, "Children retrieved successfully.");
        }

        [HttpGet("search-players")]
        [Authorize(Roles = "Parent,SystemAdmin")]
        public async Task<IActionResult> SearchPlayers([FromQuery] string? name)
        {
            var parentUserId = GetCurrentUserId();
            var results = await _parentService.SearchAvailablePlayersAsync(name, parentUserId);
            return OkResponse(results, "Available players retrieved successfully.");
        }

        // 🟢 OPTIMIZED: RESTful routing. Moved identifier to the path.
        [HttpPost("children/{playerId}/requests")]
        [Authorize(Roles = "Parent,SystemAdmin")]
        public async Task<IActionResult> SendChildRequest(int playerId)
        {
            var parentUserId = GetCurrentUserId();
            await _parentService.SendChildJoinRequestAsync(parentUserId, playerId);
            return OkResponse(new { }, "Join request sent successfully.");
        }

        [HttpGet("my-pending-requests")]
        [Authorize(Roles = "Parent,SystemAdmin")]
        public async Task<IActionResult> GetMyPendingRequests()
        {
            var parentUserId = GetCurrentUserId();
            var requests = await _parentService.GetPendingRequestsForParentAsync(parentUserId);
            return OkResponse(requests, "Pending requests retrieved successfully.");
        }

        [HttpPatch("child-requests/{requestId}/cancel")]
        [Authorize(Roles = "Parent,SystemAdmin")]
        public async Task<IActionResult> CancelChildRequest(int requestId)
        {
            var parentUserId = GetCurrentUserId();
            await _parentService.CancelChildJoinRequestAsync(requestId, parentUserId);
            return OkResponse(new { }, "Join request cancelled successfully.");
        }

        [HttpDelete("children/{playerId}")]
        [Authorize(Roles = "Parent,SystemAdmin")]
        public async Task<IActionResult> UnlinkChild(int playerId)
        {
            var parentUserId = GetCurrentUserId();
            await _parentService.UnlinkChildAsync(parentUserId, playerId);
            return OkResponse(new { }, "Child unlinked successfully.");
        }

        [HttpGet("player-pending-requests")]
        [Authorize(Roles = "Player,SystemAdmin")]
        public async Task<IActionResult> GetPlayerPendingRequests()
        {
            var playerUserId = GetCurrentUserId();
            var requests = await _parentService.GetPendingRequestsForPlayerAsync(playerUserId);
            return OkResponse(requests, "Player pending requests retrieved successfully.");
        }

        [HttpPut("child-requests/{requestId}/respond")]
        [Authorize(Roles = "Player,SystemAdmin")]
        public async Task<IActionResult> RespondToChildRequest(int requestId, [FromBody] RespondParentJoinRequestDto dto)
        {
            var playerUserId = GetCurrentUserId();
            await _parentService.RespondToChildJoinRequestAsync(requestId, dto.Status, playerUserId);
            return OkResponse(new { }, "Responded to join request successfully.");
        }

        [HttpGet("my-parents")]
        [Authorize(Roles = "Player,SystemAdmin")]
        public async Task<IActionResult> GetMyParents()
        {
            var playerUserId = GetCurrentUserId();
            var parents = await _parentService.GetMyParentsAsync(playerUserId);
            return OkResponse(parents, "Linked parents retrieved successfully.");
        }

        [HttpDelete("parents/{parentId}")]
        [Authorize(Roles = "Player,SystemAdmin")]
        public async Task<IActionResult> UnlinkParent(int parentId)
        {
            var playerUserId = GetCurrentUserId();
            await _parentService.UnlinkParentAsync(playerUserId, parentId);
            return OkResponse(new { }, "Parent unlinked successfully.");
        }
    }
}