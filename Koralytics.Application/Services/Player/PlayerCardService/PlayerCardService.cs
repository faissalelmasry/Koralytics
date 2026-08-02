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
        private readonly GroqOptions _groqOptions;

        public PlayerCardService(
            IUnitOfWork unitOfWork,
            ILogger<PlayerCardService> logger,
            IMapper mapper,
            ICardInvalidationList invalidationList,
            HttpClient httpClient,
            IOptions<GroqOptions> groqOptions)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
            _invalidationList = invalidationList;
            _httpClient = httpClient;
            _groqOptions = groqOptions.Value;

            if (_httpClient.BaseAddress is null && !string.IsNullOrWhiteSpace(_groqOptions.BaseUrl))
            {
                _httpClient.BaseAddress = new Uri(_groqOptions.BaseUrl);
            }

            if (!_httpClient.DefaultRequestHeaders.Contains("Authorization") && !string.IsNullOrWhiteSpace(_groqOptions.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _groqOptions.ApiKey);
            }
        }

        public async Task<PlayerCardDto> GetPlayerCardAsync(int playerId)
        {
            _logger.LogInformation("Fetching player card for player {PlayerId}", playerId);

            var playerCard = await _unitOfWork.Repository<PlayerCard>()
            .GetQueryableAsNoTracking()
            .Include(pc => pc.Player)
            .ThenInclude(p => p.PlayerPositions)
            .Include(pc => pc.CategoryRatings)
            .ThenInclude(cr => cr.DrillCategory)
            .FirstOrDefaultAsync(pc => pc.PlayerId == playerId);

            if (playerCard is null || _invalidationList.TryConsume(playerId))
            {
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

            var trainingMatchCategoryAvgs =
                await GetTrainingMatchAggregatesAsync(
                    playerId,
                    targetCategories);

            var tournamentMatchCategoryAvgs =
                await GetTournamentMatchAggregatesAsync(
                    playerId,
                    targetCategories);

            var ratingLookups = BuildRatingLookups(
                categoryDrillAvgs,
                trainingMatchCategoryAvgs,
                tournamentMatchCategoryAvgs);

            var playerCard = existingCard ?? new PlayerCard
            {
                PlayerId = playerId
            };

            PlayerCardCalculator.UpdateCategoryRatings(
                playerCard,
                ratingLookups);

            PlayerCardCalculator.UpdateOverallRating(playerCard);

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

            var player = await _unitOfWork.Repository<PlayerEntity>()
                .GetQueryable()
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player is null)
                throw new NotFoundException($"Player with id {playerId} was not found");

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
                    PlayerId = playerId,
                    ArchetypePlayerName = player.ArchetypePlayerName,
                    ArchetypeText = player.ArchetypeText ?? string.Empty,
                    ArchetypeLastRevealedAt = player.ArchetypeLastRevealedAt
                };
            }

            var cardDto = await GetPlayerCardAsync(playerId);

            var isGoalkeeper = string.Equals(cardDto.Position, "GK", StringComparison.OrdinalIgnoreCase);

            string categoryStatsText = isGoalkeeper
                ? $"Goalkeeping: {cardDto.GoalkeepingRating ?? 0}"
                : $"Passing: {cardDto.PassingRating ?? 0}, Shooting: {cardDto.ShootingRating ?? 0}, Dribbling: {cardDto.DribblingRating ?? 0}, Defending: {cardDto.DefendingRating ?? 0}, Speed/Pace: {cardDto.PaceRating ?? 0}, Physical: {cardDto.PhysicalRating ?? 0}";

            var isElite = cardDto.OverallRating >= 80;
            var exactRating = (int)Math.Round(cardDto.OverallRating);

            var minRating = isElite ? exactRating : Math.Max(45, (int)Math.Floor(cardDto.OverallRating - 4));
            var maxRating = isElite ? exactRating : Math.Min(99, (int)Math.Ceiling(cardDto.OverallRating + 4));

            var ratingConstraintText = isElite
                ? $"EXACT EA SPORTS FC 26 RATING REQUIRED: MUST BE EXACTLY {exactRating} OVERALL (NO RATING THRESHOLD OR RANGE ALLOWED)."
                : $"EA SPORTS FC 26 RATING BRACKET: STRICTLY BETWEEN {minRating} AND {maxRating}.";

            var prompt = $@"
