using System.Text;
using System.Text.Json;
using Koralytics.Application.DTOs.Match;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Match;
using Koralytics.Domain.Entities.AI;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Match
{
    public class MatchReportService : IMatchReportService
    {
        private readonly HttpClient _httpClient;
        private readonly IMatchService _matchService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MatchReportService> _logger;

        public MatchReportService(
            HttpClient httpClient,
            IMatchService matchService,
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            ILogger<MatchReportService> logger)
        {
            _httpClient = httpClient;
            _matchService = matchService;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> GenerateMatchReportAsync(int matchId)
        {
            _logger.LogInformation("Fetching combined details for match {MatchId}...", matchId);

            var combinedDetails = await _matchService.GetCombinedMatchDetailsAsync(matchId);

            if (combinedDetails == null)
                throw new NotFoundException($"Match details not found for MatchId {matchId}");

            string matchJsonString = JsonSerializer.Serialize(combinedDetails);

            var flowId = _configuration["Langflow:FlowId"];
            var promptNodeId = _configuration["Langflow:PromptNodeId"] ?? "Prompt Template-8hQ9i";

            var requestPayload = new
            {
                input_type = "chat",
                output_type = "chat",
                tweaks = new Dictionary<string, object>
                {
                    {
                        promptNodeId, new
                        {
                            match_data = matchJsonString
                        }
                    }
                }
            };

            var requestContent = new StringContent(
                JsonSerializer.Serialize(requestPayload),
                Encoding.UTF8,
                "application/json"
            );

            _logger.LogInformation("Sending match data payload to Langflow (Flow: {FlowId}, Node: {NodeId})...", flowId, promptNodeId);

            var response = await _httpClient.PostAsync($"api/v1/run/{flowId}?stream=false", requestContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Langflow API failed with status {StatusCode}: {Error}", response.StatusCode, errorContent);
                response.EnsureSuccessStatusCode();
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(responseBody);

            string generatedReport = jsonDoc.RootElement
                .GetProperty("outputs")[0]
                .GetProperty("outputs")[0]
                .GetProperty("results")
                .GetProperty("message")
                .GetProperty("text")
                .GetString() ?? string.Empty;

            _logger.LogInformation("Report generated successfully. Saving AIReport entity for match {MatchId}...", matchId);

            var aiReport = new AIReport
            {
                ReportType = AIReportType.Match, 
                ReferenceId = matchId,           
                ReportText = generatedReport
            };

            await _unitOfWork.Repository<AIReport>().AddAsync(aiReport);
            await _unitOfWork.SaveChangesAsync();

            return generatedReport;
        }

        public async Task<AIReportResponseDto> GetMatchReportAsync(int referenceId, AIReportType reportType = AIReportType.Match)
        {
            _logger.LogInformation("Fetching AI report for ReferenceId {ReferenceId} and ReportType {ReportType}...", referenceId, reportType);

            var report = await _unitOfWork.Repository<AIReport>()
                .GetQueryableAsNoTracking()
                .Where(r => r.ReferenceId == referenceId && r.ReportType == reportType)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            if (report == null)
                throw new NotFoundException($"AI report not found for ReferenceId {referenceId} with type {reportType}.");

            return new AIReportResponseDto
            {
                Id = report.Id,
                ReportType = report.ReportType,
                ReferenceId = report.ReferenceId,
                AcademyId = report.AcademyId,
                ReportText = report.ReportText,
                CreatedAt = report.CreatedAt
            };
        }
    }
}