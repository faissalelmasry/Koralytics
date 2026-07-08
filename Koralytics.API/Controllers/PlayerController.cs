using System.Security.Claims;
using Koralytics.Application.Services.Player.PlayerCardService;
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

        public PlayerController(
            IPlayerTransferService playerTransferService,
            IPlayerCardService playerCardService)
        {
            _playerTransferService = playerTransferService;
            _playerCardService = playerCardService;
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

        [HttpPost("{playerId}/card/recalculate")]
        [Authorize]
        public async Task<IActionResult> RecalculatePlayerCard(int playerId)
        {
            await _playerCardService.RecalculatePlayerCardAsync(playerId);
            return NoContent();
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
