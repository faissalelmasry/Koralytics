using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Application.Services.Player;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Enums;
using static Koralytics.Application.Services.Player.PlayerCardCalculator;

namespace Koralytics.Application.UnitTests.Player
{
    public class PlayerCardCalculatorTests
    {
        [Fact]
        public void CalculateCategoryScore_BothDrillAndTournament_ReturnsCorrectWeight()
        {
            var score = CalculateCategoryScore(
                drillAvg: 70m, trainingMatchAvg: 80m, tournamentAvg: 90m,
                hasDrill: true, hasTraining: true, hasTournament: true);

            var expectedTraining = 0.4m * 70m + 0.6m * 80m; // 76
            var expectedScore = 0.3m * 76m + 0.7m * 90m; // 85.8
            Assert.Equal(expectedScore, score);
        }

        [Fact]
        public void CalculateCategoryScore_DrillOnly_ReturnsDrillAvg()
        {
            var score = CalculateCategoryScore(
                drillAvg: 70m, trainingMatchAvg: 0, tournamentAvg: 0,
                hasDrill: true, hasTraining: false, hasTournament: false);

            Assert.Equal(70m, score);
        }

        [Fact]
        public void CalculateCategoryScore_NoData_ReturnsZero()
        {
            var score = CalculateCategoryScore(
                drillAvg: 0, trainingMatchAvg: 0, tournamentAvg: 0,
                hasDrill: false, hasTraining: false, hasTournament: false);

            Assert.Equal(0m, score);
        }

        [Fact]
        public void CalculateOverallRating_MultipleCategoryScores_ReturnsAverage()
        {
            var scores = new[] { 70m, 80m, 90m };
            var overall = CalculateOverallRating(scores);
            Assert.Equal(80m, overall);
        }

        [Fact]
        public void CalculateOverallRating_NoScores_ReturnsZero()
        {
            var overall = CalculateOverallRating([]);
            Assert.Equal(0m, overall);
        }

        [Fact]
        public void DetermineClassification_BothAbove90_ReturnsElite()
        {
            var result = DetermineClassification(95m, 92m);

            Assert.Equal(TransferClassification.Elite, result);
        }

        [Fact]
        public void DetermineClassification_TrainingHigherBy15OrMore_ReturnsTrainable()
        {
            var result = DetermineClassification(90m, 75m);

            Assert.Equal(TransferClassification.Trainable, result);
        }

        [Fact]
        public void DetermineClassification_TournamentHigherBy15OrMore_ReturnsNatural()
        {
            var result = DetermineClassification(75m, 90m);

            Assert.Equal(TransferClassification.Natural, result);
        }

        [Fact]
        public void DetermineClassification_BothBelow70_ReturnsNeedsWork()
        {
            var result = DetermineClassification(65m, 60m);

            Assert.Equal(TransferClassification.NeedsWork, result);
        }

        [Fact]
        public void DetermineClassification_GapLessThan15_ReturnsDeveloping()
        {
            var result = DetermineClassification(85m, 75m);

            Assert.Equal(TransferClassification.Developing, result);
        }

        [Fact]
        public void DetermineClassification_GapExactly15_ReturnsTrainable()
        {
            var result = DetermineClassification(90m, 75m);

            Assert.Equal(TransferClassification.Trainable, result);
        }

        [Fact]
        public void DetermineClassification_BothExactly90_ReturnsElite()
        {
            var result = DetermineClassification(90m, 90m);

            Assert.Equal(TransferClassification.Elite, result);
        }

        [Fact]
        public void GetDifficultyWeight_Advanced_Returns2()
        {
            Assert.Equal(2.0m, GetDifficultyWeight(DifficultyLevel.Advanced));
        }

        [Fact]
        public void GetDifficultyWeight_Intermediate_Returns1Point5()
        {
            Assert.Equal(1.5m, GetDifficultyWeight(DifficultyLevel.Intermediate));
        }

        [Fact]
        public void GetDifficultyWeight_Beginner_Returns1()
        {
            Assert.Equal(1.0m, GetDifficultyWeight(DifficultyLevel.Beginner));
        }

