using Koralytics.Application.DTOs.Parent;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Services.Parent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Koralytics.Api.Controllers
{
    [ApiController]
    [Authorize(Roles = "Parent")]
    [Route("api/koralytics/parent-assistant")]
    public class ParentAssistantController : ControllerBase
    {
        private readonly IParentAiService _aiService;
        private readonly IParentPlayerAccessService _accessService;
        private readonly ILogger<ParentAssistantController> _logger;

        public ParentAssistantController(
            IParentAiService aiService,
            IParentPlayerAccessService accessService,
            ILogger<ParentAssistantController> logger)
        {
            _aiService = aiService;
            _accessService = accessService;
            _logger = logger;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> SendParentQuery(
            [FromBody] ParentChatRequest request,
            CancellationToken cancellationToken)
        {
            var validation = await ValidateAndResolveAsync(request);
            if (validation.ErrorResult != null)
            {
                return validation.ErrorResult;
            }

            try
            {
                var result = await _aiService.ProcessParentQueryAsync(
                    request,
                    validation.ParentUserId!,
                    validation.AuthorizedPlayerIds!,
                    cancellationToken);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing Koralytics parent assistant query for parent {ParentUserId}",
                    validation.ParentUserId);

                return StatusCode(500, new
                {
                    message = "Error processing Koralytics parent assistant query."
                });
            }
        }

        [HttpPost("chat/stream")]
        public async Task ChatStream(
            [FromBody] ParentChatRequest request,
            CancellationToken cancellationToken)
        {
            var validation = await ValidateAndResolveAsync(request);
            if (validation.ErrorResult != null)
            {
                Response.StatusCode = validation.StatusCode;
                await Response.WriteAsync(validation.ErrorMessage ?? "Unauthorized");
                return;
            }

            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("X-Accel-Buffering", "no");

            try
            {
                await foreach (var chunk in _aiService.StreamParentQueryAsync(
                    request,
                    validation.ParentUserId!,
                    validation.AuthorizedPlayerIds!,
                    cancellationToken))
                {
                    await Response.WriteAsync($"data: {chunk}\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error streaming Koralytics parent assistant query for parent {ParentUserId}",
                    validation.ParentUserId);

                var errorPayload = "{\"eventType\":\"error\",\"text\":\"عذراً، حدث خطأ أثناء الوصول إلى بيانات الأكاديمية. يرجى المحاولة مرة أخرى لاحقاً.\"}";
                await Response.WriteAsync($"data: {errorPayload}\n\n", cancellationToken);
            }
        }

        private async Task<ValidationResult> ValidateAndResolveAsync(ParentChatRequest? request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return ValidationResult.Fail(BadRequest(new { message = "Message content cannot be empty." }),
                    400, "Message content cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(request.SessionId))
            {
                return ValidationResult.Fail(BadRequest(new { message = "Session ID is required." }),
                    400, "Session ID is required.");
            }

            var parentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(parentUserId))
            {
                return ValidationResult.Fail(Unauthorized(), 401, "Unauthorized");
            }

            IReadOnlyList<int> authorizedPlayerIds;
            try
            {
                authorizedPlayerIds = await _accessService.GetAuthorizedPlayerIdsAsync(parentUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed resolving authorized players for parent {ParentUserId}", parentUserId);
                return ValidationResult.Fail(StatusCode(500, new { message = "Unable to verify player access." }),
                    500, "Unable to verify player access.");
            }

            if (authorizedPlayerIds == null || authorizedPlayerIds.Count == 0)
            {
                return ValidationResult.Fail(Forbid(), 403, "Forbidden");
            }

            if (request!.RequestedPlayerId.HasValue)
            {
                if (!authorizedPlayerIds.Contains(request.RequestedPlayerId.Value))
                {
                    return ValidationResult.Fail(Forbid(), 403, "Forbidden");
                }

                return ValidationResult.Ok(parentUserId, new List<int> { request.RequestedPlayerId.Value });
            }

            return ValidationResult.Ok(parentUserId, authorizedPlayerIds);
        }

        private class ValidationResult
        {
            public IActionResult? ErrorResult { get; private set; }
            public int StatusCode { get; private set; }
            public string? ErrorMessage { get; private set; }
            public string? ParentUserId { get; private set; }
            public IReadOnlyList<int>? AuthorizedPlayerIds { get; private set; }

            public static ValidationResult Fail(IActionResult result, int statusCode, string message) =>
                new ValidationResult { ErrorResult = result, StatusCode = statusCode, ErrorMessage = message };

            public static ValidationResult Ok(string parentUserId, IReadOnlyList<int> authorizedPlayerIds) =>
                new ValidationResult { ParentUserId = parentUserId, AuthorizedPlayerIds = authorizedPlayerIds };
        }
    }
}