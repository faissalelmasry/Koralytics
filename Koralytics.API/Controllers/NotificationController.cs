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

        public NotificationController(
            IAnnouncementNotificationService announcementService,
            IPlayerNotificationService playerNotificationService,
            IScouterNotificationService scouterNotificationService)
        {
            _announcementService = announcementService;
            _playerNotificationService = playerNotificationService;
            _scouterNotificationService = scouterNotificationService;
        }

        #region 1. Academy Announcements

        /// <summary>
        /// Dispatches an announcement notification to targeted academy audiences.
        /// </summary>
        /// <param name="academyId">The hosting tenant academy identifier.</param>
        /// <param name="body">The announcement metadata targeting payload requirements.</param>
        [HttpPost("academies/{academyId:int}/announcements")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SendAcademyAnnouncement([FromRoute] int academyId, [FromBody] CreateAnnouncementDto body)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var currentUserId))
            {
                return Unauthorized("User identity validation failed.");
            }

            // Invokes service layer validation & group routing pipeline
            await _announcementService.SendAnnouncementNotificationAsync(academyId, currentUserId, body);

            return Accepted();
        }

        #endregion

        #region 2. Player & Parent Engagement

        /// <summary>
        /// Triggers a milestone real-time broadcast to a specific player connection room.
        /// </summary>
        /// <param name="playerId">The target player identifier.</param>
        /// <param name="achievementType">The milestone achievement name or type description.</param>
        [HttpPost("players/{playerId:int}/milestone")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> NotifyPlayerMilestone([FromRoute] int playerId, [FromQuery] string achievementType)
        {
            await _playerNotificationService.NotifyPlayerMilestoneAsync(playerId, achievementType);
            return Ok(new { Message = "Milestone notification dispatched successfully." });
        }

        /// <summary>
        /// Dispatches real-time critical events directly to parents tracking a specific player.
        /// </summary>
        /// <param name="playerId">The target player identity context.</param>
        /// <param name="eventType">The business classification of the alert event.</param>
        [HttpPost("players/{playerId:int}/parent-alert")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> NotifyPlayerParents([FromRoute] int playerId, [FromQuery] string eventType)
        {
            await _playerNotificationService.NotifyParentAsync(playerId, eventType);
            return Ok(new { Message = "Parent notifications dispatched successfully." });
        }

        /// <summary>
        /// Forces execution of a subscription grace boundary alert to both athlete and guardian channels.
        /// </summary>
        /// <remarks>Can be used manually by management dashboards or tested directly before scheduling jobs.</remarks>
        [HttpPost("players/{playerId:int}/academies/{academyId:int}/subscription-grace")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> TriggerSubscriptionGraceNotification([FromRoute] int playerId, [FromRoute] int academyId)
        {
            await _playerNotificationService.NotifySubscriptionGraceAsync(playerId, academyId);
            return Ok(new { Message = "Subscription grace period alerts successfully broadcasted." });
        }

        #endregion

        #region 3. Scouter Networks

        /// <summary>
        /// Broadcasts player performance upgrades, highlight additions, or MOTM updates to following scouters.
        /// </summary>
        /// <param name="playerId">The targeted performer profile identity.</param>
        /// <param name="eventType">The type of performance event triggered (e.g., "HighlightUploaded", "Awarded MOTM").</param>
        [HttpPost("players/{playerId:int}/scouter-alerts")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> NotifyScouterFollowers([FromRoute] int playerId, [FromQuery] string eventType)
        {
            await _scouterNotificationService.NotifyScouterFollowersAsync(playerId, eventType);
            return Ok(new { Message = "Scouter feed updates dispatched successfully." });
        }

        #endregion
    }
}