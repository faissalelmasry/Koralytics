using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Koralytics.Application.DTOs.Player;
using Koralytics.Application.DTOs.ScouterDtos;
using Koralytics.Application.Interfaces.Scouter;
using Koralytics.Application.Interfaces.ScouterInterfaces;
using Koralytics.Domain.Entities.Scouter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koralytics.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScouterController : ControllerBase
    {
        private readonly IScouterFollowService _followService;
        private readonly IScouterReportService _reportService;
        private readonly IScouterSearchService _searchService;
        private readonly IScouterShortlistService _shortlistService;

        public ScouterController(
            IScouterFollowService followService,
            IScouterReportService reportService,
            IScouterSearchService searchService,
            IScouterShortlistService shortlistService)
        {
            _followService = followService;
            _reportService = reportService;
            _searchService = searchService;
            _shortlistService = shortlistService;
        }

        #region 1. Player Search & Discovery

        [HttpPost("search")]
        [Authorize(Roles = "Scouter,SystemAdmin")]
        public async Task<IActionResult> SearchPlayers([FromBody] PlayerSearchFiltersDto filters)
        {
            var result = await _searchService.SearchPlayersAsync(filters);
            return Ok(new
            {
                message = "Search query completed successfully.",
                data = result
            });
        }


        #endregion

        #region 2. Shortlist Management

        [HttpGet("{scouterId}/shortlist")]
        [Authorize(Roles = "Scouter,SystemAdmin")]
        public async Task<IActionResult> GetShortlist(int scouterId)
        {
            var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var requesterRole = User.FindFirstValue(ClaimTypes.Role)!;

            if (requesterRole == "Scouter" && requesterId != scouterId)
                return Forbid();

            var result = await _shortlistService.GetShortlistAsync(scouterId);
            return Ok(new
            {
                message = $"Successfully retrieved shortlist for Scouter ID {scouterId}.",
                data = result
            });
        }

        [HttpPost("{scouterId}/shortlist/{playerId}")]
        [Authorize(Roles = "Scouter,SystemAdmin")]
        public async Task<IActionResult> AddToShortlist(int scouterId, int playerId)
        {
            var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var requesterRole = User.FindFirstValue(ClaimTypes.Role)!;

            if (requesterRole == "Scouter" && requesterId != scouterId)
                return Forbid();

            var result = await _shortlistService.AddToShortlistAsync(scouterId, playerId);
            return Ok(new
            {
                message = $"Player with ID {playerId} has been successfully added to Scouter {scouterId}'s shortlist.",
                data = result
            });
        }

        [HttpDelete("{scouterId}/shortlist/{playerId}")]
        [Authorize(Roles = "Scouter,SystemAdmin")]
        public async Task<IActionResult> RemoveFromShortlist(int scouterId, int playerId)
        {
            var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var requesterRole = User.FindFirstValue(ClaimTypes.Role)!;

            if (requesterRole == "Scouter" && requesterId != scouterId)
                return Forbid();

            await _shortlistService.RemoveFromShortlistAsync(scouterId, playerId);
            return Ok(new
            {
                message = $"Player with ID {playerId} was successfully removed from Scouter {scouterId}'s shortlist."
            });
        }

        #endregion

        #region 3. Social Interaction & Engagement Tracking

        [HttpGet("{scouterId}/followed-players")]
        [Authorize(Roles = "Scouter,SystemAdmin")]
        public async Task<IActionResult> GetFollowedPlayers(int scouterId)
        {
            var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var requesterRole = User.FindFirstValue(ClaimTypes.Role)!;

            if (requesterRole == "Scouter" && requesterId != scouterId)
                return Forbid();

            var followedPlayers = await _followService.GetFollowedPlayersAsync(scouterId);
            return Ok(new
            {
                message = $"Successfully retrieved followed players for Scouter ID {scouterId}.",
                data = followedPlayers
            });
        }

        [HttpPost("{scouterId}/follow/{playerId}")]
        [Authorize(Roles = "Scouter,SystemAdmin")]
        public async Task<IActionResult> FollowPlayer(int scouterId, int playerId)
        {
            var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var requesterRole = User.FindFirstValue(ClaimTypes.Role)!;

            if (requesterRole == "Scouter" && requesterId != scouterId)
                return Forbid();

            await _followService.FollowPlayerAsync(scouterId, playerId);
            return Ok(new
            {
                message = $"Scouter ID {scouterId} is now successfully following Player ID {playerId}."
            });
        }

        [HttpDelete("{scouterId}/follow/{playerId}")]
        [Authorize(Roles = "Scouter,SystemAdmin")]
        public async Task<IActionResult> UnfollowPlayer(int scouterId, int playerId)
        {
            var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var requesterRole = User.FindFirstValue(ClaimTypes.Role)!;

            if (requesterRole == "Scouter" && requesterId != scouterId)
                return Forbid();

            await _followService.UnfollowPlayerAsync(playerId, scouterId);
            return Ok(new
            {
                message = $"Scouter ID {scouterId} has successfully unfollowed Player ID {playerId}."
            });
        }

        [HttpPost("{scouterId}/view-profile/{playerId}")]
        [Authorize(Roles = "Scouter,SystemAdmin")]
        public async Task<IActionResult> LogProfileView(int scouterId, int playerId)
        {
            var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var requesterRole = User.FindFirstValue(ClaimTypes.Role)!;

            if (requesterRole == "Scouter" && requesterId != scouterId)
                return Forbid();

            await _followService.LogProfileViewAsync(scouterId, playerId);
            return Ok(new
            {
                message = $"Profile view interaction successfully logged. Scouter ID {scouterId} viewed Player ID {playerId}."
            });
        }

        [HttpGet("{playerId}/profile-views")]
        [Authorize(Roles = "Player,Parent,SystemAdmin")]
        public async Task<IActionResult> GetProfileViews(int playerId)
        {
            var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var requesterRole = User.FindFirstValue(ClaimTypes.Role)!;

            if ((requesterRole == "Player" || requesterRole == "Parent") && requesterId != playerId)
            {
                return Forbid();
            }

            var result = await _followService.GetProfileViewsAnalyticsAsync(playerId);

            return Ok(new
            {
                message = $"Successfully compiled profile visibility metrics for Player ID {playerId}.",
                data = result
            });
        }
        #endregion

        #region 4. AI Scouting Reports & Verifications

        [HttpGet("{scouterId}/reports/{playerId}")]
        [Authorize(Roles = "Scouter,SystemAdmin")]
        public async Task<IActionResult> GetScoutingReport(int scouterId, int playerId)
        {
            var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var requesterRole = User.FindFirstValue(ClaimTypes.Role)!;

            if (requesterRole == "Scouter" && requesterId != scouterId)
                return Forbid();

            var report = await _reportService.GetScoutingReportAsync(scouterId, playerId);
            return Ok(new
            {
                message = $"Scouting report for Player ID {playerId} retrieved successfully.",
                data = report
            });
        }

        [HttpPost("{scouterId}/reports/{playerId}/generate")]
        [Authorize(Roles = "Scouter,SystemAdmin")]
        public async Task<IActionResult> GenerateScoutingReport(int scouterId, int playerId)
        {
            var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var requesterRole = User.FindFirstValue(ClaimTypes.Role)!;

            if (requesterRole == "Scouter" && requesterId != scouterId)
                return Forbid();

            var reportText = await _reportService.GenerateScoutingReportAsync(scouterId, playerId);
            return Ok(new
            {
                message = $"AI Scouting Report successfully generated for Player ID {playerId}.",
                report = reportText
            });
        }

        [HttpPost("{scouterId}/verify")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> VerifyScouter(int scouterId)
        {
            await _reportService.VerifyScouterAsync(scouterId);
            return Ok(new
            {
                message = $"Scouter account with ID {scouterId} has been successfully verified."
            });
        }

        #endregion
       
        #region 5. Scouter Profile Retrieval

        [HttpGet("me")]
        [Authorize(Roles = "Scouter")]
        public async Task<IActionResult> GetMyProfile()
        {
            var scouterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var profile = await _searchService.GetScouterByIdAsync(scouterId);
            if (profile == null)
                return NotFound(new { message = "Scouter profile not found." });

            return Ok(new
            {
                message = "Your profile retrieved successfully.",
                data = profile
            });
        }

        [HttpGet("{scouterId}")]
        [Authorize(Roles = "SystemAdmin,Player,Parent")]
        public async Task<IActionResult> GetScouterProfileById(int scouterId)
        {
            var profile = await _searchService.GetScouterByIdAsync(scouterId);
            if (profile == null)
                return NotFound(new { message = $"Scouter with ID {scouterId} was not found." });

            return Ok(new
            {
                message = $"Successfully retrieved profile details for Scouter ID {scouterId}.",
                data = profile
            });
        }

        #endregion
    }
}