using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Koralytics.Application.DTOs.Notification;
using Koralytics.Application.Interfaces.Notification;

namespace Koralytics.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly IAnnouncementNotificationService _announcementService;
        private readonly IPlayerNotificationService _playerNotificationService;
        private readonly IScouterNotificationService _scouterNotificationService;
        private readonly IMatchNotificationService _matchNotificationService;
        private readonly IRealTimeBridge _realTimeBridge;

        public NotificationController(
           IAnnouncementNotificationService announcementService,
           IPlayerNotificationService playerNotificationService,
           IScouterNotificationService scouterNotificationService,
           IMatchNotificationService matchNotificationService,
           IRealTimeBridge realTimeBridge)
        {
            _announcementService = announcementService;
            _playerNotificationService = playerNotificationService;
            _scouterNotificationService = scouterNotificationService;
            _matchNotificationService = matchNotificationService;
            _realTimeBridge = realTimeBridge;
        }

        private bool TryGetRequester(out int requesterId, out string requesterRole, out IActionResult? errorResult)
        {
            requesterId = 0;
            requesterRole = string.Empty;
            errorResult = null;

            var idClaim = User.FindFirst("id")?.Value
                       ?? User.FindFirst("sub")?.Value
                       ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            var roleClaim = User.FindFirst("role")?.Value
                         ?? User.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out requesterId))
            {
                errorResult = Unauthorized(new { message = "Unable to resolve caller identity from the provided credentials." });
                return false;
            }

            
            if (string.IsNullOrEmpty(roleClaim))
            {
                errorResult = Unauthorized(new { message = "Unable to resolve caller role. Token is missing role claim." });
                return false;
            }

            requesterRole = roleClaim;
            return true;
        }

        #region 1. User Notification Management (Active Feed & Reading Status)

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyNotifications([FromQuery] int skip = 0, [FromQuery] int take = 50)
        {
            if (!TryGetRequester(out var currentUserId, out _, out var authError))
                return authError!;

            var notifications = await _realTimeBridge.GetNotificationsAsync(currentUserId, skip, take, HttpContext.RequestAborted);
            return Ok(notifications);
        }

        [HttpPatch("{notificationId}/read")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> MarkAsRead([FromRoute] string notificationId)
        {
            if (!TryGetRequester(out var currentUserId, out _, out var authError))
                return authError!;

            await _realTimeBridge.MarkAsReadAsync(currentUserId, notificationId, HttpContext.RequestAborted);
            return NoContent();
        }

        [HttpDelete("expired")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> PurgeExpiredNotifications()
        {
            if (!TryGetRequester(out var currentUserId, out _, out var authError))
                return authError!;

            await _realTimeBridge.DeleteExpiredNotificationsAsync(currentUserId, HttpContext.RequestAborted);
            return Ok(new { message = "Expired notifications purged successfully." });
        }

        #endregion

        #region 2. Academy Announcements

        [HttpPost("academies/{academyId:int}/announcements")]
        [Authorize(Roles = "Coach,SystemAdmin")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SendAcademyAnnouncement([FromRoute] int academyId, [FromBody] CreateAnnouncementDto body)
        {
            if (!TryGetRequester(out var currentUserId, out _, out var authError))
                return authError!;

            var isSystemAdmin = User.IsInRole("SystemAdmin");

            await _announcementService.SendAnnouncementNotificationAsync(
                academyId,
                currentUserId,
                body,
                isSystemAdmin: isSystemAdmin,
                cancellationToken: HttpContext.RequestAborted);

            return Accepted();
        }

        #endregion

        #region 3. Player & Parent Engagement

        [HttpPost("players/{playerId:int}/milestone")]
        [Authorize(Roles = "Coach,SystemAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> NotifyPlayerMilestone([FromRoute] int playerId, [FromQuery] string achievementType)
        {
            await _playerNotificationService.NotifyPlayerMilestoneAsync(playerId, achievementType, HttpContext.RequestAborted);
            return Ok(new { message = "Milestone notification dispatched successfully." });
        }

        [HttpPost("players/{playerId:int}/parent-alert")]
        [Authorize(Roles = "Coach,SystemAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> NotifyPlayerParents([FromRoute] int playerId, [FromQuery] string eventType)
        {
            await _playerNotificationService.NotifyParentAsync(playerId, eventType, HttpContext.RequestAborted);
            return Ok(new { message = "Parent notifications dispatched successfully." });
        }

        [HttpPost("players/{playerId:int}/academies/{academyId:int}/subscription-grace")]
        [Authorize(Roles = "Coach,SystemAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> TriggerSubscriptionGraceNotification([FromRoute] int playerId, [FromRoute] int academyId)
        {
            await _playerNotificationService.NotifySubscriptionGraceAsync(playerId, academyId, HttpContext.RequestAborted);
            return Ok(new { message = "Subscription grace period alerts successfully broadcasted." });
        }

        /// <summary>
        /// Notifies the academy administration that a player has successfully completed a subscription payment.
        /// </summary>
        [HttpPost("players/{playerId:int}/academies/{academyId:int}/subscription-paid")]
        [Authorize(Roles = "SystemAdmin,Parent,Player")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> TriggerAcademySubscriptionPaidNotification([FromRoute] int playerId, [FromRoute] int academyId)
        {
            
            if (!TryGetRequester(out var currentUserId, out var role, out var authError))
                return authError!;

            if (role == "Player" && currentUserId != playerId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Players can only trigger payment notifications for their own profiles." });
            }

            

            await _playerNotificationService.NotifyAcademySubscriptionPaidAsync(
      playerId,
      academyId,
      currentUserId,
      role,
      HttpContext.RequestAborted);
            return Ok(new { message = "Academy administration notified of successful payment." });
        }

        [HttpPost("players/bulk-milestone")]
        [Authorize(Roles = "Coach,SystemAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> NotifyMultiplePlayersMilestone([FromBody] NotifyMultiplePlayersDto body)
        {
            if (body.PlayerIds == null || !body.PlayerIds.Any())
                return BadRequest(new { error = "Player IDs list cannot be empty." });

            if (body.PlayerIds.Count > 500)
                return BadRequest(new { error = "Cannot process more than 500 players in a single request." });

            if (string.IsNullOrWhiteSpace(body.Message) || body.Message.Length > 200)
                return BadRequest(new { error = "Message cannot be empty and must be under 200 characters." });

            await _playerNotificationService.NotifyMultiplePlayersAsync(
                body.PlayerIds,
                body.Message,
                HttpContext.RequestAborted);

            return Ok(new { message = "Bulk milestone notifications dispatched successfully." });
        }

        [HttpPost("players/bulk-parent-alert")]
        [Authorize(Roles = "Coach,SystemAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> NotifyParentsOfPlayers([FromBody] NotifyParentsOfPlayersDto body)
        {
            if (body.PlayerIds == null || !body.PlayerIds.Any())
                return BadRequest(new { error = "Player IDs list cannot be empty." });

            if (body.PlayerIds.Count > 500)
                return BadRequest(new { error = "Cannot process more than 500 players in a single request." });

            if (string.IsNullOrWhiteSpace(body.EventType) || body.EventType.Length > 100)
                return BadRequest(new { error = "Event type cannot be empty and must be under 100 characters." });

            await _playerNotificationService.NotifyParentsOfPlayersAsync(
                body.PlayerIds,
                body.EventType,
                HttpContext.RequestAborted);

            return Ok(new { message = "Bulk parent notifications dispatched successfully." });
        }

        #endregion

        #region 4. Scouter Networks

        [HttpPost("players/{playerId:int}/scouter-alerts")]
        [Authorize(Roles = "Coach,SystemAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> NotifyScouterFollowers([FromRoute] int playerId, [FromQuery] string eventType)
        {
            await _scouterNotificationService.NotifyScouterFollowersAsync(playerId, eventType, HttpContext.RequestAborted);
            return Ok(new { message = "Scouter feed updates dispatched successfully." });
        }

        #endregion

        #region 5. Match & Live Events

        [HttpPost("matches/{matchId:int}/events")]
        [Authorize(Roles = "Coach,SystemAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> TriggerMatchEventNotification(
            [FromRoute] int matchId,
            [FromQuery] string eventTitle,
            [FromQuery] string eventMessage,
            [FromQuery] string eventType)
        {
            if (string.IsNullOrWhiteSpace(eventTitle) || eventTitle.Length > 100)
                return BadRequest(new { error = "Event title is required and must be under 100 characters." });

            if (string.IsNullOrWhiteSpace(eventMessage) || eventMessage.Length > 500)
                return BadRequest(new { error = "Event message is required and must be under 500 characters." });

            if (string.IsNullOrWhiteSpace(eventType) || eventType.Length > 50)
                return BadRequest(new { error = "Event type is required and must be under 50 characters." });

            await _matchNotificationService.NotifyMatchEventAsync(matchId, eventTitle, eventMessage, eventType, HttpContext.RequestAborted);
            return Ok(new { message = "Match event successfully broadcasted to all related participants." });
        }

        
        [HttpPost("academies/{academyId:int}/academy-alerts")]
        [Authorize(Roles = "Coach,SystemAdmin,AcademyAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> NotifyAcademy([FromRoute] int academyId, [FromQuery] string message)
        {
            if (string.IsNullOrWhiteSpace(message) || message.Length > 300)
                return BadRequest(new { error = "Message is required and must be under 300 characters." });

            await _matchNotificationService.NotifyAcademyAsync(academyId, message, HttpContext.RequestAborted);

            return Ok(new { message = "Academy notification successfully broadcasted." });
        }

        [HttpPost("academies/bulk-alert")]
        [Authorize(Roles = "SystemAdmin,Coach,AcademyAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> NotifyAcademies([FromBody] AcademyNotificationDto dto, CancellationToken cancellationToken)
        {
            await _matchNotificationService.NotifyAcademiesAsync(dto.AcademyIds, dto.Message, cancellationToken);

            return Ok(new { message = "Academies successfully notified." });
        }

        #endregion
    }
}