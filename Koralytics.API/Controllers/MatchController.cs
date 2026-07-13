using Koralytics.API.Controllers.BaseController;
using Koralytics.Application.DTOs.Match;
using Koralytics.Application.Interfaces.Match;
using Koralytics.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Koralytics.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class MatchController : ApiBaseController
    {
        private readonly IMatchService _matchService;
        private readonly IMatchEventService _matchEventService;
        private readonly IMatchRatingService _matchRatingService;
        private readonly IMatchAnalyticsService _matchAnalyticsService;

        public MatchController(IMatchService matchService, IMatchEventService matchEventService, IMatchRatingService matchRatingService, IMatchAnalyticsService matchAnalyticsService)
        {
            _matchService = matchService;
            _matchEventService = matchEventService;
            _matchRatingService = matchRatingService;
            _matchAnalyticsService = matchAnalyticsService;
        }

        [HttpPost("friendly")]
        [Authorize(Roles = "Coach,AcademyAdmin")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateFriendlyMatch([FromBody] CreateFriendlyMatchDto dto)
        {
            var result = await _matchService.CreateFriendlyMatchAsync(dto);
            return CreatedResponse(result, nameof(GetMatch), new { matchId = result.Id }, "Friendly match created successfully");
        }

        [HttpPost("tournament")]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateTournamentMatch([FromBody] CreateTournamentMatchDto dto)
        {
            var result = await _matchService.CreateTournamentMatchAsync(dto);
            return CreatedResponse(result, nameof(GetMatch), new { matchId = result.Id }, "Tournament match created successfully");
        }

        [HttpPost("session")]
        [Authorize(Roles = "Coach")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateSessionMatch([FromBody] CreateSessionMatchDto dto)
        {
            var result = await _matchService.CreateSessionMatchAsync(dto);
            return CreatedResponse(result, nameof(GetMatch), new { matchId = result.Id }, "Session match created successfully");
        }

        [HttpGet("{matchId:int}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMatch(int matchId)
        {
            var result = await _matchService.GetMatchAsync(matchId);
            return OkResponse(result);
        }

        [HttpPatch("{matchId:int}/start")]
        [Authorize(Roles = "SuperAdmin,Coach,AcademyAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> StartMatch(int matchId)
        {
            var result = await _matchService.StartMatchAsync(matchId);
            return OkResponse(result, "Match started successfully");
        }

        [HttpGet]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMatchesByDate([FromQuery] DateTime date, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _matchService.GetMatchesByDateAsync(date, page, pageSize);
            return OkResponse(result);
        }

        [HttpGet("team/{teamId:int}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTeamMatches(int teamId, [FromQuery] MatchStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _matchService.GetTeamMatchesByStatusAsync(teamId, status, page, pageSize);
            return OkResponse(result);
        }

        [HttpGet("team/{teamId:int}/form-guide")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetFormGuide(int teamId, [FromQuery] MatchFormat format)
        {
            var result = await _matchService.GetFormGuideAsync(teamId, format);
            return OkResponse(result);
        }

        [HttpPost("{matchId:int}/events")]
        [Authorize(Roles = "Coach")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> LogMatchEvent(int matchId, [FromBody] LogMatchEventDto dto)
        {
            var result = await _matchEventService.LogMatchEventAsync(matchId, dto);
            return CreatedResponse(result, nameof(GetMatchTimeline), new { matchId }, "Match event logged successfully");
        }

        [HttpPost("{matchId:int}/session-events")]
        [Authorize(Roles = "Coach")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> LogSessionMatchEvent(int matchId, [FromBody] LogSessionMatchEventDto dto)
        {
            var result = await _matchEventService.LogSessionMatchEventAsync(matchId, dto);
            return CreatedResponse(result, nameof(GetMatchTimeline), new { matchId }, "Session match event logged successfully");
        }

        [HttpGet("{matchId:int}/timeline")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMatchTimeline(int matchId)
        {
            var result = await _matchEventService.GetMatchTimelineAsync(matchId);
            return OkResponse(result);
        }

        [HttpPost("{matchId:int}/lineup")]
        [Authorize(Roles = "Coach")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SubmitLineup(int matchId, [FromBody] SubmitLineupDto dto)
        {
            var coachId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _matchRatingService.SubmitLineupAsync(matchId, coachId, dto);
            return CreatedResponse(result, nameof(GetLineup), new { matchId }, "Lineup submitted successfully");
        }

        [HttpGet("{matchId:int}/lineup")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLineup(int matchId)
        {
            var result = await _matchRatingService.GetLineupAsync(matchId);
            return OkResponse(result);
        }

        [HttpPatch("{matchId:int}/end")]
        [Authorize(Roles = "Coach,AcademyAdmin,SuperAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> EndMatch(int matchId)
        {
            var result = await _matchService.EndMatchAsync(matchId);
            return OkResponse(result, "Match ended successfully");
        }

        [HttpPost("{matchId:int}/ratings")]
        [Authorize(Roles = "SuperAdmin,Coach,AcademyAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SubmitMatchRatings(int matchId, [FromBody] SubmitMatchRatingsDto dto)
        {
            var coachId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _matchRatingService.SubmitMatchRatingsAsync(matchId, coachId, dto);
            return OkResponse(result, "Match ratings submitted successfully");
        }

        [HttpGet("head-to-head")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetHeadToHead([FromQuery] int teamAId, [FromQuery] int teamBId)
        {
            var result = await _matchAnalyticsService.GetHeadToHeadAsync(teamAId, teamBId);
            return OkResponse(result);
        }

        [HttpGet("team/{teamId:int}/analysis")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPostMatchAnalysis(int teamId)
        {
            var result = await _matchAnalyticsService.GetPostMatchAnalysisAsync(teamId);
            return OkResponse(result);
        }
    }
}
