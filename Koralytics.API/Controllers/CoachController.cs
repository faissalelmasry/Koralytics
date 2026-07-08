using System.Security.Claims;
using Koralytics.Application.Services.Coach.CoachNoteService;
using Koralytics.Application.Services.Coach.CoachSquadService;
using Koralytics.Application.DTOs.Coach;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koralytics.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Coach,AcademyAdmin")]
    public class CoachController : ControllerBase
    {
        private readonly ICoachSquadService _coachSquadService;
        private readonly ICoachNoteService _coachNoteService;

        public CoachController(
            ICoachSquadService coachSquadService,
            ICoachNoteService coachNoteService)
        {
            _coachSquadService = coachSquadService;
            _coachNoteService = coachNoteService;
        }

        /// <summary>
        /// Returns the full squad for a given team, including each player's
        /// FIFA-card style rating and availability status.
        /// Only the coach assigned to that team may call this endpoint.
        /// </summary>
        [HttpGet("{coachId}/teams/{teamId}/squad")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetSquad(int coachId, int teamId)
        {
            // Enforce that the authenticated coach can only query their own data
            var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (requesterId != coachId)
                return Forbid();

            var squad = await _coachSquadService.GetSquadAsync(coachId, teamId);
            return Ok(squad);
        }

        /// <summary>
        /// Splits the attending players of a drill session into two balanced training groups
        /// using a snake-draft algorithm sorted by overall rating.
        /// </summary>
        [HttpPost("sessions/{sessionId}/split")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> SplitTrainingTeams(int sessionId)
        {
            var coachId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var split = await _coachSquadService.SplitTrainingTeamsAsync(coachId, sessionId);
            return Ok(split);
        }

        /// <summary>
        /// Returns a side-by-side comparison of two players' category ratings and attributes.
        /// Accessible by both Coach and AcademyAdmin roles.
        /// </summary>
        [HttpGet("squad/compare")]
        public async Task<IActionResult> CompareSquadPlayers(
            [FromQuery] int playerAId,
            [FromQuery] int playerBId)
        {
            if (playerAId == playerBId)
                return BadRequest(new { message = "playerAId and playerBId must be different." });

            var comparison = await _coachSquadService.GetSquadComparisonAsync(playerAId, playerBId);
            return Ok(comparison);
        }
        /// <summary>
        /// Writes a note about a player who belongs to one of the coach's active teams.
        /// Optionally links the note to a drill session or match.
        /// </summary>
        [HttpPost("notes")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> WriteNote([FromBody] WriteNoteDto dto)
        {
            var coachId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var academyId = int.Parse(User.FindFirstValue("academyId")!);

            var note = await _coachNoteService.WriteNoteAsync(coachId, academyId, dto);
            return CreatedAtAction(nameof(GetPlayerNotes),
                new { playerId = dto.PlayerId }, note);
        }

        /// <summary>
        /// Returns all notes written by the authenticated coach for a specific player,
        /// ordered newest-first.
        /// </summary>
        [HttpGet("players/{playerId}/notes")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetPlayerNotes(int playerId)
        {
            var coachId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var notes = await _coachNoteService.GetPlayerNotesAsync(coachId, playerId);
            return Ok(notes);
        }
    }
}
