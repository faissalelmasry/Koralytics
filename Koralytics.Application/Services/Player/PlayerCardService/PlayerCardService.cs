using AutoMapper;
using Koralytics.Application.DTOs.Player;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Services.Player.Helpers;
using Koralytics.Domain.Entities.Drill;
using Koralytics.Domain.Entities.Match;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PlayerEntity = Koralytics.Domain.Entities.Player.Player;

namespace Koralytics.Application.Services.Player.PlayerCardService
{
    public class PlayerCardService : IPlayerCardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PlayerCardService> _logger;
        private readonly IMapper _mapper;
        private readonly ICardInvalidationList _invalidationList;

        public PlayerCardService(
            IUnitOfWork unitOfWork,
            ILogger<PlayerCardService> logger,
            IMapper mapper,
            ICardInvalidationList invalidationList)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
            _invalidationList = invalidationList;
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

        private async Task<PlayerCardDto?> ProjectPlayerCardDtoAsync(int playerId)
        {
            var data = await _unitOfWork.Repository<PlayerCard>()
                .GetQueryableAsNoTracking()
                .Where(pc => pc.PlayerId == playerId)
                .Select(pc => new
                {
                    pc.OverallRating,
                    pc.TransferClassification,
                    pc.Player.FirstName,
                    pc.Player.LastName,
                    pc.Player.PreferredFoot,
                    pc.Player.WeakFootRating,
                    pc.Player.ArchetypePlayerName,
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
                TransferClassification = data.TransferClassification.ToString(),
                PreferredFoot = data.PreferredFoot,
                WeakFootRating = data.WeakFootRating,
                ArchetypePlayerName = data.ArchetypePlayerName,
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
                TransferClassification = card.TransferClassification.ToString(),
                Position = player.PlayerPositions
                    .FirstOrDefault(p => p.IsPrimary)?.Position ?? string.Empty,
                PreferredFoot = player.PreferredFoot,
                WeakFootRating = player.WeakFootRating,
                ArchetypePlayerName = player.ArchetypePlayerName,
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
    }
}
