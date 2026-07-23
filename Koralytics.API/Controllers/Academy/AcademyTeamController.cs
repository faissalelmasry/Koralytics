using Koralytics.API.Controllers.BaseController;
using Koralytics.Application.DTOs.Academies;
using Koralytics.Application.Services.Academy.AcademyTeamService;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;

namespace Koralytics.API.Controllers.Academies
{
    [ApiController]
    [Route("api/Academy")]
    [Authorize]
    [Produces("application/json")]
    public class AcademyTeamController : ApiBaseController
    {
        private readonly IAcademyTeamService _academyTeamService;

        public AcademyTeamController(IAcademyTeamService academyTeamService)
        {
            _academyTeamService = academyTeamService;
        }

        [HttpPost("{academyId}/age-groups")]
        [Authorize(Roles = "AcademyAdmin,Coach")]
        public async Task<IActionResult> CreateAgeGroup(int academyId, [FromBody] CreateAgeGroupDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _academyTeamService.CreateAgeGroupAsync(academyId, dto, userId);
            return OkResponse(result, "Age group created successfully.");
        }

        [HttpGet("{academyId}/age-groups")]
        [Authorize(Roles = "AcademyAdmin,Coach,Player,Parent")]
        public async Task<IActionResult> GetAgeGroups(int academyId)
        {
            var result = await _academyTeamService.GetAgeGroupsByAcademyAsync(academyId);
            return OkResponse(result, "Age groups retrieved successfully.");
        }

        [HttpPost("{academyId}/teams")]
        [Authorize(Roles = "AcademyAdmin,Coach")]
        public async Task<IActionResult> CreateTeam(int academyId, [FromBody] CreateTeamDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _academyTeamService.CreateTeamAsync(academyId, dto, userId);
            return OkResponse(result, "Team created successfully.");
        }

        [HttpGet("{academyId}/teams")]
        [Authorize(Roles = "AcademyAdmin,Coach,Player,Parent")]
        public async Task<IActionResult> GetTeams(int academyId)
        {
            var result = await _academyTeamService.GetTeamsByAcademyAsync(academyId);
            return OkResponse(result, "Teams retrieved successfully.");
        }

        [HttpPost("teams/{teamId}/coaches/{coachId}")]
        [Authorize(Roles = "AcademyAdmin")]
        public async Task<IActionResult> AssignCoachToTeam(int teamId, int coachId)
        {
            var userId = GetCurrentUserId();
            var result = await _academyTeamService.AssignCoachToTeamAsync(coachId, teamId, userId);
            return OkResponse(result, "Coach assigned to team successfully.");
        }

        [HttpDelete("teams/{teamId}/coaches/{coachId}")]
        [Authorize(Roles = "AcademyAdmin")]
        public async Task<IActionResult> RemoveCoachFromTeam(int teamId, int coachId)
        {
            var userId = GetCurrentUserId();
            await _academyTeamService.RemoveCoachFromTeamAsync(coachId, teamId, userId);
            return NoContentResponse("Coach removed from team successfully.");
        }

        [HttpPost("teams/{teamId}/players/{playerId}")]
        [Authorize(Roles = "AcademyAdmin,Coach")]
        public async Task<IActionResult> AssignPlayerToTeam(int teamId, int playerId)
        {
            var userId = GetCurrentUserId();
            await _academyTeamService.AssignPlayerToTeamAsync(playerId, teamId, userId);
            return NoContentResponse("Player assigned to team successfully.");
        }

        [HttpDelete("teams/{teamId}/players/{playerId}")]
        [Authorize(Roles = "AcademyAdmin,Coach")]
        public async Task<IActionResult> RemovePlayerFromTeam(int teamId, int playerId)
        {
            var userId = GetCurrentUserId();
            await _academyTeamService.RemovePlayerFromTeamAsync(playerId, teamId, userId);
            return NoContentResponse("Player removed from team successfully.");
        }

    }
}
