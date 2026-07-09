using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Enums;
using static Koralytics.Application.Services.Player.PlayerCardService.PlayerCardService;

namespace Koralytics.Application.Services.Player
{
    public static class PlayerCardCalculator
    {
        public sealed record RatingLookups
        {
            public required Dictionary<int, decimal> Drill { get; init; }

            public required Dictionary<int, decimal> Training { get; init; }

            public required Dictionary<int, decimal> Tournament { get; init; }
        }
        public sealed record CategoryAggregate
        {
            public required int CategoryId { get; init; }

            public required string Name { get; init; }

            public decimal Avg { get; init; }

            public decimal WeightedSum { get; init; }

            public decimal TotalWeight { get; init; }

            public int Count { get; init; }
        }
        // ─── Difficulty Weight ───────────────────────────────────────────
        public static decimal GetDifficultyWeight(DifficultyLevel level) => level switch
        {
            DifficultyLevel.Beginner => 1.0m,
            DifficultyLevel.Intermediate => 1.5m,
            DifficultyLevel.Advanced => 2.0m,
            _ => 1.0m
        };

        // ─── Weighted Drill Average ──────────────────────────────────────
        public static decimal CalculateWeightedDrillAvg(
            IEnumerable<(decimal FinalScore, DifficultyLevel Difficulty)> drills)
        {
            var list = drills.ToList();
            if (!list.Any()) return 0;

            var weightedSum = list.Sum(d => d.FinalScore * 10m * GetDifficultyWeight(d.Difficulty));
            var totalWeight = list.Sum(d => GetDifficultyWeight(d.Difficulty));

            return totalWeight > 0 ? weightedSum / totalWeight : 0;
        }

        // ─── Training Combined (Drill + Training Match) ──────────────────
        public static decimal CalculateTrainingCombined(
            decimal drillAvg,
            decimal trainingMatchAvg,
            bool hasDrill,
            bool hasTraining)
        => (hasDrill, hasTraining) switch
        {
            (true, true) => 0.4m * drillAvg + 0.6m * trainingMatchAvg,
            (true, false) => drillAvg,
            (false, true) => trainingMatchAvg,
            (false, false) => 0
        };

        // ─── Category Score (Training Combined + Tournament) ─────────────
        public static decimal CalculateCategoryScore(
            decimal drillAvg,
            decimal trainingMatchAvg,
            decimal tournamentAvg,
            bool hasDrill,
            bool hasTraining,
            bool hasTournament)
        {
            var trainingCombined = CalculateTrainingCombined(
                drillAvg, trainingMatchAvg, hasDrill, hasTraining);

            return (trainingCombined > 0, hasTournament) switch
            {
                (true, true) => 0.3m * trainingCombined + 0.7m * tournamentAvg,
                (true, false) => trainingCombined,
                (false, true) => tournamentAvg,
                (false, false) => 0
            };
        }

        // ─── Overall Rating ──────────────────────────────────────────────
        public static decimal CalculateOverallRating(
            IEnumerable<decimal> categoryScores)
        {
            var list = categoryScores.ToList();
            return list.Any() ? list.Average() : 0;
        }

        // ─── Overall Training Avg ────────────────────────────────────────
        public static decimal CalculateOverallTrainingAvg(
            decimal overallDrillAvg,
            decimal overallTrainingMatchAvg,
            bool hasDrills,
            bool hasTrainingMatches)
        => (hasDrills, hasTrainingMatches) switch
        {
            (true, true) => 0.4m * overallDrillAvg + 0.6m * overallTrainingMatchAvg,
            (true, false) => overallDrillAvg,
            (false, true) => overallTrainingMatchAvg,
            (false, false) => 0
        };

