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
            return Ok(result);
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
            return Ok(result);
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
            return Ok(result);
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
            return NoContent();
        }

        #endregion

        #region 3. Social Interaction & Engagement Tracking
        [HttpGet("{scouterId}/followed-players")]
        [Authorize(Roles = "Scouter,SystemAdmin")]
        public async Task<IActionResult> GetFollowedPlayers(int scouterId)
        {
            var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var requesterRole = User.FindFirstValue(ClaimTypes.Role)!;

            // Prevent scouters from viewing lists owned by other scouters
            if (requesterRole == "Scouter" && requesterId != scouterId)
                return Forbid();

            var followedPlayers = await _followService.GetFollowedPlayersAsync(scouterId);
            return Ok(followedPlayers);
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
            return NoContent();
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
            return NoContent();
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
            return NoContent();
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
            return Ok(report);
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
            return Ok(new { report = reportText });
        }

        [HttpPost("{scouterId}/verify")]
        [Authorize(Roles = "SystemAdmin")] 
        public async Task<IActionResult> VerifyScouter(int scouterId)
        {
            await _reportService.VerifyScouterAsync(scouterId);
            return NoContent();
        }

        #endregion
    }
}