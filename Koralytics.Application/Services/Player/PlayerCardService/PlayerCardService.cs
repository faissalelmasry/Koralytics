using Koralytics.Application.DTOs.Player;
using Koralytics.Application.Interfaces;
using Koralytics.Domain.Entities.Drill;
using Koralytics.Domain.Entities.Match;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using PlayerEntity = Koralytics.Domain.Entities.Player.Player;
using PlayerCardEntity = Koralytics.Domain.Entities.Player.PlayerCard;
using PlayerCategoryRatingEntity = Koralytics.Domain.Entities.Player.PlayerCategoryRating;
using DrillResultEntity = Koralytics.Domain.Entities.Drill.DrillResult;
using DrillCategoryEntity = Koralytics.Domain.Entities.Drill.DrillCategory;
using MatchPlayerRatingEntity = Koralytics.Domain.Entities.Match.MatchPlayerRating;
using MatchPlayerCategoryRatingEntity = Koralytics.Domain.Entities.Match.MatchPlayerCategoryRating;

namespace Koralytics.Application.Services.Player.PlayerCardService
{
    public class PlayerCardService : IPlayerCardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PlayerCardService> _logger;

        public PlayerCardService(
            IUnitOfWork unitOfWork,
            ILogger<PlayerCardService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<PlayerCardDto> GetPlayerCardAsync(int playerId)
        {
            _logger.LogInformation("Fetching player card for player {PlayerId}", playerId);

            var playerCard = await _unitOfWork.Repository<PlayerCardEntity>()
            .GetQueryableAsNoTracking()
            .Include(pc => pc.Player)
            .ThenInclude(p => p.PlayerPositions)
            .Include(pc => pc.CategoryRatings)
            .ThenInclude(cr => cr.DrillCategory)
            .FirstOrDefaultAsync(pc => pc.PlayerId == playerId);

            if (playerCard is null)
            {
                var playerExists = await _unitOfWork.Repository<PlayerEntity>()
                    .ExistsAsync(p => p.Id == playerId);

                if (!playerExists)
                    throw new NotFoundException($"Player with id {playerId} was not found");

                await RecalculateCategoryRatingAsync(playerId);

                playerCard = await _unitOfWork.Repository<PlayerCardEntity>()
                    .GetQueryableAsNoTracking()
                    .Include(pc => pc.Player)
                        .ThenInclude(p => p.PlayerPositions)
                    .Include(pc => pc.CategoryRatings)
                        .ThenInclude(cr => cr.DrillCategory)
                    .FirstOrDefaultAsync(pc => pc.PlayerId == playerId);
            }

            return MapToDto(playerCard);
        }

        public async Task RecalculateCategoryRatingAsync(int playerId)
        {
            _logger.LogInformation("Recalculating player card for player {PlayerId}", playerId);

            var player = await _unitOfWork.Repository<PlayerEntity>()
                .GetQueryableAsNoTracking()
                .Include(p => p.PlayerPositions)
                .FirstOrDefaultAsync(p => p.Id == playerId);
            if (player is null)
                throw new NotFoundException($"Player with Id {playerId} not found");

            var primaryPosition = player.PlayerPositions
                .FirstOrDefault(p => p.IsPrimary)?.Position;

            var isGoalkeeper = string.Equals(primaryPosition, "GK",
                StringComparison.OrdinalIgnoreCase);

            var targetCategories = isGoalkeeper
                ? new[] { "GoalKeeping" }
                : new[] { "Speed", "Shooting", "Passing", "Dribbling", "Defending", "Physical" };

            var drillResults = await _unitOfWork.Repository<DrillResultEntity>()
                .GetQueryableAsNoTracking()
                .Include(dr => dr.Drill)
                    .ThenInclude(d => d.DrillTemplate)
                        .ThenInclude(dt => dt.DrillCategory)
                .Where(dr => dr.PlayerId == playerId)
                .ToListAsync();

            var matchRatings = await _unitOfWork.Repository<MatchPlayerRatingEntity>()
                .GetQueryableAsNoTracking()
                .Include(mpr => mpr.Match)
                .Include(mpr => mpr.CategoryRatings)
                .Where(mpr => mpr.PlayerId == playerId)
                .ToListAsync();

            var trainingMatchRatings = matchRatings
                .Where(mpr => mpr.Match.Type == Koralytics.Domain.Enums.MatchType.Friendly
                           || mpr.Match.Type == Koralytics.Domain.Enums.MatchType.Session)
                .ToList();

            var tournamentMatchRatings = matchRatings
                .Where(mpr => mpr.Match.Type == Koralytics.Domain.Enums.MatchType.Tournament)
                .ToList();

            var existingCard = await _unitOfWork.Repository<PlayerCardEntity>()
                .GetQueryable()
                .Include(pc => pc.CategoryRatings)
                .FirstOrDefaultAsync(pc => pc.PlayerId == playerId);

            var playerCard = existingCard ?? new PlayerCardEntity { PlayerId = playerId };
            playerCard.CategoryRatings.Clear();

            var categoryDrillAvgs = drillResults
                .GroupBy(dr => dr.Drill!.DrillTemplate!.DrillCategory)
                .Where(g => g.Key is not null && targetCategories.Contains(g.Key.Name))
                .ToDictionary(
                    g => g.Key!.Id,
                    g =>
                    {
                        var weightedSum = g.Sum(dr =>
                            dr.FinalScore * 10m * GetDifficultyWeight(dr.Drill!.DifficultyLevel));
                        var totalWeight = g.Sum(dr =>
                            GetDifficultyWeight(dr.Drill!.DifficultyLevel));
                        return totalWeight > 0 ? weightedSum / totalWeight : 0;
                    });

            var trainingCategoryRatings = trainingMatchRatings
                .SelectMany(mpr => mpr.CategoryRatings)
                .ToList();

            var categoryTrainingMatchAvgs = trainingCategoryRatings
                .GroupBy(cr => cr.DrillCategoryId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Average(cr => cr.Rating) * 10m);

            var tournamentCategoryRatings = tournamentMatchRatings
                .SelectMany(mpr => mpr.CategoryRatings)
                .ToList();

            var categoryTournamentAvgs = tournamentCategoryRatings
                .GroupBy(cr => cr.DrillCategoryId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Average(cr => cr.Rating) * 10m);

            var allCategories = await _unitOfWork.Repository<DrillCategoryEntity>()
                .GetQueryableAsNoTracking()
                .ToListAsync();

            var categoryIdByName = allCategories
                .Where(c => targetCategories.Contains(c.Name))
                .ToDictionary(c => c.Name, c => c.Id);

            foreach (var categoryName in targetCategories)
            {
                if (!categoryIdByName.TryGetValue(categoryName, out var categoryId))
                    continue;

                var drillAvg = 0m;
                var hasCategoryDrills = false;

                if (categoryDrillAvgs.ContainsKey(categoryId))
                {
                    drillAvg = categoryDrillAvgs[categoryId];
                    hasCategoryDrills = true;
                }

                var trainingMatchAvg = 0m;
                var hasCategoryTrainingMatches = false;

                if (categoryTrainingMatchAvgs.ContainsKey(categoryId))
                {
                    trainingMatchAvg = categoryTrainingMatchAvgs[categoryId];
                    hasCategoryTrainingMatches = true;
                }

                var tournamentAvg = 0m;
                var hasCategoryTournament = false;

                if (categoryTournamentAvgs.ContainsKey(categoryId))
                {
                    tournamentAvg = categoryTournamentAvgs[categoryId];
                    hasCategoryTournament = true;
                }

                var trainingCombined = (hasCategoryDrills, hasCategoryTrainingMatches) switch
                {
                    (true, true)   => (drillAvg + trainingMatchAvg) / 2m,
                    (true, false)  => drillAvg,
                    (false, true)  => trainingMatchAvg,
                    (false, false) => 0
                };

                var score = (trainingCombined > 0, hasCategoryTournament) switch
                {
                    (true, true)   => 0.3m * trainingCombined + 0.7m * tournamentAvg,
                    (true, false)  => trainingCombined,
                    (false, true)  => tournamentAvg,
                    (false, false) => 0
                };

                playerCard.CategoryRatings.Add(new PlayerCategoryRatingEntity
                {
                    DrillCategoryId = categoryId,
                    Score = score,
                    LastUpdatedAt = DateTime.UtcNow
                });
            }

            // OverallRating = average of target category scores, missing = 0
            var categoryScores = targetCategories
                .Select(cat =>
                {
                    var rating = playerCard.CategoryRatings
                        .FirstOrDefault(cr => categoryIdByName.TryGetValue(cat, out var id)
                            && cr.DrillCategoryId == id);
                    return rating?.Score ?? 0;
                })
                .ToList();

            playerCard.OverallRating = categoryScores.Average();

            // Global averages for transfer rate display (all drills + matches, unfiltered by category)
            var overallDrillAvg = drillResults.Count > 0
                ? drillResults.Sum(dr =>
                    dr.FinalScore * 10m * GetDifficultyWeight(dr.Drill!.DifficultyLevel))
                  / drillResults.Sum(dr => GetDifficultyWeight(dr.Drill!.DifficultyLevel))
                : 0;

            var overallTrainingMatchAvg = trainingMatchRatings.Count > 0
                ? trainingMatchRatings
                    .Where(mpr => mpr.CategoryRatings.Any())
                    .Select(mpr => mpr.CategoryRatings.Average(cr => cr.Rating))
                    .DefaultIfEmpty(0)
                    .Average() * 10m
                : 0;

            var hasDrills = drillResults.Count > 0;
            var hasOverallTrainingMatches = trainingMatchRatings.Count > 0;
            var hasOverallTournament = tournamentMatchRatings.Count > 0;

            var trainingCombinedAvg = (hasDrills, hasOverallTrainingMatches) switch
            {
                (true, true)   => (overallDrillAvg + overallTrainingMatchAvg) / 2m,
                (true, false)  => overallDrillAvg,
                (false, true)  => overallTrainingMatchAvg,
                (false, false) => 0
            };

            playerCard.OverallTrainingAvg = trainingCombinedAvg;

            playerCard.OverallTournamentAvg = tournamentMatchRatings.Count > 0
                ? tournamentMatchRatings
                    .Where(mpr => mpr.CategoryRatings.Any())
                    .Select(mpr => mpr.CategoryRatings.Average(cr => cr.Rating))
                    .DefaultIfEmpty(0)
                    .Average() * 10m
                : 0;

            var totalRecords = drillResults.Count + matchRatings.Count;

            playerCard.TransferClassification = totalRecords >= 5
                ? DetermineClassification(
                    playerCard.OverallTrainingAvg,
                    playerCard.OverallTournamentAvg)
                : TransferClassification.InsufficientData;

            playerCard.LastCalculatedAt = DateTime.UtcNow;

            if (existingCard is null)
                await _unitOfWork.Repository<PlayerCardEntity>().AddAsync(playerCard);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Player card recalculated for player {PlayerId}. Overall: {Rating}, Classification: {Class}",
                playerId, playerCard.OverallRating, playerCard.TransferClassification);
        }

