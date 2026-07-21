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

        #region Helpers

        /// <summary>
        /// Safely resolves the caller's identity from claims. Returns false (with a 401 result)
        /// instead of throwing if the expected claims are missing or malformed.
        /// </summary>
        private bool TryGetRequester(out int requesterId, out string requesterRole, out IActionResult? errorResult)
        {
            requesterId = 0;
            requesterRole = string.Empty;
            errorResult = null;

            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var roleClaim = User.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out requesterId) || string.IsNullOrEmpty(roleClaim))
            {
                errorResult = Unauthorized(new { message = "Unable to resolve caller identity from the provided credentials." });
                return false;
            }

            requesterRole = roleClaim;
            return true;
        }

        /// <summary>
        /// Enforces that a Scouter can only act on their own resources; SystemAdmin bypasses this check.
        /// Returns a Forbid()/Unauthorized() result to short-circuit the action, or null if the caller may proceed.
        /// </summary>
        private bool TryAuthorizeScouterOwnership(int scouterId, out IActionResult? result)
        {
            if (!TryGetRequester(out var requesterId, out _, out result))
                return false;

            
            if (!User.IsInRole("SystemAdmin") && requesterId != scouterId)
            {
                result = Forbid();
                return false;
            }

            result = null;
            return true;
        }

        /// <summary>
        /// Validates common pagination query parameters. Returns false and a BadRequest result on invalid input.
        /// </summary>
        private bool TryValidatePagination(int pageNumber, int pageSize, out IActionResult? result)
        {
            if (pageNumber < 1)
            {
                result = BadRequest(new { message = "pageNumber must be 1 or greater." });
                return false;
            }

            if (pageSize < 1 || pageSize > 100)
            {
                result = BadRequest(new { message = "pageSize must be between 1 and 100." });
                return false;
            }

            result = null;
            return true;
        }

        #endregion

        #region 1. Player Search & Discovery

        [HttpPost("search")]
        [Authorize(Roles = "Scouter,SystemAdmin")]
        public async Task<IActionResult> SearchPlayers([FromBody] PlayerSearchFiltersDto filters)
        {
            if (filters == null)
                return BadRequest(new { message = "Search filters payload is required." });

            if (!TryValidatePagination(filters.PageNumber, filters.PageSize, out var paginationError))
                return paginationError!;

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
        public async Task<IActionResult> GetShortlist(int scouterId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? searchTerm = null)
        {
            if (!TryAuthorizeScouterOwnership(scouterId, out var authError))
                return authError!;

            if (!TryValidatePagination(pageNumber, pageSize, out var paginationError))
                return paginationError!;

            var result = await _shortlistService.GetShortlistAsync(scouterId, pageNumber, pageSize, searchTerm);

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
            if (!TryAuthorizeScouterOwnership(scouterId, out var authError))
                return authError!;

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
            if (!TryAuthorizeScouterOwnership(scouterId, out var authError))
                return authError!;

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
        public async Task<IActionResult> GetFollowedPlayers(int scouterId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? searchTerm = null)
        {
            if (!TryAuthorizeScouterOwnership(scouterId, out var authError))
                return authError!;

            if (!TryValidatePagination(pageNumber, pageSize, out var paginationError))
                return paginationError!;

            var paginatedFollowedPlayers = await _followService.GetFollowedPlayersAsync(scouterId, pageNumber, pageSize, searchTerm);

            return Ok(new
            {
                message = $"Successfully retrieved followed players for Scouter ID {scouterId}.",
                data = paginatedFollowedPlayers
            });
        }

        [HttpPost("{scouterId}/follow/{playerId}")]
        [Authorize(Roles = "Scouter,SystemAdmin")]
        public async Task<IActionResult> FollowPlayer(int scouterId, int playerId)
        {
            if (!TryAuthorizeScouterOwnership(scouterId, out var authError))
                return authError!;

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
            if (!TryAuthorizeScouterOwnership(scouterId, out var authError))
                return authError!;

            await _followService.UnfollowPlayerAsync(scouterId, playerId);
            return Ok(new
            {
                message = $"Scouter ID {scouterId} has successfully unfollowed Player ID {playerId}."
            });
        }

        [HttpPost("{scouterId}/view-profile/{playerId}")]
        [Authorize(Roles = "Scouter,SystemAdmin")]
        public async Task<IActionResult> LogProfileView(int scouterId, int playerId)
        {
            if (!TryAuthorizeScouterOwnership(scouterId, out var authError))
                return authError!;

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
            if (!TryGetRequester(out var requesterId, out var requesterRole, out var authError))
                return authError!;

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
            if (!TryAuthorizeScouterOwnership(scouterId, out var authError))
                return authError!;

            
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
            if (!TryAuthorizeScouterOwnership(scouterId, out var authError))
                return authError!;

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
            if (!TryGetRequester(out var scouterId, out _, out var authError))
                return authError!;

            
            var profile = await _searchService.GetScouterByIdAsync(scouterId);

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

            return Ok(new
            {
                message = $"Successfully retrieved profile details for Scouter ID {scouterId}.",
                data = profile
            });
        }

        #endregion
    }
}