using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AutoMapper;
using Koralytics.Application.DTOs.Player;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Options;
using Koralytics.Application.Services.Player.Helpers;
using Koralytics.Domain.Entities.Drill;
using Koralytics.Domain.Entities.Match;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlayerEntity = Koralytics.Domain.Entities.Player.Player;

namespace Koralytics.Application.Services.Player.PlayerCardService
{
    public class PlayerCardService : IPlayerCardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PlayerCardService> _logger;
        private readonly IMapper _mapper;
        private readonly ICardInvalidationList _invalidationList;
        private readonly HttpClient _httpClient;
        private readonly ItiChatOptions _itiChatOptions;

        public PlayerCardService(
            IUnitOfWork unitOfWork,
            ILogger<PlayerCardService> logger,
            IMapper mapper,
            ICardInvalidationList invalidationList,
            HttpClient httpClient,
            IOptions<ItiChatOptions> itiChatOptions)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
            _invalidationList = invalidationList;
            _httpClient = httpClient;
            _itiChatOptions = itiChatOptions.Value;

            if (_httpClient.BaseAddress is null && !string.IsNullOrWhiteSpace(_itiChatOptions.BaseUrl))
            {
                _httpClient.BaseAddress = new Uri(_itiChatOptions.BaseUrl);
            }

