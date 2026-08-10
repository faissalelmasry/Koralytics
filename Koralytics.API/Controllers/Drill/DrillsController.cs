using Koralytics.Application.DTOs.Drill;
using Koralytics.Application.Services.Drill.DrillAnalytic;
using Koralytics.Application.Services.Drill.DrillResult;
using Koralytics.Application.Services.Drill.DrillSession;
using Koralytics.Application.Services.Drill.DrillTemplate;
using Koralytics.Application.Interfaces.Subscription;
using Koralytics.API.Filters;
using Koralytics.Domain.Enums;
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
        private readonly ITenantSubscriptionService _tenantSubscriptionService;

        public DrillsController(
            IDrillTemplateService templateService,
            IDrillSessionService sessionService,
            IDrillResultService resultService,
            IDrillAnalyticsService analyticsService,
            ITenantSubscriptionService tenantSubscriptionService)
        {
            _templateService = templateService;
            _sessionService = sessionService;
            _resultService = resultService;
            _analyticsService = analyticsService;
            _tenantSubscriptionService = tenantSubscriptionService;
        }

        // ==========================================
        // 🟢 OPTIMIZED: Centralized Identity & Role Resolution
        // ==========================================
        private (int UserId, string ResolvedRole, int AcademyId) GetUserIdentity()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("id") ?? User.FindFirstValue("uid");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                throw new UnauthorizedAccessException("Invalid or missing User ID in token.");
            }

            var academyIdString = User.FindFirstValue("AcademyId") ?? User.FindFirstValue("academyId");
            int academyId = int.TryParse(academyIdString, out int parsedAcademyId) ? parsedAcademyId : 0;

            string resolvedRole;
            if (User.IsInRole("SystemAdmin")) resolvedRole = "SystemAdmin";
            else if (User.IsInRole("AcademyAdmin") || User.IsInRole("Admin")) resolvedRole = "AcademyAdmin";
            else resolvedRole = User.FindFirstValue(ClaimTypes.Role) ?? "Coach";

            return (userId, resolvedRole, academyId);
        }

        // ==========================================
        // 1. TEMPLATE ENDPOINTS 
        // ==========================================

        [HttpPost("templates")]
        public async Task<IActionResult> CreateTemplate([FromBody] CreateDrillTemplateDto dto)
        {
            var identity = GetUserIdentity();

            if (identity.AcademyId > 0)
            {
                var currentCount = await _tenantSubscriptionService.CountCustomDrillTemplatesAsync(identity.AcademyId);
                var guard = await CapacityGuard.CheckCustomDrillTemplateLimitAsync(_tenantSubscriptionService, identity.AcademyId, currentCount);

                if (guard != null)
                    return guard;
            }

            var result = await _templateService.CreateTemplateAsync(dto, identity.UserId, identity.ResolvedRole, identity.AcademyId);
            return CreatedAtAction(nameof(GetTemplates), new { }, result);
        }

        [HttpGet("templates")]
        public async Task<IActionResult> GetTemplates([FromQuery] TemplateFilterDto filter)
        {
            var identity = GetUserIdentity();
            var results = await _templateService.GetTemplatesAsync(identity.AcademyId, identity.UserId, filter);
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
            var identity = GetUserIdentity();
            var results = await _templateService.GetTemplatesByCategoryAsync(categoryId, identity.AcademyId, identity.UserId, filter);
            return Ok(results);
        }

        [HttpPatch("templates/{id}/share")]
        public async Task<IActionResult> ShareTemplate(int id)
        {
            var identity = GetUserIdentity();
            await _templateService.ShareTemplateAsync(id, identity.UserId, identity.ResolvedRole, identity.AcademyId);
            return Ok(new { message = "Template successfully shared with the academy." });
        }

        [HttpGet("templates/{id}")]
        public async Task<IActionResult> GetTemplateById(int id)
        {
            var identity = GetUserIdentity();
            var result = await _templateService.GetTemplateByIdAsync(id, identity.UserId, identity.AcademyId);
            return Ok(result);
        }

        [HttpPut("templates/{id}")]
        public async Task<IActionResult> UpdateTemplate(int id, [FromBody] UpdateDrillTemplateDto dto)
        {
            var identity = GetUserIdentity();
            var result = await _templateService.UpdateTemplateAsync(id, dto, identity.UserId, identity.ResolvedRole, identity.AcademyId);
            return Ok(result);
        }

        [HttpDelete("templates/{id}")]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            var identity = GetUserIdentity();
            await _templateService.DeleteTemplateAsync(id, identity.UserId, identity.ResolvedRole, identity.AcademyId);
            return NoContent();
        }

        // ==========================================
        // 2. SESSION ENDPOINTS
        // ==========================================

        [HttpPost("sessions")]
        public async Task<IActionResult> CreateSession([FromBody] CreateDrillSessionDto dto)
        {
            var identity = GetUserIdentity();
            var result = await _sessionService.CreateSessionAsync(dto, identity.UserId, identity.AcademyId);
            return CreatedAtAction(nameof(GetSessionById), new { sessionId = result.Id }, result);
        }

        [HttpGet("sessions")]
        public async Task<IActionResult> GetCoachSessions([FromQuery] SessionFilterDto filter)
        {
            var identity = GetUserIdentity();
            var results = await _sessionService.GetCoachSessionsAsync(identity.UserId, identity.ResolvedRole, identity.AcademyId, filter);
            return Ok(results);
        }

        [HttpGet("sessions/{sessionId}")]
        public async Task<IActionResult> GetSessionById(int sessionId)
        {
            var identity = GetUserIdentity();
            var result = await _sessionService.GetSessionByIdAsync(sessionId, identity.UserId, identity.ResolvedRole, identity.AcademyId);
            return Ok(result);
        }

        [HttpPut("sessions/{sessionId}")]
        public async Task<IActionResult> UpdateSession(int sessionId, [FromBody] UpdateDrillSessionDto dto)
        {
            var identity = GetUserIdentity();
            var result = await _sessionService.UpdateSessionAsync(sessionId, dto, identity.UserId);
            return Ok(result);
        }

        [HttpDelete("sessions/{sessionId}")]
        public async Task<IActionResult> DeleteSession(int sessionId)
        {
            var identity = GetUserIdentity();
            await _sessionService.DeleteSessionAsync(sessionId, identity.UserId);
            return NoContent();
        }

        [HttpPatch("sessions/{sessionId}/complete")]
        public async Task<IActionResult> CompleteSession(int sessionId)
        {
            var identity = GetUserIdentity();
            await _sessionService.CompleteSessionAsync(sessionId, identity.UserId);
            return Ok(new { message = "Session marked as completed. Analytics cache invalidated." });
        }

        [HttpPost("sessions/{sessionId}/drills")]
        public async Task<IActionResult> AddDrillToSession(int sessionId, [FromBody] AddSessionDrillDto dto)
        {
            var identity = GetUserIdentity();
            var result = await _sessionService.AddDrillToSessionAsync(sessionId, dto, identity.UserId);
            return Ok(result);
        }

        [HttpDelete("sessions/{sessionId}/drills/{drillId}")]
        public async Task<IActionResult> RemoveDrillFromSession(int sessionId, int drillId)
        {
            var identity = GetUserIdentity();
            await _sessionService.RemoveDrillFromSessionAsync(sessionId, drillId, identity.UserId);
            return NoContent();
        }

        // ==========================================
        // 3. ATTENDANCE & RESULTS ENDPOINTS
        // ==========================================

        [HttpGet("sessions/{sessionId}/attendance")]
        public async Task<IActionResult> GetSessionAttendance(int sessionId)
        {
            var identity = GetUserIdentity();
            var roster = await _resultService.GetSessionAttendanceAsync(sessionId, identity.UserId, identity.ResolvedRole, identity.AcademyId);
            return Ok(roster);
        }

        [HttpPut("sessions/{sessionId}/attendance")]
        public async Task<IActionResult> MarkAttendance(int sessionId, [FromBody] UpdateSessionAttendanceDto dto)
        {
            var identity = GetUserIdentity();
            await _resultService.MarkAttendanceAsync(sessionId, dto, identity.UserId);
            return Ok(new { message = "Attendance updated successfully." });
        }

        [HttpGet("sessions/{sessionId}/drills/{drillId}/results")]
        public async Task<IActionResult> GetDrillResults(int sessionId, int drillId)
        {
            var identity = GetUserIdentity();
            var existingResults = await _resultService.GetDrillResultsAsync(sessionId, drillId, identity.UserId, identity.ResolvedRole, identity.AcademyId);
            return Ok(existingResults);
        }

        [HttpPost("sessions/{sessionId}/drills/{drillId}/results")]
        public async Task<IActionResult> SubmitDrillResults(int sessionId, int drillId, [FromBody] SubmitDrillResultsDto dto)
        {
            var identity = GetUserIdentity();
            await _resultService.SubmitResultsAsync(sessionId, drillId, dto, identity.UserId);
            return Ok(new { message = "Drill results submitted successfully." });
        }

        [HttpGet("players/{playerId}/progression/category/{categoryId}")]
        [RequiresPlanFeature(TierFeature.ProgressionAnalytics)]
        public async Task<IActionResult> GetPlayerProgression(int playerId, int categoryId)
        {
            var identity = GetUserIdentity();
            var result = await _resultService.GetPlayerDrillProgressionAsync(playerId, categoryId, identity.AcademyId);
            return Ok(result);
        }

        // ====================================================================
        // 4. ANALYTICS ENDPOINTS
        // ====================================================================

        [HttpGet("analytics/teams/{teamId}/weak-categories")]
        [RequiresPlanFeature(TierFeature.SquadWeakness)]
        public async Task<IActionResult> GetSquadWeakCategories(int teamId)
        {
            var report = await _analyticsService.GetSquadWeakCategoriesAsync(teamId);
            return Ok(report);
        }

        [HttpPost("coaches/{coachId}/bias/calculate")]
        [RequiresPlanFeature(TierFeature.AIInsights)]
        public async Task<IActionResult> GetCoachBiasReport(int coachId)
        {
            var identity = GetUserIdentity();
            var biasReport = await _analyticsService.DetectCoachBiasAsync(
                targetCoachId: coachId,
                academyId: identity.AcademyId,
                currentUserId: identity.UserId,
                currentUserRole: identity.ResolvedRole
            );
            return Ok(biasReport);
        }
    }
}