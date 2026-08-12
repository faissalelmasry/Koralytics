using Koralytics.Application.DTOs.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Application.Interfaces.ScouterInterfaces;
using Koralytics.Application.DTOs.ScouterDtos;
using Koralytics.Application.Interfaces;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Entities.Match;
using Microsoft.EntityFrameworkCore;
using AutoMapper.QueryableExtensions;
using AutoMapper;
using Koralytics.Application.Services.Player.Helpers;
using Koralytics.Application.Services.Player.PlayerCardService;
using Koralytics.Application.DTOs.Scouter;
using Koralytics.Domain.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using ScouterEntity = Koralytics.Domain.Entities.Scouter.Scouter;

namespace Koralytics.Application.Services.Scouter.ScouterSearchService
{
    public class ScouterSearchService : IScouterSearchService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ScouterSearchService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ScouterSearchService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<ScouterSearchService> logger,
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<PaginatedResult<PlayerCardDto>> SearchPlayersAsync(PlayerSearchFiltersDto filters)
        {
            _logger.LogInformation("Executing structured player search. Search filters input payload received.");

          
            var query = _unitOfWork.Repository<Domain.Entities.Player.Player>().GetQueryableAsNoTracking()
                .Where(p => _unitOfWork.Repository<PlayerCard>().GetQueryableAsNoTracking().Any(pc => pc.PlayerId == p.Id));

            var today = DateTime.UtcNow.Date;

            if (filters != null)
            {
                if (filters.MinAge.HasValue)
                {
                    var minAge = filters.MinAge.Value;
                    query = query.Where(p => today.Year - p.DateOfBirth.Year -
                        (today.Month < p.DateOfBirth.Month || (today.Month == p.DateOfBirth.Month && today.Day < p.DateOfBirth.Day) ? 1 : 0) >= minAge);
                }

                if (filters.MaxAge.HasValue)
                {
                    var maxAge = filters.MaxAge.Value;
                    query = query.Where(p => today.Year - p.DateOfBirth.Year -
                        (today.Month < p.DateOfBirth.Month || (today.Month == p.DateOfBirth.Month && today.Day < p.DateOfBirth.Day) ? 1 : 0) <= maxAge);
                }

                if (filters.PreferredFoot.HasValue)
                {
                  
                    int footValue = filters.PreferredFoot.Value;
                    query = query.Where(p => (int)p.PreferredFoot == footValue);
                }

                if (filters.Positions != null && filters.Positions.Any())
                {
                    var positions = filters.Positions;
                    query = query.Where(p => p.PlayerPositions.Any(pp => positions.Contains(pp.Position)));
                }

                if (filters.AcademyId.HasValue)
                {
                    var academyId = filters.AcademyId.Value;
                    query = query.Where(p => p.PlayerAcademies.Any(pa => pa.AcademyId == academyId && pa.LeftAt == null));
                }

                if (filters.Format.HasValue)
                {
                    var format = filters.Format.Value;
                    query = query.Where(p => _unitOfWork.Repository<MatchLineup>().GetQueryableAsNoTracking().Any(ml => ml.PlayerId == p.Id && ml.Match.Format == format));
                }

                if (filters.MinRating.HasValue)
                {
                    var minRating = filters.MinRating.Value;
                    query = query.Where(p => _unitOfWork.Repository<PlayerCard>().GetQueryableAsNoTracking()
                        .Any(pc => pc.PlayerId == p.Id && pc.OverallRating >= minRating));
                }

                if (filters.MaxRating.HasValue)
                {
                    var maxRating = filters.MaxRating.Value;
                    query = query.Where(p => _unitOfWork.Repository<PlayerCard>().GetQueryableAsNoTracking()
                        .Any(pc => pc.PlayerId == p.Id && pc.OverallRating <= maxRating));
                }
            }

            int totalCount = await query.CountAsync();

            if (totalCount == 0 || filters == null)
            {
                _logger.LogInformation("No players matched the specified search matrix filters.");
                return new PaginatedResult<PlayerCardDto>
                {
                    Items = new List<PlayerCardDto>(),
                    TotalCount = 0,
                    PageNumber = filters?.PageNumber ?? 1,
                    PageSize = filters?.PageSize ?? 10
                };
            }

            var playerCardQuery = _unitOfWork.Repository<PlayerCard>().GetQueryableAsNoTracking();

            // Step 1: Get only the paginated Player IDs (Can easily be translated to SQL)
            var paginatedPlayerIds = await query
                .OrderByDescending(p => p.Id)
                .Skip((filters.PageNumber - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .Select(p => p.Id)
                .ToListAsync();

            // Step 2: Fetch the PlayerCards for those IDs and Project directly
            var validDtos = await playerCardQuery
                .Where(pc => paginatedPlayerIds.Contains(pc.PlayerId))
                .ProjectTo<PlayerCardDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            // Step 3: Re-order them descending by Id to match the original pagination order
            validDtos = validDtos.OrderByDescending(pc => pc.PlayerId).ToList();

            _logger.LogInformation("Successfully completed player search execution. Returned {Count} item records.", validDtos.Count);

            return new PaginatedResult<PlayerCardDto>
            {
                Items = validDtos,
                TotalCount = totalCount,
                PageNumber = filters.PageNumber,
                PageSize = filters.PageSize
            };
        }

        public async Task<ScouterProfileDto> GetScouterByIdAsync(int scouterId)
        {
            _logger.LogInformation("Retrieving profile for ScouterId: {ScouterId}", scouterId);
            var scouterDto = await _unitOfWork.Repository<ScouterEntity>()
                .GetQueryableAsNoTracking()
                .Where(s => s.Id == scouterId)
                .ProjectTo<ScouterProfileDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            if (scouterDto == null)
            {
                throw new NotFoundException($"Scouter with ID {scouterId} was not found.");
            }

            _logger.LogInformation("Successfully loaded profile for ScouterId: {ScouterId}", scouterId);
            return scouterDto;
        }

        public async Task<string> AIChatBotAsync(AIChatBotRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                throw new ArgumentException("Chat message cannot be empty.", nameof(request));
            }

            try
            {
                var baseUrl = "https://koralytics-langflow.happymeadow-f8cd49ac.centralus.azurecontainerapps.io/";
                var flowId = "bb77f971-eb7c-4e75-95fc-43064223a281";
                var apiKey = _configuration["Langflow:ScouterApiKey"];

                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    _logger.LogError("Langflow:BaseUrl is missing from configuration.");
                    throw new InvalidOperationException("Langflow:BaseUrl is not configured.");
                }

                if (string.IsNullOrWhiteSpace(flowId))
                {
                    _logger.LogError("Langflow:ScouterFlowId is missing from configuration.");
                    throw new InvalidOperationException("Langflow:ScouterFlowId is not configured.");
                }

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    _logger.LogError("Langflow:ScouterApiKey is missing from configuration.");
                    throw new InvalidOperationException("Langflow:ScouterApiKey is not configured.");
                }

                _logger.LogInformation(
                    "Langflow configuration loaded successfully. BaseUrl: {BaseUrl}, FlowId: {FlowId}",
                    baseUrl,
                    flowId);

                _logger.LogInformation("Sending request to Scouter Langflow AI ChatBot (FlowId: {FlowId})...", flowId);

                var sessionId = !string.IsNullOrWhiteSpace(request.SessionId)
                    ? request.SessionId
                    : Guid.NewGuid().ToString();

                var requestPayload = new
                {
                    input_value = request.Message,
                    input_type = "chat",
                    output_type = "chat",
                    session_id = sessionId
                };

                var httpRequestMessage = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{baseUrl.TrimEnd('/')}/api/v1/run/{flowId}?stream=false")
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(requestPayload),
                        Encoding.UTF8,
                        "application/json"
                    )
                };

                if (!string.IsNullOrEmpty(apiKey))
                {
                    httpRequestMessage.Headers.Add("x-api-key", apiKey);
                }

                var response = await _httpClient.SendAsync(httpRequestMessage);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Scouter Langflow API failed with status {StatusCode}: {Error}", response.StatusCode, errorContent);
                    return "عذراً، حدث خطأ أثناء التواصل مع المساعد الذكي. يرجى المحاولة مرة أخرى لاحقاً.";
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                using var jsonDoc = JsonDocument.Parse(responseBody);

                string botReply = jsonDoc.RootElement
                    .GetProperty("outputs")[0]
                    .GetProperty("outputs")[0]
                    .GetProperty("results")
                    .GetProperty("message")
                    .GetProperty("text")
                    .GetString() ?? string.Empty;

                _logger.LogInformation("Scouter Langflow AI ChatBot response generated successfully.");

                return string.IsNullOrWhiteSpace(botReply)
                    ? "عذراً، لم يتم استلام رد من المساعد الذكي. يرجى المحاولة مرة أخرى."
                    : botReply;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while calling Scouter AI ChatBot.");
                return "عذراً، حدث خطأ أثناء التواصل مع المساعد الذكي. يرجى المحاولة مرة أخرى لاحقاً.";
            }
        }
    }
}