        // ─── Transfer Classification ─────────────────────────────────────
        public static TransferClassification DetermineClassification(
    decimal trainingAvg,
    decimal tournamentAvg)
        {
            const decimal EliteThreshold = 90m;
            const decimal NeedsWorkThreshold = 70m;
            const decimal GapThreshold = 15m;

            if (trainingAvg >= EliteThreshold &&
                tournamentAvg >= EliteThreshold)
            {
                return TransferClassification.Elite;
            }

            if (trainingAvg < NeedsWorkThreshold &&
                tournamentAvg < NeedsWorkThreshold)
            {
                return TransferClassification.NeedsWork;
            }

            var gap = trainingAvg - tournamentAvg;

            if (Math.Abs(gap) >= GapThreshold)
            {
                return gap > 0
                    ? TransferClassification.Trainable
                    : TransferClassification.Natural;
            }

            return TransferClassification.Developing;
        }
        public static void UpdateCategoryRatings(
    PlayerCard playerCard,
    RatingLookups lookups)
        {
            var allCategoryIds = lookups.Drill.Keys
                .Union(lookups.Training.Keys)
                .Union(lookups.Tournament.Keys);

            foreach (var categoryId in allCategoryIds)
            {
                var drillAvg = lookups.Drill.GetValueOrDefault(categoryId);
                var trainingAvg = lookups.Training.GetValueOrDefault(categoryId);
                var tournamentAvg = lookups.Tournament.GetValueOrDefault(categoryId);

                var score = CalculateCategoryScore(
                    drillAvg,
                    trainingAvg,
                    tournamentAvg,
                    lookups.Drill.ContainsKey(categoryId),
                    lookups.Training.ContainsKey(categoryId),
                    lookups.Tournament.ContainsKey(categoryId));

                var existingRating = playerCard.CategoryRatings
                    .FirstOrDefault(x => x.DrillCategoryId == categoryId);

                if (existingRating is not null)
                {
                    existingRating.Score = score;
                    existingRating.LastUpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    playerCard.CategoryRatings.Add(new PlayerCategoryRating
                    {
                        DrillCategoryId = categoryId,
                        Score = score,
                        LastUpdatedAt = DateTime.UtcNow
                    });
                }
            }
        }
        public static void UpdateOverallRating(PlayerCard playerCard)
        {
            playerCard.OverallRating =
                CalculateOverallRating(
                    playerCard.CategoryRatings.Select(x => x.Score));
        }
        public static void UpdateOverallAverages(
    PlayerCard playerCard,
    List<CategoryAggregate> drillAggregates,
    List<CategoryAggregate> trainingAggregates,
    List<CategoryAggregate> tournamentAggregates)
        {
            var overallDrillAvg = drillAggregates.Any()
                ? drillAggregates.Sum(x => x.WeightedSum) /
                  drillAggregates.Sum(x => x.TotalWeight)
                : 0;

            var overallTrainingAvg = trainingAggregates.Any()
                ? trainingAggregates.Average(x => x.Avg)
                : 0;

            var overallTournamentAvg = tournamentAggregates.Any()
                ? tournamentAggregates.Average(x => x.Avg)
                : 0;

            playerCard.OverallTrainingAvg =
                PlayerCardCalculator.CalculateOverallTrainingAvg(
                    overallDrillAvg,
                    overallTrainingAvg,
                    drillAggregates.Any(),
                    trainingAggregates.Any());

            playerCard.OverallTournamentAvg =
                overallTournamentAvg;
        }
        public static void UpdateTransferClassification(
    PlayerCard playerCard,
    List<CategoryAggregate> drillAggregates,
    List<CategoryAggregate> trainingAggregates,
    List<CategoryAggregate> tournamentAggregates)
        {
            var totalRecords =
                drillAggregates.Sum(x => x.Count) +
                trainingAggregates.Sum(x => x.Count) +
                tournamentAggregates.Sum(x => x.Count);

            playerCard.TransferClassification =
                totalRecords >= 5
                    ? PlayerCardCalculator.DetermineClassification(
                        playerCard.OverallTrainingAvg,
                        playerCard.OverallTournamentAvg)
                    : TransferClassification.InsufficientData;
        }
        public static RatingLookups BuildRatingLookups(
    List<CategoryAggregate> drillAggregates,
    List<CategoryAggregate> trainingAggregates,
    List<CategoryAggregate> tournamentAggregates)
        {
            return new RatingLookups
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
    }
}
