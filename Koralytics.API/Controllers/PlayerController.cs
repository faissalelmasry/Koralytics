using System.Security.Claims;
using Koralytics.Application.DTOs.Player;
using Koralytics.Application.Services.Player.PlayerCardService;
using Koralytics.Application.Services.Player.PlayerGoalService;
using Koralytics.Application.Services.Player.PlayerProfileServices;
using Koralytics.Application.Services.Player.PlayerTransferService;
using Koralytics.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Koralytics.Application.Services.Storage;

namespace Koralytics.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlayerController : ControllerBase
    {
        private readonly IPlayerTransferService _playerTransferService;
        private readonly IPlayerCardService _playerCardService;
        private readonly IPlayerProfileService _playerProfileService;
        private readonly IStorageService _storageService;
        private readonly IPlayerGoalService _playerGoalService;

        public PlayerController(
            IPlayerTransferService playerTransferService,
            IPlayerCardService playerCardService,
            IPlayerProfileService playerProfileService,
            IStorageService storageService,
            IPlayerGoalService playerGoalService)
        {
            _playerTransferService = playerTransferService;
            _playerCardService = playerCardService;
            _playerProfileService = playerProfileService;
            _storageService = storageService;
            _playerGoalService = playerGoalService;
        }
        [HttpPatch("{playerId}/availability")]
        [Authorize(Roles = "Player,Coach,AcademyAdmin")]
        public async Task<IActionResult> UpdateAvailability(int playerId, [FromQuery] AvailabilityStatus status)
        {
            var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var requesterRole = User.FindFirstValue(ClaimTypes.Role)!;
            var requesterAcademyId = int.Parse(User.FindFirstValue("academyId")!);

            if (requesterRole == "Player" && requesterId != playerId)
                return Forbid();

            await _playerTransferService.UpdateAvailabilityAsync(playerId, status, requesterAcademyId, requesterRole);
            return NoContent();
        }
        [HttpPost("{playerId}/transfer/{newAcademyId}")]
        [Authorize(Roles = "AcademyAdmin")]
        public async Task<IActionResult> TransferPlayer(int playerId, int newAcademyId)
        {
            var requesterAcademyId = int.Parse(User.FindFirstValue("academyId")!);

            await _playerTransferService.TransferPlayerAsync(playerId, newAcademyId, requesterAcademyId);
            return NoContent();
        }
        [HttpPost("{playerId}/Loan/{AcademyId}")]
        [Authorize(Roles = "AcademyAdmin")]
        public async Task<IActionResult> LoanPlayer(int playerId, int AcademyId)
        {
            var requesterAcademyId = int.Parse(User.FindFirstValue("academyId")!);

            await _playerTransferService.LoanPlayerAsync(playerId, AcademyId, requesterAcademyId);
            return NoContent();
        }

        [HttpGet("{playerId}/card")]
        [Authorize]
        public async Task<IActionResult> GetPlayerCard(int playerId)
        {
            var card = await _playerCardService.GetPlayerCardAsync(playerId);

            return Ok(card);
        }

        [HttpGet("{playerId}/profile")]
        [Authorize]
        public async Task<IActionResult> GetPlayerProfile(int playerId)
        {
            var profile = await _playerProfileService.GetPlayerProfileAsync(playerId);
            return Ok(profile);
        }

        [HttpGet("{playerId}/timeline/drills")]
        [Authorize(Roles = "Player")]
        public async Task<IActionResult> GetDrillTimeline(
            int playerId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (requesterId != playerId)
                return Forbid();

            var timeline = await _playerProfileService.GetDrillTimelineAsync(
                playerId, page, pageSize);

            return Ok(timeline);
        }

        [HttpGet("{playerId}/timeline/matches")]
        [Authorize(Roles = "Player")]
        public async Task<IActionResult> GetMatchTimeline(
            int playerId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (requesterId != playerId)
                return Forbid();

            var timeline = await _playerProfileService.GetMatchTimelineAsync(
                playerId, page, pageSize);

            return Ok(timeline);
        }

        [HttpGet("{playerId}/timeline/achievements")]
        [Authorize(Roles = "Player")]
        public async Task<IActionResult> GetAchievementTimeline(
            int playerId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (requesterId != playerId)
                return Forbid();

            var timeline = await _playerProfileService.GetAchievementTimelineAsync(
                playerId, page, pageSize);

            return Ok(timeline);
        }

        [HttpPost("{playerId}/card/recalculate")]
        [Authorize]
        public async Task<IActionResult> RecalculatePlayerCard(int playerId)
        {
            await _playerCardService.RecalculatePlayerCardAsync(playerId);
            return NoContent();
        }

        [HttpGet("{playerId}/academy/{academyId}/comparison")]
        [Authorize(Roles = "Player")]
        public async Task<IActionResult> GetPlayerVsAcademyAverage(int playerId, int academyId)
        {
            var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (requesterId != playerId)
                return Forbid();

            var comparison = await _playerProfileService.GetPlayerVsAcademyAverageAsync(
                playerId, academyId);
            return Ok(comparison);
        }

        [HttpGet("{playerId}/scouter-views")]
        [Authorize(Roles = "Player")]
        public async Task<IActionResult> GetScouterViewsCount(
            int playerId,
            [FromQuery] int year,
            [FromQuery] int month)
        {
            var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (requesterId != playerId)
                return Forbid();

            var views = await _playerProfileService.GetScouterViewsCountAsync(
                playerId, year, month);
            return Ok(views);
        }

        [HttpGet("{playerId}/transfer-rate")]
        [Authorize]
        public async Task<IActionResult> GetTransferRate(int playerId)
        {
            var rate = await _playerCardService.GetDrillToMatchTransferRateAsync(playerId);
            if (rate is null)
                return NotFound(new { message = "Insufficient data. Player card not yet calculated." });

            return Ok(rate);
        }

        [HttpPost("{playerId}/highlights")]
        [Authorize(Roles = "Player")]
        public async Task<IActionResult> UploadHighlight(int playerId, [FromForm] int academyId, IFormFile file, [FromForm] string? title)
        {
            var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (requesterId != playerId)
                return Forbid();

            var result = await _storageService.UploadHighlightAsync(playerId, academyId, file, title);
            return CreatedAtAction(nameof(GetHighlights), new { playerId }, result);
        }

        [HttpDelete("{playerId}/highlights/{highlightId}")]
        [Authorize(Roles = "Player")]
        public async Task<IActionResult> DeleteHighlight(int playerId, int highlightId)
        {
            var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (requesterId != playerId)
                return Forbid();

            await _storageService.DeleteHighlightAsync(highlightId, playerId);
            return NoContent();
        }

        [HttpPatch("{playerId}/highlights/{highlightId}/pin")]
        [Authorize(Roles = "Player")]
        public async Task<IActionResult> PinHighlight(int playerId, int highlightId)
        {
            var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (requesterId != playerId)
                return Forbid();

            await _storageService.PinHighlightAsync(highlightId, playerId);
            return NoContent();
        }

        [HttpGet("{playerId}/highlights")]
        [Authorize]
        public async Task<IActionResult> GetHighlights(int playerId)
        {
            var highlights = await _storageService.GetHighlightsAsync(playerId);
            return Ok(highlights);
        }
        [HttpPost("{playerId}/goals")]
        [Authorize(Roles = "Coach,AcademyAdmin")]
        public async Task<IActionResult> CreatePlayerGoal(int playerId, [FromBody] CreatePlayerGoalDto dto)
        {
            var result = await _playerGoalService.CreatePlayerGoalAsync(playerId, dto);
            return Ok(result);
        }

        [HttpPatch("goals/{goalId:int}")]
        [Authorize(Roles = "Coach,AcademyAdmin")]
        public async Task<IActionResult> UpdatePlayerGoal(int goalId, [FromBody] UpdatePlayerGoalDto dto)
        {
            var result = await _playerGoalService.UpdatePlayerGoalAsync(goalId, dto);
            return Ok(result);
        }
    }
}
