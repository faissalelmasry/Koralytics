using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        private readonly IRealTimeBridge _realTimeBridge; // Injected to manage Redis operations directly

        public NotificationController(
            IAnnouncementNotificationService announcementService,
            IPlayerNotificationService playerNotificationService,
            IScouterNotificationService scouterNotificationService,
            IRealTimeBridge realTimeBridge)
        {
            _announcementService = announcementService;
            _playerNotificationService = playerNotificationService;
            _scouterNotificationService = scouterNotificationService;
            _realTimeBridge = realTimeBridge;
        }

        #region 1. User Notification Management (Active Feed & Reading Status)

        /// <summary>
        /// Retrieves the active, chronological list of notifications for the currently logged-in user.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyNotifications([FromQuery] int skip = 0, [FromQuery] int take = 50)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var currentUserId))
            {
                return Unauthorized("User identity validation failed.");
            }

            // Fetches ordered, paginated notifications from Redis cache (Hash + ZSET)
            var notifications = await _realTimeBridge.GetNotificationsAsync(currentUserId, skip, take, HttpContext.RequestAborted);
            return Ok(notifications);
        }

        /// <summary>
        /// Marks a specific notification as read for the logged-in user without altering its original score.
        /// </summary>
        /// <param name="notificationId">The unique ID of the target notification.</param>
        [HttpPatch("{notificationId}/read")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> MarkAsRead([FromRoute] string notificationId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var currentUserId))
            {
                return Unauthorized("User identity validation failed.");
            }

            // Updates IsRead to true inside the Redis Hash, keeping the ZSET score intact
            await _realTimeBridge.MarkAsReadAsync(currentUserId, notificationId, HttpContext.RequestAborted);
            return NoContent();
        }

        /// <summary>
        /// Triggers manual garbage collection for notifications older than 30 days for the logged-in user.
        /// </summary>
        [HttpDelete("expired")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> PurgeExpiredNotifications()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var currentUserId))
            {
                return Unauthorized("User identity validation failed.");
            }

            
            await _realTimeBridge.DeleteExpiredNotificationsAsync(currentUserId, HttpContext.RequestAborted);
            return Ok(new { message = "Expired notifications purged successfully." });
        }

        #endregion

        #region 2. Academy Announcements

        /// <summary>
        /// Dispatches an announcement notification to targeted academy audiences.
        /// Restricted to academy staff; the service layer additionally verifies the
        /// caller is actually a member of THIS specific academy (role claim alone
        /// can't prove that).
        ///
        /// NOTE: this is a direct, no-persistence path used to test the real-time
        /// (SignalR + Redis) dispatch pipeline in isolation. It does NOT create an
        /// AcademyAnnouncement history row. Production announcement creation goes
        /// through AcademyAnnouncementService.SendAnnouncementAsync instead, which
        /// persists the announcement first and then calls this same dispatch logic
        /// internally. Do not wire the frontend to this endpoint directly.
        /// </summary>
        /// <param name="academyId">The hosting tenant academy identifier.</param>
        /// <param name="body">The announcement metadata targeting payload requirements.</param>
        [HttpPost("academies/{academyId:int}/announcements")]
        [Authorize(Roles = "Coach,SystemAdmin")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SendAcademyAnnouncement([FromRoute] int academyId, [FromBody] CreateAnnouncementDto body)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var currentUserId))
            {
                return Unauthorized("User identity validation failed.");
            }

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

       
        /// <summary>
        /// Triggers a milestone real-time broadcast to a specific player connection room.
        /// </summary>
        /// <param name="playerId">The target player identifier.</param>
        /// <param name="achievementType">The milestone achievement name or type description.</param>
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

        /// <summary>
        /// Dispatches real-time critical events directly to parents tracking a specific player.
        /// </summary>
        /// <param name="playerId">The target player identity context.</param>
        /// <param name="eventType">The business classification of the alert event.</param>
        [HttpPost("players/{playerId:int}/parent-alert")]
        [Authorize(Roles = "Coach,SystemAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> NotifyPlayerParents([FromRoute] int playerId, [FromQuery] string eventType)
        {
            await _playerNotificationService.NotifyParentAsync(playerId, eventType, HttpContext.RequestAborted);
            return Ok(new { message = "Parent notifications dispatched successfully." });
        }

        /// <summary>
        /// Forces execution of a subscription grace boundary alert to both athlete and guardian channels.
        /// </summary>
        /// <remarks>Can be used manually by management dashboards or tested directly before scheduling jobs.</remarks>
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

        #endregion

        #region 4. Scouter Networks

        /// <summary>
        /// Broadcasts player performance upgrades, highlight additions, or MOTM updates to following scouters.
        /// </summary>
        /// <param name="playerId">The targeted performer profile identity.</param>
        /// <param name="eventType">The type of performance event triggered (e.g., "HighlightUploaded", "Awarded MOTM").</param>
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
    }
}