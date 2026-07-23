using Koralytics.Application.DTOs.Drill;
using Koralytics.Application.Services.Drill.DrillAnalytic;
using Koralytics.Application.Services.Drill.DrillResult;
using Koralytics.Application.Services.Drill.DrillSession;
using Koralytics.Application.Services.Drill.DrillTemplate;
using Koralytics.Domain.Entities.Drill;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Koralytics.API.Controllers.Drill
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DrillsController : ControllerBase
    {
        private readonly IDrillTemplateService _templateService;
        private readonly IDrillSessionService _sessionService;
        private readonly IDrillResultService _resultService;
        private readonly IDrillAnalyticsService _analyticsService;

        public DrillsController(
            IDrillTemplateService templateService,
            IDrillSessionService sessionService,
            IDrillResultService resultService,
            IDrillAnalyticsService analyticsService)
        {
            _templateService = templateService;
            _sessionService = sessionService;
            _resultService = resultService;
            _analyticsService = analyticsService;
        }

        // ==========================================
        // 🟢 REAL JWT CLAIMS EXTRACTOR
        // ==========================================
        private (int UserId, string Role, int? AcademyId) GetCurrentUserClaims()
        {
            // 1. Extract User ID
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("id") ?? User.FindFirstValue("uid");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                throw new UnauthorizedAccessException("Invalid or missing User ID in token.");
            }

            // 2. Extract Role
            var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? string.Empty;

            // 3. Extract AcademyId
            var academyIdString = User.FindFirstValue("AcademyId") ?? User.FindFirstValue("academyId");
            int? academyId = null;
            if (!string.IsNullOrEmpty(academyIdString) && int.TryParse(academyIdString, out int parsedAcademyId))
            {
                academyId = parsedAcademyId;
            }

            return (userId, role, academyId);
        }

        // ==========================================
        // 1. TEMPLATE ENDPOINTS (/api/drills/templates)
        // ==========================================

        [HttpPost("templates")]
        public async Task<IActionResult> CreateTemplate([FromBody] CreateDrillTemplateDto dto)
        {
            var claims = GetCurrentUserClaims();
            var result = await _templateService.CreateTemplateAsync(dto, claims.UserId, claims.Role, claims.AcademyId);

            return CreatedAtAction(nameof(GetTemplates), new { }, result);
        }

        [HttpGet("templates")]
        public async Task<IActionResult> GetTemplates([FromQuery] TemplateFilterDto filter)
        {
            var claims = GetCurrentUserClaims();
            int currentAcademyId = claims.AcademyId ?? 0;

            var results = await _templateService.GetTemplatesAsync(currentAcademyId, claims.UserId, filter);

            return Ok(results);
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetDrillCategories()
        {
            var categories = await _templateService.GetCategoriesAsync();
            return Ok(categories);
        }

        [HttpGet("templates/category/{categoryId}")]
        public async Task<IActionResult> GetTemplatesByCategory(int categoryId, [FromQuery] TemplateFilterDto filter)
        {
            var claims = GetCurrentUserClaims();
            int currentAcademyId = claims.AcademyId ?? 0;

            var results = await _templateService.GetTemplatesByCategoryAsync(categoryId, currentAcademyId, claims.UserId, filter);

            return Ok(results);
        }

        [HttpPatch("templates/{id}/share")]
        public async Task<IActionResult> ShareTemplate(int id)
        {
            var claims = GetCurrentUserClaims();
            await _templateService.ShareTemplateAsync(id, claims.UserId, claims.Role, claims.AcademyId);

            return Ok(new { message = "Template successfully shared with the academy." });
        }

        [HttpGet("templates/{id}")]
        public async Task<IActionResult> GetTemplateById(int id)
        {
            var claims = GetCurrentUserClaims();
            var result = await _templateService.GetTemplateByIdAsync(id, claims.UserId, claims.AcademyId);

            return Ok(result);
        }

        [HttpPut("templates/{id}")]
        public async Task<IActionResult> UpdateTemplate(int id, [FromBody] UpdateDrillTemplateDto dto)
        {
            var claims = GetCurrentUserClaims();
            var result = await _templateService.UpdateTemplateAsync(id, dto, claims.UserId, claims.Role, claims.AcademyId);

            return Ok(result);
        }

        [HttpDelete("templates/{id}")]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            var claims = GetCurrentUserClaims();
            await _templateService.DeleteTemplateAsync(id, claims.UserId, claims.Role, claims.AcademyId);

            return NoContent();
        }

        // ==========================================
        // 2. SESSION ENDPOINTS (/api/drills/sessions)
        // ==========================================

        [HttpPost("sessions")]
        public async Task<IActionResult> CreateSession([FromBody] CreateDrillSessionDto dto)
        {
            var claims = GetCurrentUserClaims();
            int currentAcademyId = claims.AcademyId ?? 0;

            var result = await _sessionService.CreateSessionAsync(dto, claims.UserId, currentAcademyId);
            return CreatedAtAction(nameof(GetSessionById), new { sessionId = result.Id }, result);
        }

            [HttpGet("sessions")]
            public async Task<IActionResult> GetCoachSessions([FromQuery] SessionFilterDto filter)
            {
                var claims = GetCurrentUserClaims();
                int currentAcademyId = claims.AcademyId ?? 0;

                // 🟢 Use built-in ASP.NET Role checking
                bool isAdmin = User.IsInRole("AcademyAdmin") || User.IsInRole("Admin") || User.IsInRole("SystemAdmin");
                string roleToPass = isAdmin ? "AcademyAdmin" : "Coach";

                var results = await _sessionService.GetCoachSessionsAsync(claims.UserId, roleToPass, currentAcademyId, filter);
                return Ok(results);
            }
 

        [HttpGet("sessions/{sessionId}")]
        public async Task<IActionResult> GetSessionById(int sessionId)
        {
            var claims = GetCurrentUserClaims();
            int currentAcademyId = claims.AcademyId ?? 0;

            // 🟢 UPDATED: Passing claims.Role and currentAcademyId to the service
            var result = await _sessionService.GetSessionByIdAsync(sessionId, claims.UserId, claims.Role, currentAcademyId);
            return Ok(result);
        }

        [HttpPut("sessions/{sessionId}")]
        public async Task<IActionResult> UpdateSession(int sessionId, [FromBody] UpdateDrillSessionDto dto)
        {
            var claims = GetCurrentUserClaims();
            var result = await _sessionService.UpdateSessionAsync(sessionId, dto, claims.UserId);
            return Ok(result);
        }

        [HttpDelete("sessions/{sessionId}")]
        public async Task<IActionResult> DeleteSession(int sessionId)
        {
            var claims = GetCurrentUserClaims();
            await _sessionService.DeleteSessionAsync(sessionId, claims.UserId);
            return NoContent();
        }

        [HttpPatch("sessions/{sessionId}/complete")]
        public async Task<IActionResult> CompleteSession(int sessionId)
        {
            var claims = GetCurrentUserClaims();
            await _sessionService.CompleteSessionAsync(sessionId, claims.UserId);

            return Ok(new { message = "Session marked as completed. Analytics cache invalidated." });
        }

        [HttpPost("sessions/{sessionId}/drills")]
        public async Task<IActionResult> AddDrillToSession(int sessionId, [FromBody] AddSessionDrillDto dto)
        {
            var claims = GetCurrentUserClaims();
            var result = await _sessionService.AddDrillToSessionAsync(sessionId, dto, claims.UserId);
            return Ok(result);
        }

        [HttpDelete("sessions/{sessionId}/drills/{drillId}")]
        public async Task<IActionResult> RemoveDrillFromSession(int sessionId, int drillId)
        {
            var claims = GetCurrentUserClaims();
            await _sessionService.RemoveDrillFromSessionAsync(sessionId, drillId, claims.UserId);
            return NoContent();
        }

        // ==========================================
        // 3. ATTENDANCE & RESULTS ENDPOINTS
        // ==========================================

        [HttpGet("sessions/{sessionId}/attendance")]
        public async Task<IActionResult> GetSessionAttendance(int sessionId)
        {
            var claims = GetCurrentUserClaims();
            var roster = await _resultService.GetSessionAttendanceAsync(sessionId, claims.UserId);
            return Ok(roster);
        }

        [HttpPut("sessions/{sessionId}/attendance")]
        public async Task<IActionResult> MarkAttendance(int sessionId, [FromBody] UpdateSessionAttendanceDto dto)
        {
            var claims = GetCurrentUserClaims();
            await _resultService.MarkAttendanceAsync(sessionId, dto, claims.UserId);

            return Ok(new { message = "Attendance updated successfully." });
        }

        [HttpGet("sessions/{sessionId}/drills/{drillId}/results")]
        public async Task<IActionResult> GetDrillResults(int sessionId, int drillId)
        {
            var claims = GetCurrentUserClaims();
            var existingResults = await _resultService.GetDrillResultsAsync(sessionId, drillId, claims.UserId);
            return Ok(existingResults);
        }

        [HttpPost("sessions/{sessionId}/drills/{drillId}/results")]
        public async Task<IActionResult> SubmitDrillResults(int sessionId, int drillId, [FromBody] SubmitDrillResultsDto dto)
        {
            var claims = GetCurrentUserClaims();
            await _resultService.SubmitResultsAsync(sessionId, drillId, dto, claims.UserId);

            return Ok(new { message = "Drill results submitted successfully." });
        }

        [HttpGet("players/{playerId}/progression/category/{categoryId}")]
        public async Task<IActionResult> GetPlayerProgression(int playerId, int categoryId)
        {
            var claims = GetCurrentUserClaims();
            int currentAcademyId = claims.AcademyId ?? 0;

            var result = await _resultService.GetPlayerDrillProgressionAsync(playerId, categoryId, currentAcademyId);

            return Ok(result);
        }

        // ====================================================================
        // 4. ANALYTICS ENDPOINTS
        // ====================================================================

        [HttpGet("analytics/teams/{teamId}/weak-categories")]
        public async Task<IActionResult> GetSquadWeakCategories(int teamId)
        {
            var report = await _analyticsService.GetSquadWeakCategoriesAsync(teamId);
            return Ok(report);
        }

        [HttpPost("coaches/{coachId}/bias/calculate")]
        public async Task<IActionResult> GetCoachBiasReport(int coachId)
        {
            // 1. Extract Identity
            var claims = GetCurrentUserClaims();

            // 2. Delegate to Service
            var biasReport = await _analyticsService.DetectCoachBiasAsync(
                targetCoachId: coachId,
                academyId: claims.AcademyId ?? 0,
                currentUserId: claims.UserId,
                currentUserRole: claims.Role
            );

            // 3. Return
            return Ok(biasReport);
        }
    }
}