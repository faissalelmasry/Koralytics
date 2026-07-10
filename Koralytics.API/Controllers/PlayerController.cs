using System.Security.Claims;
using Koralytics.Application.Services.Player.PlayerCardService;
using Koralytics.Application.Services.Player.PlayerProfileServices;
using Koralytics.Application.Services.Player.PlayerTransferService;
using Koralytics.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Koralytics.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlayerController : ControllerBase
    {
        private readonly IPlayerTransferService _playerTransferService;
        private readonly IPlayerCardService _playerCardService;
        private readonly IPlayerProfileService _playerProfileService;

        public PlayerController(
            IPlayerTransferService playerTransferService,
            IPlayerCardService playerCardService,
            IPlayerProfileService playerProfileService)
        {
            _playerTransferService = playerTransferService;
            _playerCardService = playerCardService;
            _playerProfileService = playerProfileService;
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
    }
}