            if (!_httpClient.DefaultRequestHeaders.Contains("Authorization") && !string.IsNullOrWhiteSpace(_itiChatOptions.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _itiChatOptions.ApiKey);
            }
        }

        public async Task<PlayerCardDto> GetPlayerCardAsync(int playerId)
        {
            _logger.LogInformation("Fetching player card for player {PlayerId}", playerId);

            // Check the invalidation list BEFORE running any DB query.
            // If the card is stale we skip the expensive eager-load entirely and go
            // straight to recalculate → project, saving one heavy round-trip.
            if (_invalidationList.TryConsume(playerId))
            {
                _logger.LogInformation(
                    "Player card for player {PlayerId} is invalidated — recalculating before serving",
                    playerId);

                await RecalculatePlayerCardAsync(playerId);

                var dto = await ProjectPlayerCardDtoAsync(playerId);
                if (dto is null)
                    throw new NotFoundException($"Player card for player {playerId} was not found");

                return dto;
            }

            // Happy path: card exists and is fresh — one query with all needed includes.
            var playerCard = await _unitOfWork.Repository<PlayerCard>()
                .GetQueryableAsNoTracking()
                .Include(pc => pc.Player)
                    .ThenInclude(p => p.PlayerPositions)
                .Include(pc => pc.CategoryRatings)
                    .ThenInclude(cr => cr.DrillCategory)
                .FirstOrDefaultAsync(pc => pc.PlayerId == playerId);

            // First-time calculation: no card exists yet for this player.
            if (playerCard is null)
            {
                _logger.LogInformation(
                    "No player card found for player {PlayerId} — calculating for the first time",
                    playerId);

                await RecalculatePlayerCardAsync(playerId);

                var dto = await ProjectPlayerCardDtoAsync(playerId);
                if (dto is null)
                    throw new NotFoundException($"Player card for player {playerId} was not found");

                return dto;
            }

            return MapToDto(playerCard);
        }

        public async Task RecalculatePlayerCardAsync(int playerId)
        {
            _logger.LogInformation(
                "Recalculating player card for player {PlayerId}",
                playerId);

            var (existingCard, primaryPosition) = await GetCardAndPositionAsync(playerId);
            var targetCategories = GetTargetCategories(primaryPosition);

            var categoryDrillAvgs = await GetDrillAggregatesAsync(
                playerId,
                targetCategories);

            // Single round-trip for both training and tournament ratings.
            // Splitting happens in memory after the fetch (data set is small per player).
            var (trainingMatchCategoryAvgs, tournamentMatchCategoryAvgs) =
                await GetMatchAggregatesAsync(playerId, targetCategories);

            var ratingLookups = BuildRatingLookups(
                categoryDrillAvgs,
                trainingMatchCategoryAvgs,
                tournamentMatchCategoryAvgs);

            var playerCard = existingCard ?? new PlayerCard
            {
                PlayerId = playerId
            };

            var categoryNamesById = categoryDrillAvgs
                .Concat(trainingMatchCategoryAvgs)
                .Concat(tournamentMatchCategoryAvgs)
                .GroupBy(x => x.CategoryId)
                .ToDictionary(g => g.Key, g => g.First().Name);

            PlayerCardCalculator.UpdateCategoryRatings(
                playerCard,
                ratingLookups);

            PlayerCardCalculator.UpdateOverallRating(
                playerCard,
                primaryPosition,
                categoryNamesById);

            PlayerCardCalculator.UpdateOverallAverages(
                playerCard,
                categoryDrillAvgs,
                trainingMatchCategoryAvgs,
                tournamentMatchCategoryAvgs);

            PlayerCardCalculator.UpdateTransferClassification(
                playerCard,
                categoryDrillAvgs,
                trainingMatchCategoryAvgs,
                tournamentMatchCategoryAvgs);

            playerCard.LastCalculatedAt = DateTime.UtcNow;
            playerCard.NeedsRecalculation = false;

            await SavePlayerCardAsync(existingCard, playerCard);

            _logger.LogInformation(
                "Player card recalculated for player {PlayerId}. Overall: {Rating}, Classification: {Class}",
                playerId,
                playerCard.OverallRating,
                playerCard.TransferClassification);
        }

        public async Task<TransferRateDto?> GetDrillToMatchTransferRateAsync(int playerId)
        {
            _logger.LogInformation("Fetching transfer rate for player {PlayerId}", playerId);

            var playerCard = await _unitOfWork.Repository<PlayerCard>()
                .GetQueryableAsNoTracking()
                .Include(pc => pc.Player)
                .FirstOrDefaultAsync(pc => pc.PlayerId == playerId);

            if (playerCard is not null)
                return _mapper.Map<TransferRateDto>(playerCard);

            var playerExists = await _unitOfWork.Repository<PlayerEntity>()
                .ExistsAsync(p => p.Id == playerId);

            if (!playerExists)
                throw new NotFoundException($"Player with Id {playerId} not found");

            return null;
        }

        public async Task<List<MiniPlayerCardDto?>> GetMiniPlayerCardsAsync(int[] playerIds)
        {
            _logger.LogInformation("Fetching mini player cards for {Count} players", playerIds.Length);

            if (playerIds.Length == 0)
                return new List<MiniPlayerCardDto?>();

            var cards = await _unitOfWork.Repository<PlayerCard>()
                .GetQueryableAsNoTracking()
                .Where(pc => playerIds.Contains(pc.PlayerId))
                .Select(pc => new MiniPlayerCardDto
                {
                    PlayerId = pc.PlayerId,
                    FullName = pc.Player.FirstName + " " + pc.Player.LastName,
                    Position = pc.Player.PlayerPositions
                        .Where(pp => pp.IsPrimary)
                        .Select(pp => pp.Position)
                        .FirstOrDefault() ?? string.Empty,
                    ProfileImageUrl = pc.Player.ProfileImageUrl,
                    OverallRating = pc.OverallRating
                })
                .ToDictionaryAsync(pc => pc.PlayerId);

            return playerIds
                .Select(id => cards.TryGetValue(id, out var card) ? card : null)
                .ToList();
        }

        public async Task<PlayerArchetypeDto> RevealArchetypeNameAsync(int playerId)
        {
            _logger.LogInformation("Revealing archetype name for player {PlayerId}", playerId);

            // Load PlayerCard with its Player (tracked, for saving archetype fields later)
            // in a single round-trip. Previously: loaded player entity alone, then called
            // GetPlayerCardAsync which loaded Player + Card + Positions + CategoryRatings again
            // internally — two DB queries returning the same data.
            var playerCard = await _unitOfWork.Repository<PlayerCard>()
                .GetQueryable()                          // tracked — Player.Archetype* fields are modified below
                .Include(pc => pc.Player)
                    .ThenInclude(p => p.PlayerPositions)
                .Include(pc => pc.CategoryRatings)
                    .ThenInclude(cr => cr.DrillCategory)
                .FirstOrDefaultAsync(pc => pc.PlayerId == playerId);

            if (playerCard is null)
            {
                // No card exists yet: verify the player exists, run first-time
                // calculation, then re-read with all required navigations.
                var playerEntity = await _unitOfWork.Repository<PlayerEntity>()
                    .GetQueryable()
                    .FirstOrDefaultAsync(p => p.Id == playerId);

                if (playerEntity is null)
                    throw new NotFoundException($"Player with id {playerId} was not found");

                await RecalculatePlayerCardAsync(playerId);

                playerCard = await _unitOfWork.Repository<PlayerCard>()
                    .GetQueryable()
                    .Include(pc => pc.Player)
                        .ThenInclude(p => p.PlayerPositions)
                    .Include(pc => pc.CategoryRatings)
                        .ThenInclude(cr => cr.DrillCategory)
                    .FirstOrDefaultAsync(pc => pc.PlayerId == playerId)
                    ?? throw new NotFoundException($"Player card for player {playerId} was not found");
            }

            // Extract the tracked player and build the DTO from the already-loaded card —
            // no extra GetPlayerCardAsync call or duplicate DB round-trip.
            var player  = playerCard.Player;
            var cardDto = MapToDto(playerCard);

            if (player.ArchetypeLastRevealedAt.HasValue &&
                (DateTime.UtcNow - player.ArchetypeLastRevealedAt.Value) < TimeSpan.FromDays(7) &&
                !string.IsNullOrWhiteSpace(player.ArchetypePlayerName))
            {
                _logger.LogInformation(
                    "Archetype for player {PlayerId} was revealed less than 7 days ago on {LastRevealed}. Returning existing cached archetype.",
                    playerId,
                    player.ArchetypeLastRevealedAt.Value);

                return new PlayerArchetypeDto
                {
                    PlayerId            = playerId,
                    ArchetypePlayerName = player.ArchetypePlayerName,
                    ArchetypeText       = player.ArchetypeText ?? string.Empty,
                    ArchetypeLastRevealedAt = player.ArchetypeLastRevealedAt
                };
            }

            var isGoalkeeper = string.Equals(cardDto.Position, "GK", StringComparison.OrdinalIgnoreCase);

            string categoryStatsText = isGoalkeeper
                ? $"Goalkeeping: {cardDto.GoalkeepingRating ?? 0:F1}"
                : $"Pace/Speed: {cardDto.PaceRating ?? 0:F1}, Dribbling: {cardDto.DribblingRating ?? 0:F1}, Shooting: {cardDto.ShootingRating ?? 0:F1}, Passing: {cardDto.PassingRating ?? 0:F1}, Defending: {cardDto.DefendingRating ?? 0:F1}, Physicality: {cardDto.PhysicalRating ?? 0:F1}";

            var isElite = cardDto.OverallRating >= 80;
            var exactRating = (int)Math.Round(cardDto.OverallRating);

            var minRating = isElite ? exactRating : Math.Max(45, (int)Math.Floor(cardDto.OverallRating - 4));
            var maxRating = isElite ? exactRating : Math.Min(99, (int)Math.Ceiling(cardDto.OverallRating + 4));

            var ratingConstraintText = isElite
                ? $"EXACT CARD RATING: MUST BE EXACTLY {exactRating} OVERALL (ELITE SUPERSTAR LEVEL)."
                : $"EGYPTIAN PRO PLAYER RATING BRACKET: STRICTLY BETWEEN {minRating} AND {maxRating} OVERALL.";

            var prompt = $@"
You are an expert EA Sports FC 26 (FC 26 / FIFA) player database analyst and chief tactical scout.
Select the most accurate real-world player archetype by analyzing official EA FC 26 player cards and conducting a HOLISTIC MULTI-ATTRIBUTE EVALUATION of the following player card:

=== PLAYER CARD EVALUATION PROFILE ===
- Player Name: {cardDto.PlayerName}
- PRIMARY POSITION: {cardDto.Position} (MANDATORY EXACT POSITION MATCH)
- Overall Card Rating: {cardDto.OverallRating:F1} ({ratingConstraintText})
- Preferred Foot: {cardDto.PreferredFoot}
- Weak Foot Rating: {cardDto.WeakFootRating} out of 5 stars
- Category Ratings Breakdown: {categoryStatsText}
{(string.IsNullOrWhiteSpace(cardDto.PlayStyleTag) ? "" : $"- Playstyle Tag: {cardDto.PlayStyleTag}\n")}

=== CRITICAL EVALUATION RULES ===

RULE #1: ABSOLUTE STRICT POSITION MATCHING (ZERO TOLERANCE FOR POSITION MISMATCH):
   - The matched real-world player archetype MUST play natively in the EXACT SAME PRIMARY POSITION in EA FC 26 as the player ({cardDto.Position}).
   - ST / CF -> Striker / Center-Forward ONLY (NEVER return a winger, midfielder, or defender).
   - RW / LW / RM / LM -> Winger / Wide Attacker ONLY (NEVER return a striker, central midfielder, or defender).
   - CM / CAM / CDM -> Central Midfielder / Playmaker / Holding Midfielder ONLY (NEVER return a winger or defender).
   - CB -> Center-Back ONLY.
   - LB / RB / LWB / RWB -> Fullback / Wing-Back ONLY.
   - GK -> Goalkeeper ONLY.
   - POSITION MISMATCH IS AN ABSOLUTE FAILURE. IF THE PLAYER IS A {cardDto.Position}, THE ARCHETYPE MUST BE A REAL-WORLD {cardDto.Position}.

RULE #2: HOLISTIC FIFA/FC 26 CARD STATS & ATTRIBUTES MATCHING:
   - Match dominant category ratings ({categoryStatsText}). If Pace & Dribbling are highest -> pick a fast dribbler; if Passing is highest -> pick a playmaker; if Defending/Physicality is highest -> pick a defensive wall.
   - Match preferred foot ({cardDto.PreferredFoot}) and account for inverted vs traditional roles (e.g. Left-footed RW = inverted cut-inside winger).
   - Match weak foot rating ({cardDto.WeakFootRating}/5). A 4/5 or 5/5 weak foot requires an ambidextrous/dual-footed player.

RULE #3: MANDATORY PLAYER TIER & EGYPTIAN/ARAB RESTRICTIONS:
{(isElite ? $@"   - RATING >= 80 (WORLD-CLASS INTERNATIONAL SUPERSTAR): The player rating is {cardDto.OverallRating:F1} (>= 80). IT IS STRICTLY AND ABSOLUTELY FORBIDDEN TO RETURN LOCAL EGYPTIAN OR ARAB LEAGUE PLAYERS. You MUST select an international world-class superstar archetype playing in the Top 5 European Leagues or Roshn Saudi League (e.g. Kevin De Bruyne, Kylian Mbappé, Erling Haaland, Jude Bellingham, Vinícius Jr., Rodri, Pedri, Virgil van Dijk, Courtois, etc.), OR global elite Egyptian superstars ONLY (Mohamed Salah or Omar Marmoush). NO OTHER EGYPTIAN OR ARAB PLAYERS ARE ALLOWED FOR RATING >= 80. Official EA FC 26 rating MUST BE EXACTLY {exactRating}." : $@"   - RATING < 80 (MANDATORY EGYPTIAN PROFESSIONAL PLAYER): The overall rating is {cardDto.OverallRating:F1} (under 80). You MUST MANDATORILY select a real-world EGYPTIAN professional player archetype playing in position {cardDto.Position} in the Egyptian Premier League (Al Ahly, Zamalek, Pyramids, Future, etc.) or Egyptian international expats (e.g., Mostafa Mohamed, Mahmoud Trezeguet, Ibrahim Adel, Emam Ashour, Zizo, Mohamed Abdelmonem, Mohamed El Shenawy, Hossam Abdelmaguid, Mohamed Shehata, Marwan Attia, Osama Faisal, etc.). DO NOT select non-Egyptian European players when rating is under 80.")}

RULE #4: RESPONSE TEXT RULES (ZERO MENTIONS OF EA SPORTS FC / FC 26 / FIFA / VIDEO GAMES):
   - In the output description property (""archetypeText""), DO NOT EVER write words like 'EA Sports FC', 'FC 26', 'EA FC', 'FIFA', 'video game', 'card rating', or 'database'.
   - Write purely as a real-world professional football scout writing a professional scouting report comparing real-world playstyle, preferred foot, weak foot, top attributes, position, career clubs, and tactical role.

=== OUTPUT FORMAT ===
Return ONLY a valid JSON object with two string properties: ""archetypePlayerName"" and ""archetypeText"".
- ""archetypePlayerName"": Full official name of the matched real-world player archetype.
- ""archetypeText"": A detailed, 2-sentence professional tactical comparison explaining how this player's preferred foot ({cardDto.PreferredFoot}), weak foot ({cardDto.WeakFootRating}/5), top category stats ({categoryStatsText}), and primary position ({cardDto.Position}) perfectly mirror the archetype player's real-world career, main clubs, and on-pitch playstyle. (ZERO MENTIONS OF FIFA / FC 26 / EA FC IN THIS TEXT).
".Trim();

            var systemPrompt = "You are an expert EA Sports FC 26 (FC 26 / FIFA) player database analyst and professional football scout. Use official EA FC 26 player cards, ratings, rosters, and attributes internally for matching. ALWAYS output strictly valid JSON. NEVER mention 'EA Sports FC', 'FC 26', 'EA FC', 'FIFA', 'video game', or 'ratings database' in the output text ('archetypeText').";

            var requestBody = new
            {
                model_id = string.IsNullOrWhiteSpace(_itiChatOptions.ModelId) ? "openai.gpt-oss-120b-1:0" : _itiChatOptions.ModelId,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                system_prompt = systemPrompt,
                max_tokens = _itiChatOptions.MaxTokens > 0 ? _itiChatOptions.MaxTokens : 1024
            };

            string archetypePlayerName = string.Empty;
            string archetypeText = string.Empty;

            try
            {
                var requestUrl = string.IsNullOrWhiteSpace(_httpClient.BaseAddress?.ToString())
                    ? (string.IsNullOrWhiteSpace(_itiChatOptions.BaseUrl) ? "http://apiaccess.iti.net.eg/api/v1/student/chat" : _itiChatOptions.BaseUrl)
                    : "";

                var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody);
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonString);

                if (doc.RootElement.TryGetProperty("output_text", out var outputProp))
                {
                    var content = outputProp.GetString();
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        var cleanJson = content.Trim();
                        if (cleanJson.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                            cleanJson = cleanJson.Substring(7);
                        if (cleanJson.StartsWith("```"))
                            cleanJson = cleanJson.Substring(3);
                        if (cleanJson.EndsWith("```"))
                            cleanJson = cleanJson.Substring(0, cleanJson.Length - 3);
                        cleanJson = cleanJson.Trim();

                        using var parsedContent = JsonDocument.Parse(cleanJson);
                        if (parsedContent.RootElement.TryGetProperty("archetypePlayerName", out var nameProp))
                            archetypePlayerName = nameProp.GetString() ?? string.Empty;

                        if (parsedContent.RootElement.TryGetProperty("archetypeText", out var textProp))
                            archetypeText = textProp.GetString() ?? string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while querying ITI Bedrock Gateway API for player archetype (PlayerId: {PlayerId})", playerId);
            }

            // Post-processing Safety Cleaning: Strip any accidental FIFA / FC 26 / EA Sports FC mentions from archetypeText
            if (!string.IsNullOrWhiteSpace(archetypeText))
            {
                archetypeText = archetypeText
                    .Replace("EA Sports FC 26", "Professional Football", StringComparison.OrdinalIgnoreCase)
                    .Replace("EA Sports FC", "Professional Football", StringComparison.OrdinalIgnoreCase)
                    .Replace("EA FC 26", "Professional Football", StringComparison.OrdinalIgnoreCase)
                    .Replace("EA FC", "Professional Football", StringComparison.OrdinalIgnoreCase)
                    .Replace("FC 26", "Professional Football", StringComparison.OrdinalIgnoreCase)
                    .Replace("FC26", "Professional Football", StringComparison.OrdinalIgnoreCase)
                    .Replace("FIFA 26", "Professional Football", StringComparison.OrdinalIgnoreCase)
                    .Replace("FIFA", "Professional Football", StringComparison.OrdinalIgnoreCase);
            }

            // Post-processing Safety Validation: Reject 75+ rated superstars if player rating is < 72
            var highRatedSuperstars = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Omar Marmoush", "Mohamed Salah", "Mostafa Mohamed", "Emam Ashour", "Ahmed Sayed Zizo",
                "Mahmoud Trezeguet", "Mohamed Elneny", "Mohamed Abdelmonem", "Virgil van Dijk",
                "Erling Haaland", "Kylian Mbappé", "Kylian Mbappe", "Kevin De Bruyne", "Jude Bellingham",
                "Rodri", "Vinicius Jr", "Vinicius Junior", "Manuel Neuer", "Thibaut Courtois", "Achraf Hakimi"
            };

            if (cardDto.OverallRating < 72 && highRatedSuperstars.Contains(archetypePlayerName.Trim()))
            {
                _logger.LogWarning(
                    "LLM assigned high-rated superstar '{Star}' to low-rated player (Rating: {Rating}). Triggering rating-appropriate override.",
                    archetypePlayerName,
                    cardDto.OverallRating);

                archetypePlayerName = string.Empty;
            }

            if (string.IsNullOrWhiteSpace(archetypePlayerName))
            {
                var (fallbackName, fallbackDesc) = GetRatingAppropriateFallback(cardDto.OverallRating, cardDto.Position);
                archetypePlayerName = fallbackName;
                archetypeText = fallbackDesc;
            }

            player.ArchetypePlayerName = archetypePlayerName;
            player.ArchetypeText = archetypeText;
            player.ArchetypeLastRevealedAt = null;//DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            return new PlayerArchetypeDto
            {
                PlayerId = playerId,
                ArchetypePlayerName = archetypePlayerName,
                ArchetypeText = archetypeText,
                ArchetypeLastRevealedAt = player.ArchetypeLastRevealedAt
            };
        }

        private async Task<PlayerCardDto?> ProjectPlayerCardDtoAsync(int playerId)
        {
            var data = await _unitOfWork.Repository<PlayerCard>()
                .GetQueryableAsNoTracking()
                .Where(pc => pc.PlayerId == playerId)
                .Select(pc => new
                {
                    pc.OverallRating,
                    pc.OverallTrainingAvg,
                    pc.OverallTournamentAvg,
                    pc.TransferClassification,
                    pc.Player.FirstName,
                    pc.Player.LastName,
                    pc.Player.PreferredFoot,
                    pc.Player.WeakFootRating,
                    pc.Player.ArchetypePlayerName,
                    pc.Player.ArchetypeLastRevealedAt,
                    pc.Player.PlayStyleTag,
                    pc.Player.ProfileImageUrl,
                    PrimaryPosition = pc.Player.PlayerPositions
                        .Where(pp => pp.IsPrimary)
                        .Select(pp => pp.Position)
                        .FirstOrDefault(),
                    Categories = pc.CategoryRatings
                        .Select(cr => new { cr.DrillCategory.Name, cr.Score })
                })
                .FirstOrDefaultAsync();

            if (data is null)
                return null;

            var dto = new PlayerCardDto
            {
                PlayerName = $"{data.FirstName} {data.LastName}",
                Position = data.PrimaryPosition ?? string.Empty,
                OverallRating = data.OverallRating,
                OverallTrainingAvg = data.OverallTrainingAvg,
                OverallTournamentAvg = data.OverallTournamentAvg,
                TransferClassification = data.TransferClassification.ToString(),
                PreferredFoot = data.PreferredFoot,
                WeakFootRating = data.WeakFootRating,
                ArchetypePlayerName = data.ArchetypePlayerName,
                ArchetypeLastRevealedAt = data.ArchetypeLastRevealedAt,
                PlayStyleTag = data.PlayStyleTag,
                ProfileImageUrl = data.ProfileImageUrl
            };

            foreach (var cat in data.Categories)
            {
                switch (cat.Name)
                {
                    case "Passing": dto.PassingRating = cat.Score; break;
                    case "Shooting": dto.ShootingRating = cat.Score; break;
                    case "Dribbling": dto.DribblingRating = cat.Score; break;
                    case "Defending": dto.DefendingRating = cat.Score; break;
                    case "Speed": dto.PaceRating = cat.Score; break;
                    case "Physical": dto.PhysicalRating = cat.Score; break;
                    case "GoalKeeping": dto.GoalkeepingRating = cat.Score; break;
                }
            }

            return dto;
        }

        private async Task<(PlayerCard? Card, string? PrimaryPosition)> GetCardAndPositionAsync(int playerId)
        {
            var existingCard = await _unitOfWork.Repository<PlayerCard>()
                .GetQueryable()
                .Include(pc => pc.Player)
                    .ThenInclude(p => p.PlayerPositions)
                .Include(pc => pc.CategoryRatings)
                .FirstOrDefaultAsync(pc => pc.PlayerId == playerId);

            if (existingCard is not null)
            {
                var position = existingCard.Player.PlayerPositions
                    .FirstOrDefault(x => x.IsPrimary)?.Position;

                return (existingCard, position);
            }

            var player = await _unitOfWork.Repository<PlayerEntity>()
                .GetQueryableAsNoTracking()
                .Include(p => p.PlayerPositions)
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player is null)
                throw new NotFoundException($"Player with id {playerId} was not found");

            var primaryPosition = player.PlayerPositions
                .FirstOrDefault(x => x.IsPrimary)?.Position;

            return (null, primaryPosition);
        }

        private static string[] GetTargetCategories(string? primaryPosition)
        {
            var isGoalkeeper = string.Equals(
                primaryPosition,
                "GK",
                StringComparison.OrdinalIgnoreCase);

            return isGoalkeeper
                ? ["GoalKeeping"]
                : [
                    "Speed",
                    "Shooting",
                    "Passing",
                    "Dribbling",
                    "Defending",
                    "Physical"
                  ];
        }
        private async Task<List<PlayerCardCalculator.CategoryAggregate>> GetDrillAggregatesAsync(int playerId,string[] targetCategories)
        {
            return await _unitOfWork.Repository<DrillResult>()
                .GetQueryableAsNoTracking()
                .Where(dr =>
                    dr.PlayerId == playerId &&
                    targetCategories.Contains(
                        dr.Drill.DrillTemplate.DrillCategory.Name))
                .GroupBy(dr => new
                {
                    dr.Drill.DrillTemplate.CategoryId,
                    dr.Drill.DrillTemplate.DrillCategory.Name
                })
                .Select(g => new PlayerCardCalculator.CategoryAggregate
                {
                    CategoryId = g.Key.CategoryId,
                    Name = g.Key.Name,
                    WeightedSum = g.Sum(dr =>
                        dr.FinalScore * 10m *
                        (dr.Drill.DifficultyLevel == DifficultyLevel.Beginner ? 1m :
                         dr.Drill.DifficultyLevel == DifficultyLevel.Intermediate ? 1.5m : 2m)),

                    TotalWeight = g.Sum(dr =>
                        dr.Drill.DifficultyLevel == DifficultyLevel.Beginner ? 1m :
                        dr.Drill.DifficultyLevel == DifficultyLevel.Intermediate ? 1.5m : 2m),

                    Count = g.Count()
                })
                .ToListAsync();
        }
        /// <summary>
        /// Fetches training (Friendly + Session) and tournament match category ratings
        /// in a single DB round-trip, then splits and aggregates in memory.
        /// Reduces RecalculatePlayerCardAsync from 3 sequential queries to 2.
        /// </summary>
        private async Task<(List<PlayerCardCalculator.CategoryAggregate> Training,
                             List<PlayerCardCalculator.CategoryAggregate> Tournament)>
            GetMatchAggregatesAsync(int playerId, string[] targetCategories)
        {
            // One query covering all relevant match types.
            var raw = await _unitOfWork.Repository<MatchPlayerCategoryRating>()
                .GetQueryableAsNoTracking()
                .Where(cr =>
                    cr.MatchPlayerRating.PlayerId == playerId &&
                    (cr.MatchPlayerRating.Match.Type == Domain.Enums.MatchType.Friendly  ||
                     cr.MatchPlayerRating.Match.Type == Domain.Enums.MatchType.Session   ||
                     cr.MatchPlayerRating.Match.Type == Domain.Enums.MatchType.Tournament) &&
                    targetCategories.Contains(cr.DrillCategory.Name))
                .Select(cr => new
                {
                    cr.DrillCategoryId,
                    cr.DrillCategory.Name,
                    IsTraining =
                        cr.MatchPlayerRating.Match.Type == Domain.Enums.MatchType.Friendly ||
                        cr.MatchPlayerRating.Match.Type == Domain.Enums.MatchType.Session,
                    cr.Rating
                })
                .ToListAsync();

            var training = raw
                .Where(x => x.IsTraining)
                .GroupBy(x => new { x.DrillCategoryId, x.Name })
                .Select(g => new PlayerCardCalculator.CategoryAggregate
                {
                    CategoryId = g.Key.DrillCategoryId,
                    Name       = g.Key.Name,
                    Avg        = g.Average(x => x.Rating) * 10m,
                    Count      = g.Count()
                })
                .ToList();

            var tournament = raw
                .Where(x => !x.IsTraining)
                .GroupBy(x => new { x.DrillCategoryId, x.Name })
                .Select(g => new PlayerCardCalculator.CategoryAggregate
                {
                    CategoryId = g.Key.DrillCategoryId,
                    Name       = g.Key.Name,
                    Avg        = g.Average(x => x.Rating) * 10m,
                    Count      = g.Count()
                })
                .ToList();

            return (training, tournament);
        }
        private static PlayerCardCalculator.RatingLookups BuildRatingLookups(List<PlayerCardCalculator.CategoryAggregate> drillAggregates,
            List<PlayerCardCalculator.CategoryAggregate> trainingAggregates,
            List<PlayerCardCalculator.CategoryAggregate> tournamentAggregates)
        {
            return new PlayerCardCalculator.RatingLookups
            {
                Drill = drillAggregates.ToDictionary(
                    x => x.CategoryId,
                    x => x.TotalWeight > 0
                        ? x.WeightedSum / x.TotalWeight
                        : 0),

                Training = trainingAggregates.ToDictionary(
                    x => x.CategoryId,
                    x => x.Avg),

                Tournament = tournamentAggregates.ToDictionary(
                    x => x.CategoryId,
                    x => x.Avg)
            };
        }
 
        private async Task SavePlayerCardAsync(PlayerCard? existingCard,PlayerCard playerCard)
        {
            if (existingCard is null)
            {
                await _unitOfWork.Repository<PlayerCard>()
                    .AddAsync(playerCard);
            }

            await _unitOfWork.SaveChangesAsync();
        }
        private static PlayerCardDto MapToDto(PlayerCard card)
        {
            var player= card.Player;
            var dto = new PlayerCardDto
            {
                PlayerName = $"{player.FirstName} {player.LastName}",
                OverallRating = card.OverallRating,
                OverallTrainingAvg = card.OverallTrainingAvg,
                OverallTournamentAvg = card.OverallTournamentAvg,
                TransferClassification = card.TransferClassification.ToString(),
                Position = player.PlayerPositions
                    .FirstOrDefault(p => p.IsPrimary)?.Position ?? string.Empty,
                PreferredFoot = player.PreferredFoot,
                WeakFootRating = player.WeakFootRating,
                ArchetypePlayerName = player.ArchetypePlayerName,
                ArchetypeLastRevealedAt = player.ArchetypeLastRevealedAt,
                PlayStyleTag = player.PlayStyleTag,
                ProfileImageUrl = player.ProfileImageUrl,
            };

            foreach (var rating in card.CategoryRatings ?? [])
            {
                switch (rating.DrillCategory.Name)
                {
                    case "Passing": dto.PassingRating = rating.Score; break;
                    case "Shooting": dto.ShootingRating = rating.Score; break;
                    case "Dribbling": dto.DribblingRating = rating.Score; break;
                    case "Defending": dto.DefendingRating = rating.Score; break;
                    case "Speed": dto.PaceRating = rating.Score; break;
                    case "Physical": dto.PhysicalRating = rating.Score; break;
                    case "GoalKeeping": dto.GoalkeepingRating = rating.Score; break;
                }
            }

            return dto;
        }

        private static (string Name, string Text) GetRatingAppropriateFallback(decimal rating, string position)
        {
            var isGk = string.Equals(position, "GK", StringComparison.OrdinalIgnoreCase);
            var isCb = string.Equals(position, "CB", StringComparison.OrdinalIgnoreCase);
            var isWinger = position.Contains("W", StringComparison.OrdinalIgnoreCase) || position.Contains("M", StringComparison.OrdinalIgnoreCase);
            var isStriker = string.Equals(position, "ST", StringComparison.OrdinalIgnoreCase) || string.Equals(position, "CF", StringComparison.OrdinalIgnoreCase);

            if (rating >= 85)
            {
                if (isGk) return ("Thibaut Courtois", "World-class shot stopper with elite positioning and command.");
                if (isCb) return ("Virgil van Dijk", "Dominant centre-back with supreme composure, physical strength, and aerial control.");
                if (isWinger) return ("Mohamed Salah", "Elite world-class winger with lethal finishing, pace, and playmaking ability.");
                return ("Kevin De Bruyne", "Masterclass playmaker with exceptional vision and tactical intellect.");
            }

            if (rating >= 75)
            {
                if (isGk) return ("Mohamed El Shenawy ", "Experienced goalkeeper with solid reflexes and reliable leadership.");
                if (isCb) return ("Mohamed Abdelmonem", "Modern centre-back with excellent ball progression and tackling prowess.");
                if (isWinger) return ("Ahmed Sayed Zizo", "Versatile winger known for pinpoint crossing, work rate, and set-piece mastery.");
                if (isStriker) return ("Mostafa Mohamed", "Strong physical striker with lethal aerial threat and clinical finishing.");
                return ("Emam Ashour", "Dynamic box-to-box midfielder with high work rate and powerful long-range shooting.");
            }

            // Low-rated prospect fallback (Rating < 75 e.g. 60-70)
            if (isGk) return ("Hamza Alaa", "Promising young Egyptian goalkeeper with agile reflexes.");
            if (isCb) return ("Hossam Abdelmaguid", "Tall Egyptian centre-back prospect with strong physical presence.");
            if (isWinger) return ("Ibrahim Adel", "Agile Egyptian winger prospect with quick feet and dribbling flair.");
            if (isStriker) return ("Osama Faisal", "Hardworking young Egyptian striker profile with good mobility.");
            return ("Mohamed Shehata", "Energetic young Egyptian midfielder with active pressing and ball recovery.");
        }
    }
}