        public async Task<TransferRateDto?> GetDrillToMatchTransferRateAsync(int playerId)
        {
            _logger.LogInformation("Fetching transfer rate for player {PlayerId}", playerId);

            var player = await _unitOfWork.Repository<PlayerEntity>()
                .FindAsync(p => p.Id == playerId);
            if (player is null)
                throw new NotFoundException($"Player with Id {playerId} not found");

            var playerCard = await _unitOfWork.Repository<PlayerCardEntity>()
                .GetQueryableAsNoTracking()
                .Include(pc => pc.Player)
                .FirstOrDefaultAsync(pc => pc.PlayerId == playerId);

            if (playerCard is null)
                return null;

            return new TransferRateDto
            {
                PlayerId = playerId,
                PlayerName = $"{playerCard.Player.FirstName} {playerCard.Player.LastName}",
                OverallTrainingAvg = playerCard.OverallTrainingAvg,
                OverallTournamentAvg = playerCard.OverallTournamentAvg,
                TransferGap = playerCard.OverallTrainingAvg - playerCard.OverallTournamentAvg,
                Classification = playerCard.TransferClassification.ToString()
            };
        }

        private static decimal GetDifficultyWeight(DifficultyLevel level) => level switch
        {
            DifficultyLevel.Beginner => 0.8m,
            DifficultyLevel.Intermediate => 1.0m,
            DifficultyLevel.Advanced => 1.2m,
            _ => 1.0m
        };

        private static TransferClassification DetermineClassification(
            decimal trainingAvg, decimal tournamentAvg)
        {
            if (trainingAvg >= 90 && tournamentAvg >= 90)
                return TransferClassification.Elite;
            if (trainingAvg <= 70 && tournamentAvg <= 70)
                return TransferClassification.NeedsWork;

            var gap = trainingAvg - tournamentAvg;
            if (gap > 15)
                return TransferClassification.Trainable;
            if (gap < -15)
                return TransferClassification.Natural;

            return TransferClassification.Developing;
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
                LastCalculatedAt = card.LastCalculatedAt,
                Position = player.PlayerPositions
                    .FirstOrDefault(p => p.IsPrimary)?.Position ?? string.Empty,
                PreferredFoot = player.PreferredFoot,
                WeakFootRating = player.WeakFootRating,
                ArchetypePlayerName = player.ArchetypePlayerName,
                PlayStyleTag = player.PlayStyleTag,
                ProfileImageUrl = player.ProfileImageUrl
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