You are an expert EA Sports FC 26 (FC 26) database analyst and top-tier football tactical scout specializing in official EA Sports FC 26 player ratings and rosters.
Analyze the following player's profile and stat distribution:
- Player Name: {cardDto.PlayerName}
- REQUIRED PRIMARY POSITION: {cardDto.Position} (MANDATORY EXACT POSITION MATCH)
- Player Overall Rating: {cardDto.OverallRating:F1}
- RATING CONSTRAINT: {ratingConstraintText}
- Preferred Foot: {cardDto.PreferredFoot}
- Weak Foot Rating: {cardDto.WeakFootRating}/5
- Category Ratings: {categoryStatsText}

MANDATORY EA SPORTS FC 26 (FC 26) RULES:

1. RATING CONSTRAINT:
{(isElite ? $@"   - RATING >= 80 (EXACT RATING MATCH MANDATORY): The player's rating is {cardDto.OverallRating:F1} (rounded to {exactRating}). You MUST select a real-world international player archetype in EA Sports FC 26 playing in the Top 5 European Leagues or Roshn Saudi League whose official EA FC 26 overall rating is EXACTLY {exactRating}!
   - DO NOT select a player rated {exactRating - 1}, {exactRating + 1}, or any other rating. The archetype's official EA FC 26 rating MUST BE EXACTLY {exactRating}." : $@"   - RATING < 80 (RATING BRACKET {minRating} TO {maxRating}): Select a real-world Egyptian professional player in EA Sports FC 26 whose FC 26 overall rating is strictly between {minRating} and {maxRating} and plays as {cardDto.Position}.
   - In your description (archetypeText), state the player's current club (e.g., FC Nantes, Al Ahly, Zamalek, Pyramids, Al Jazira, etc.). If no Egyptian player matches this position in the {minRating}-{maxRating} rating window, select a player from the Top 5 European Leagues or Roshn Saudi League within {minRating}-{maxRating}.")}

2. EXACT POSITION MATCH (ZERO POSITION MISMATCH):
   - The matched real-world player archetype MUST play in the EXACT SAME PRIMARY POSITION in EA FC 26 as the player ({cardDto.Position}).
   - CB -> Centre-Back | ST/CF -> Striker | RW/LW/RM/LM -> Winger | CM/CAM/CDM -> Central Midfielder | GK -> Goalkeeper.

3. PREFERRED FOOT & STAT HARMONY:
   - Match preferred foot ({cardDto.PreferredFoot}) and similar stat breakdown ({categoryStatsText}).

4. OUTPUT FORMAT: Return ONLY a valid JSON object with two string properties: ""archetypePlayerName"" and ""archetypeText"".
   - ""archetypePlayerName"": Full name of the matched EA FC 26 player archetype.
   - ""archetypeText"": A detailed, highly complimentary 2-sentence description stating: (1) The main real-world clubs the archetype player has played for during his career (e.g. Al Ahly, Zamalek, FC Nantes, Ajax, Manchester United, Real Madrid, Liverpool, etc.), (2) The exact positions he has played in throughout his career, and (3) An encouraging tactical breakdown highlighting how this player's preferred foot, primary position, and stat ratings make him remarkably close in style to this archetype.
".Trim();

            var requestBody = new
            {
                model = string.IsNullOrWhiteSpace(_groqOptions.ModelName) ? "llama-3.3-70b-versatile" : _groqOptions.ModelName,
                messages = new[]
                {
                    new { role = "system", content = "You are a professional football scouting AI specializing in EA Sports FC 26 (FC 26) player database. Always output strictly valid JSON." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.3,
                response_format = new { type = "json_object" }
            };

            string archetypePlayerName = string.Empty;
            string archetypeText = string.Empty;

            try
            {
                var response = await _httpClient.PostAsJsonAsync("chat/completions", requestBody);
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonString);

                var choices = doc.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() > 0)
                {
                    var content = choices[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString();

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
                _logger.LogError(ex, "Error occurred while querying Groq API for player archetype (PlayerId: {PlayerId})", playerId);
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
        private async Task<List<PlayerCardCalculator.CategoryAggregate>> GetTrainingMatchAggregatesAsync(int playerId,string[] targetCategories)
        {
            return await _unitOfWork.Repository<MatchPlayerCategoryRating>()
                .GetQueryableAsNoTracking()
                .Where(cr =>
                    cr.MatchPlayerRating.PlayerId == playerId &&
                    (cr.MatchPlayerRating.Match.Type == Domain.Enums.MatchType.Friendly ||
                     cr.MatchPlayerRating.Match.Type == Domain.Enums.MatchType.Session) &&
                    targetCategories.Contains(cr.DrillCategory.Name))
                .GroupBy(cr => new
                {
                    cr.DrillCategoryId,
                    cr.DrillCategory.Name
                })
                .Select(g => new PlayerCardCalculator.CategoryAggregate
                {
                    CategoryId = g.Key.DrillCategoryId,
                    Name = g.Key.Name,
                    Avg = g.Average(x => x.Rating) * 10m,
                    Count = g.Count()
                })
                .ToListAsync();
        }
        private async Task<List<PlayerCardCalculator.CategoryAggregate>> GetTournamentMatchAggregatesAsync(int playerId,string[] targetCategories)
        {
            return await _unitOfWork.Repository<MatchPlayerCategoryRating>()
                .GetQueryableAsNoTracking()
                .Where(cr =>
                    cr.MatchPlayerRating.PlayerId == playerId &&
                    cr.MatchPlayerRating.Match.Type == Domain.Enums.MatchType.Tournament &&
                    targetCategories.Contains(cr.DrillCategory.Name))
                .GroupBy(cr => new
                {
                    cr.DrillCategoryId,
                    cr.DrillCategory.Name
                })
                .Select(g => new PlayerCardCalculator.CategoryAggregate
                {
                    CategoryId = g.Key.DrillCategoryId,
                    Name = g.Key.Name,
                    Avg = g.Average(x => x.Rating) * 10m,
                    Count = g.Count()
                })
                .ToListAsync();
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
                if (isGk) return ("Mohamed El Shenawy (Al Ahly)", "Experienced goalkeeper with solid reflexes and reliable leadership.");
                if (isCb) return ("Mohamed Abdelmonem (OGC Nice)", "Modern centre-back with excellent ball progression and tackling prowess.");
                if (isWinger) return ("Ahmed Sayed Zizo (Zamalek)", "Versatile winger known for pinpoint crossing, work rate, and set-piece mastery.");
                if (isStriker) return ("Mostafa Mohamed (FC Nantes)", "Strong physical striker with lethal aerial threat and clinical finishing.");
                return ("Emam Ashour (Al Ahly)", "Dynamic box-to-box midfielder with high work rate and powerful long-range shooting.");
            }

            // Low-rated prospect fallback (Rating < 75 e.g. 60-70)
            if (isGk) return ("Hamza Alaa (Al Ahly)", "Promising young Egyptian goalkeeper with agile reflexes.");
            if (isCb) return ("Hossam Abdelmaguid (Zamalek)", "Tall Egyptian centre-back prospect with strong physical presence.");
            if (isWinger) return ("Ibrahim Adel (Pyramids FC)", "Agile Egyptian winger prospect with quick feet and dribbling flair.");
            if (isStriker) return ("Osama Faisal (Bank El Ahly)", "Hardworking young Egyptian striker profile with good mobility.");
            return ("Mohamed Shehata (Zamalek)", "Energetic young Egyptian midfielder with active pressing and ball recovery.");
        }
    }
}