        [Fact]
        public void CalculateWeightedDrillAvg_MixedDifficulties_ReturnsWeightedResult()
        {
            var drills = new[]
            {
            (FinalScore: 7.0m, Difficulty: DifficultyLevel.Beginner),
            (FinalScore: 8.0m, Difficulty: DifficultyLevel.Advanced)
        };

            var avg = CalculateWeightedDrillAvg(drills);

            // Beginner: 7 * 10 * 1.0 = 70, Advanced: 8 * 10 * 2.0 = 160
            // Total weight: 1.0 + 2.0 = 3.0
            // Result: (70 + 160) / 3.0 = 76.67
            Assert.Equal(Math.Round(230m / 3.0m, 10), Math.Round(avg, 10));
        }
        [Fact]
        public void UpdateOverallRating_ShouldCalculateAverageCorrectly()
        {
            // Arrange
            var playerCard = new PlayerCard
            {
                CategoryRatings =
                [
                    new PlayerCategoryRating { Score = 80 },
            new PlayerCategoryRating { Score = 90 },
            new PlayerCategoryRating { Score = 100 }
                ]
            };

            // Act
            UpdateOverallRating(playerCard);

            // Assert
            Assert.Equal(90m, playerCard.OverallRating);
        }

        [Fact]
        public void UpdateCategoryRatings_ShouldUpdateExistingRating()
        {
            // Arrange
            var playerCard = new PlayerCard
            {
                CategoryRatings =
                [
                    new PlayerCategoryRating
            {
                DrillCategoryId = 1,
                Score = 20
            }
                ]
            };

            var lookups = new RatingLookups
            {
                Drill = new()
        {
            {1,80}
        },

                Training = new()
        {
            {1,90}
        },

                Tournament = new()
        {
            {1,95}
        }
            };

            // Act
            UpdateCategoryRatings(
                playerCard,
                lookups);

            // Assert
            Assert.Single(playerCard.CategoryRatings);

            Assert.NotEqual(
                20,
                playerCard.CategoryRatings.First().Score);
        }
        [Fact]
        public void UpdateTransferClassification_WithLessThanFiveRecords_ShouldBeInsufficientData()
        {
            // Arrange
            var playerCard = new PlayerCard();

            List<CategoryAggregate> drills =
            [
                new CategoryAggregate
        {
            CategoryId = 1,
            Name = "Passing",
            Count = 2
        }
            ];

            List<CategoryAggregate> training =
            [
                new CategoryAggregate
        {
            CategoryId = 1,
            Name = "Passing",
            Count = 1
        }
            ];

            List<CategoryAggregate> tournament =
            [
                new CategoryAggregate
        {
            CategoryId = 1,
            Name = "Passing",
            Count = 1
        }
            ];

            // Act
            UpdateTransferClassification(
                playerCard,
                drills,
                training,
                tournament);

            // Assert
            Assert.Equal(
                TransferClassification.InsufficientData,
                playerCard.TransferClassification);
        }
        [Fact]
        public void BuildRatingLookups_ShouldCreateDictionaries()
        {
            // Arrange
            List<CategoryAggregate> drills =
            [
                new CategoryAggregate
        {
            CategoryId = 1,
            Name = "Passing",
            WeightedSum = 150,
            TotalWeight = 2
        }
            ];

            List<CategoryAggregate> training =
            [
                new CategoryAggregate
        {
            CategoryId = 1,
            Name = "Passing",
            Avg = 80
        }
            ];

            List<CategoryAggregate> tournament =
            [
                new CategoryAggregate
        {
            CategoryId = 1,
            Name = "Passing",
            Avg = 70
        }
            ];

            // Act
            var result = BuildRatingLookups(
                drills,
                training,
                tournament);

            // Assert
            Assert.Equal(75m, result.Drill[1]);
            Assert.Equal(80m, result.Training[1]);
            Assert.Equal(70m, result.Tournament[1]);
        }
        [Fact]
        public void UpdateCategoryRatings_ShouldAddCategoryWhenNotExists()
        {
            // Arrange
            var playerCard = new PlayerCard();

            var lookups = new RatingLookups
            {
                Drill = new Dictionary<int, decimal>
        {
            { 1, 80 }
        },

                Training = new Dictionary<int, decimal>
        {
            { 1, 90 }
        },

                Tournament = new Dictionary<int, decimal>
        {
            { 1, 85 }
        }
            };

            // Act
            UpdateCategoryRatings(
                playerCard,
                lookups);

            // Assert
            Assert.Single(playerCard.CategoryRatings);
        }


    }
}
