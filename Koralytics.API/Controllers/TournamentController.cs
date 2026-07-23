using Koralytics.API.Controllers.BaseController;
using Koralytics.Application.DTOs.Tournament;
using Koralytics.Application.Interfaces.Tournament;
using Koralytics.Application.Interfaces.Tournaments;
using Koralytics.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Koralytics.API.Controllers
{
    [Route("api/[controller]")]
    //[Authorize]
    public class TournamentController : ApiBaseController
    {
        private readonly ITournamentService _tournamentService;
        private readonly ITournamentDrawService _tournamentDrawService;
        private readonly ITournamentFixtureService _tournamentFixtureService;
        private readonly ITournamentReportService _tournamentReportService;

        public TournamentController(
            ITournamentService tournamentService,
            ITournamentDrawService tournamentDrawService,
            ITournamentFixtureService tournamentFixtureService,
            ITournamentReportService tournamentReportService)
        {
            _tournamentService = tournamentService;
            _tournamentDrawService = tournamentDrawService;
            _tournamentFixtureService = tournamentFixtureService;
            _tournamentReportService = tournamentReportService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTournaments()
        {
            var result = await _tournamentService.GetAllAsync();
            return OkResponse(result);
        }

        [HttpGet("{tournamentId}")]
        public async Task<IActionResult> GetTournament(int tournamentId)
        {
            var result = await _tournamentService.GetByIdAsync(tournamentId);

            if (result is null)
                return NotFound(new { message = $"Tournament with Id {tournamentId} not found" });

            return OkResponse(result);
        }
        [HttpPost]
        //[Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> CreateTournament(
            [FromBody] CreateTournamentDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _tournamentService
                .CreateTournamentAsync(dto, userId);

            return CreatedResponse(
                result,
                nameof(GetTournament),
                new { tournamentId = result.Id },
                "Tournament created successfully");
        }

        [HttpPost("{tournamentId}/invite/{academyId}")]
        //[Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> InviteAcademy(
            int tournamentId, int academyId)
        {
            await _tournamentService.InviteAcademyAsync(tournamentId, academyId);
            return NoContentResponse("Academy invited successfully");
        }

        [HttpPut("{tournamentId}/accept/{academyId}")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> AcceptInvitation(
            int tournamentId, int academyId)
        {
            await _tournamentService.AcceptInvitationAsync(tournamentId, academyId);
            return NoContentResponse("Invitation accepted successfully");
        }

        [HttpPost("{tournamentId}/squad/{teamId}")]
        public async Task<IActionResult> RegisterSquad(
            int tournamentId,
            int teamId,
            [FromBody] List<int> playerIds)
        {
            await _tournamentService.RegisterSquadAsync(
                tournamentId, teamId, playerIds);
            return NoContentResponse("Squad registered successfully");
        }

        [HttpGet("{tournamentId}/squad/{teamId}/players")]
        public async Task<IActionResult> GetRegisteredPlayerIds(
            int tournamentId, int teamId)
        {
            var result = await _tournamentService
                .GetRegisteredPlayerIdsAsync(tournamentId, teamId);
            return OkResponse(result);
        }

        [HttpPost("{tournamentId}/seeding")]
        //[Authorize(Roles = "SuperAdmin")]

        public async Task<IActionResult> GenerateSeeding(int tournamentId)
        {
            await _tournamentDrawService.GenerateSeedingAsync(tournamentId);
            return NoContentResponse("Seeding generated successfully");
        }

        [HttpPost("{tournamentId}/draw")]
        //[Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GenerateDraw(int tournamentId)
        {
            await _tournamentDrawService.GenerateDrawAsync(tournamentId);
            return NoContentResponse("Draw generated successfully");
        }

        [HttpPost("{tournamentId}/rounds/{roundId}/advance")]
        //[Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> AdvanceKnockout(
            int tournamentId, int roundId)
        {
            await _tournamentFixtureService.AdvanceKnockoutAsync(
                tournamentId, roundId);
            return NoContentResponse("Knockout round advanced successfully");
        }

        [HttpGet("{tournamentId}/bracket")]
        public async Task<IActionResult> GetBracket(int tournamentId)
        {
            var result = await _tournamentReportService
                .GetBracketAsync(tournamentId);
            return OkResponse(result);
        }

        [HttpGet("{tournamentId}/teams")]
        public async Task<IActionResult> GetTeams(int tournamentId)
        {
            var result = await _tournamentService.GetTeamsAsync(tournamentId);
            return OkResponse(result);
        }

        [HttpGet("{tournamentId}/hall-of-fame")]
        public async Task<IActionResult> GetHallOfFame(int tournamentId)
        {
            var result = await _tournamentReportService
                .GetHallOfFameAsync(tournamentId);
            return OkResponse(result);
        }

    
        [HttpPost("{tournamentId}/complete")]
        //[Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> CompleteTournament(int tournamentId)
        {
            await _tournamentReportService.CompleteTournamentAsync(tournamentId);
            return NoContentResponse("Tournament completed successfully");
        }


        private int GetCurrentUserId()
        {
            var userIdClaim = User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) ||
                !int.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("Invalid user token");

            return userId;
        }
      
        [HttpPut("{tournamentId}/status")]
        //[Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateStatus(
            int tournamentId, [FromBody] TournamentStatus status)
        {
            await _tournamentService.UpdateStatusAsync(tournamentId, status);
            return NoContentResponse("Tournament status updated successfully");
        }
    }
}
