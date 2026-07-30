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

        /// <summary>
        /// Retrieves the list of children (players) linked to the logged-in parent.
        /// </summary>
        [HttpGet("my-children")]
        [Authorize(Roles = "Parent,SystemAdmin")]
        public async Task<IActionResult> GetMyChildren()
        {
            var parentUserId = GetCurrentUserId();
            var children = await _parentService.GetMyChildrenAsync(parentUserId);
            return OkResponse(children, "Children retrieved successfully.");
        }

        /// <summary>
        /// Searches available players by name to link to the logged-in parent.
        /// </summary>
        [HttpGet("search-players")]
        [Authorize(Roles = "Parent,SystemAdmin")]
        public async Task<IActionResult> SearchPlayers([FromQuery] string? name)
        {
            var parentUserId = GetCurrentUserId();
            var results = await _parentService.SearchAvailablePlayersAsync(name, parentUserId);
            return OkResponse(results, "Available players retrieved successfully.");
        }

        /// <summary>
        /// Sends a join request from the logged-in parent to a child player.
        /// </summary>
        [HttpPost("child-requests")]
        [Authorize(Roles = "Parent,SystemAdmin")]
        public async Task<IActionResult> SendChildRequest([FromQuery] int playerId)
        {
            var parentUserId = GetCurrentUserId();
            await _parentService.SendChildJoinRequestAsync(parentUserId, playerId);
            return OkResponse(new { }, "Join request sent successfully.");
        }

        /// <summary>
        /// Retrieves pending join requests sent by the logged-in parent.
        /// </summary>
        [HttpGet("my-pending-requests")]
        [Authorize(Roles = "Parent,SystemAdmin")]
        public async Task<IActionResult> GetMyPendingRequests()
        {
            var parentUserId = GetCurrentUserId();
            var requests = await _parentService.GetPendingRequestsForParentAsync(parentUserId);
            return OkResponse(requests, "Pending requests retrieved successfully.");
        }

        /// <summary>
        /// Cancels a pending join request sent by the logged-in parent.
        /// </summary>
        [HttpPatch("child-requests/{requestId}/cancel")]
        [Authorize(Roles = "Parent,SystemAdmin")]
        public async Task<IActionResult> CancelChildRequest(int requestId)
        {
            var parentUserId = GetCurrentUserId();
            await _parentService.CancelChildJoinRequestAsync(requestId, parentUserId);
            return OkResponse(new { }, "Join request cancelled successfully.");
        }

        /// <summary>
        /// Unlinks a child player from the logged-in parent's account.
        /// </summary>
        [HttpDelete("children/{playerId}")]
        [Authorize(Roles = "Parent,SystemAdmin")]
        public async Task<IActionResult> UnlinkChild(int playerId)
        {
            var parentUserId = GetCurrentUserId();
            await _parentService.UnlinkChildAsync(parentUserId, playerId);
            return OkResponse(new { }, "Child unlinked successfully.");
        }

        /// <summary>
        /// Retrieves pending parent join requests received by the logged-in player.
        /// </summary>
        [HttpGet("player-pending-requests")]
        [Authorize(Roles = "Player,SystemAdmin")]
        public async Task<IActionResult> GetPlayerPendingRequests()
        {
            var playerUserId = GetCurrentUserId();
            var requests = await _parentService.GetPendingRequestsForPlayerAsync(playerUserId);
            return OkResponse(requests, "Player pending requests retrieved successfully.");
        }

        /// <summary>
        /// Allows a player to respond (Accept/Reject) to a parent join request.
        /// </summary>
        [HttpPut("child-requests/{requestId}/respond")]
        [Authorize(Roles = "Player,SystemAdmin")]
        public async Task<IActionResult> RespondToChildRequest(int requestId, [FromBody] RespondParentJoinRequestDto dto)
        {
            var playerUserId = GetCurrentUserId();
            await _parentService.RespondToChildJoinRequestAsync(requestId, dto.Status, playerUserId);
            return OkResponse(new { }, "Responded to join request successfully.");
        }

        /// <summary>
        /// Retrieves linked parents/guardians for the logged-in player.
        /// </summary>
        [HttpGet("my-parents")]
        [Authorize(Roles = "Player,SystemAdmin")]
        public async Task<IActionResult> GetMyParents()
        {
            var playerUserId = GetCurrentUserId();
            var parents = await _parentService.GetMyParentsAsync(playerUserId);
            return OkResponse(parents, "Linked parents retrieved successfully.");
        }

        /// <summary>
        /// Allows a player to unlink a parent/guardian from their account.
        /// </summary>
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
