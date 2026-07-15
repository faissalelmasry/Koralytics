using Koralytics.Application.DTOs.Drill;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Services.Drill.DrillAnalytic;
using Koralytics.Domain.Entities.Coach;
using Koralytics.Domain.Entities.Match;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Koralytics.Application.UnitTests.Drill
{
    public class DrillAnalyticsServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly DrillAnalyticsService _service;

        public DrillAnalyticsServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _service = new DrillAnalyticsService(_unitOfWorkMock.Object);
        }

        // ================================================================
        // 1. GetSquadWeakCategoriesAsync Tests
        // ================================================================

        [Fact]
        public async Task GetSquadWeakCategoriesAsync_ReturnsWeakestFirst_MathIsCorrect()
        {
            // --- ARRANGE ---
            var rawResults = new List<Domain.Entities.Drill.DrillResult>
            {
                // Shooting: Avg = (4 + 6) / 2 = 5.0
                CreateDrillResultForCategory(teamId: 10, categoryName: "Shooting", score: 4),
                CreateDrillResultForCategory(teamId: 10, categoryName: "Shooting", score: 6),
                
                // Passing: Avg = (8 + 10) / 2 = 9.0
                CreateDrillResultForCategory(teamId: 10, categoryName: "Passing", score: 8),
                CreateDrillResultForCategory(teamId: 10, categoryName: "Passing", score: 10),

                // Different Team (Should be ignored by the filter)
                CreateDrillResultForCategory(teamId: 99, categoryName: "Defending", score: 2)
            };

            var repoMock = new Mock<IRepository<Domain.Entities.Drill.DrillResult>>();
            repoMock.Setup(r => r.GetQueryableAsNoTracking()).Returns(rawResults.BuildMock());
            _unitOfWorkMock.Setup(u => u.Repository<Domain.Entities.Drill.DrillResult>()).Returns(repoMock.Object);

            // --- ACT ---
            var result = await _service.GetSquadWeakCategoriesAsync(teamId: 10);
            var listResult = result.ToList();

            // --- ASSERT ---
            Assert.Equal(2, listResult.Count); // Defending is ignored because it's team 99

            // The weakest category MUST be first in the list
            Assert.Equal("Shooting", listResult[0].CategoryName);
            Assert.Equal(5.0m, listResult[0].AverageScore);

            Assert.Equal("Passing", listResult[1].CategoryName);
            Assert.Equal(9.0m, listResult[1].AverageScore);
        }

        // ================================================================
        // 2. DetectCoachBiasAsync Tests (Security & Math)
        // ================================================================

        [Fact]
        public async Task DetectCoachBiasAsync_CoachViewingAnotherCoach_ThrowsUnauthorized()
        {
            // --- ACT & ASSERT ---
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.DetectCoachBiasAsync(targetCoachId: 2, academyId: 1, currentUserId: 1, currentUserRole: "Coach"));
        }

        [Fact]
        public async Task DetectCoachBiasAsync_NoPracticeData_Returns100PercentTrust()
        {
            // --- ARRANGE ---
            var emptyPracticeResults = new List<Domain.Entities.Drill.DrillResult>().BuildMock();
            var repoMock = new Mock<IRepository<Domain.Entities.Drill.DrillResult>>();
            repoMock.Setup(r => r.GetQueryableAsNoTracking()).Returns(emptyPracticeResults);
            _unitOfWorkMock.Setup(u => u.Repository<Domain.Entities.Drill.DrillResult>()).Returns(repoMock.Object);

            // --- ACT ---
            var result = await _service.DetectCoachBiasAsync(targetCoachId: 1, academyId: 1, currentUserId: 1, currentUserRole: "Coach");

            // --- ASSERT ---
            Assert.Equal(100, result.TrustPercentage);
            Assert.Equal(0, result.PlayersAnalyzedCount);
            Assert.Contains("Insufficient practice data", result.Remarks);
        }

        [Fact]
        public async Task DetectCoachBiasAsync_CalculatesTrustMathCorrectly()
        {
            // --- ARRANGE ---
            // 1. Setup Practice Scores (Subjective) -> Coach gives Player 1 an 8, Player 2 a 9
            var practiceScores = new List<Domain.Entities.Drill.DrillResult>
            {
                new Domain.Entities.Drill.DrillResult { PlayerId = 1, FinalScore = 8, CreatedById = 1, CreatedAt = DateTime.UtcNow, Drill = new Domain.Entities.Drill.Drill { Mode = Koralytics.Domain.Enums.DrillMode.Manual } },
                new Domain.Entities.Drill.DrillResult { PlayerId = 2, FinalScore = 9, CreatedById = 1, CreatedAt = DateTime.UtcNow, Drill = new Domain.Entities.Drill.Drill { Mode = Koralytics.Domain.Enums.DrillMode.Manual } }
            };
            var practiceRepoMock = new Mock<IRepository<Domain.Entities.Drill.DrillResult>>();
            practiceRepoMock.Setup(r => r.GetQueryableAsNoTracking()).Returns(practiceScores.BuildMock());
            _unitOfWorkMock.Setup(u => u.Repository<Domain.Entities.Drill.DrillResult>()).Returns(practiceRepoMock.Object);

            // 2. Setup Match Scores (Objective) -> Match gives Player 1 a 6, Player 2 a 7
            var matchScores = new List<MatchPlayerRating>
            {
                new MatchPlayerRating
                {
                    PlayerId = 1,
                    CreatedAt = DateTime.UtcNow,
                    CategoryRatings = new List<MatchPlayerCategoryRating> { new MatchPlayerCategoryRating { Rating = 6 } }
                },
                new MatchPlayerRating
                {
                    PlayerId = 2,
                    CreatedAt = DateTime.UtcNow,
                    CategoryRatings = new List<MatchPlayerCategoryRating> { new MatchPlayerCategoryRating { Rating = 7 } }
                }
            };
            var matchRepoMock = new Mock<IRepository<MatchPlayerRating>>();
            matchRepoMock.Setup(r => r.GetQueryableAsNoTracking()).Returns(matchScores.BuildMock());
            _unitOfWorkMock.Setup(u => u.Repository<MatchPlayerRating>()).Returns(matchRepoMock.Object);

            // 3. Setup Coach Academy Profile (To verify it gets updated)
            var coachAcademy = new CoachAcademy { CoachUserId = 1, AcademyId = 1, BiasScore = 0 };
            var coachAcademyRepoMock = new Mock<IRepository<CoachAcademy>>();
            coachAcademyRepoMock.Setup(r => r.GetQueryable()).Returns(new List<CoachAcademy> { coachAcademy }.BuildMock());
            _unitOfWorkMock.Setup(u => u.Repository<CoachAcademy>()).Returns(coachAcademyRepoMock.Object);

            // THE MATH:
            // Delta 1 = |8 - 6| = 2
            // Delta 2 = |9 - 7| = 2
            // Avg Delta = 2
            // Formula: 100 - (2 * 10) = 80% Trust Score

            // --- ACT ---
            // AcademyAdmin viewing Coach 1 (Passes security check)
            var result = await _service.DetectCoachBiasAsync(targetCoachId: 1, academyId: 1, currentUserId: 99, currentUserRole: "AcademyAdmin");

            // --- ASSERT ---
            Assert.Equal(80m, result.TrustPercentage);
            Assert.Equal(2, result.PlayersAnalyzedCount);

            // Verify the Database Profile was updated correctly
            Assert.Equal(80m, coachAcademy.BiasScore);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DetectCoachBiasAsync_InsaneBias_TrustDoesNotGoBelowZero()
        {
            // --- ARRANGE ---
            var practiceScores = new List<Domain.Entities.Drill.DrillResult>
            {
                // Coach gives a 10
                new Domain.Entities.Drill.DrillResult { PlayerId = 1, FinalScore = 10, CreatedById = 1, CreatedAt = DateTime.UtcNow, Drill = new Domain.Entities.Drill.Drill { Mode = Koralytics.Domain.Enums.DrillMode.Manual } }
            };
            var practiceRepoMock = new Mock<IRepository<Domain.Entities.Drill.DrillResult>>();
            practiceRepoMock.Setup(r => r.GetQueryableAsNoTracking()).Returns(practiceScores.BuildMock());
            _unitOfWorkMock.Setup(u => u.Repository<Domain.Entities.Drill.DrillResult>()).Returns(practiceRepoMock.Object);

            var matchScores = new List<MatchPlayerRating>
            {
                // Real match is a 0 (Delta is 10. Formula: 100 - (10*10) = 0)
                new MatchPlayerRating { PlayerId = 1,
CategoryRatings = new List<MatchPlayerCategoryRating> { new MatchPlayerCategoryRating { Rating = -5 } }                    , CreatedAt = DateTime.UtcNow } // A negative rating would technically make trust drop to -50%
            };
            var matchRepoMock = new Mock<IRepository<MatchPlayerRating>>();
            matchRepoMock.Setup(r => r.GetQueryableAsNoTracking()).Returns(matchScores.BuildMock());
            _unitOfWorkMock.Setup(u => u.Repository<MatchPlayerRating>()).Returns(matchRepoMock.Object);

            var coachAcademyRepoMock = new Mock<IRepository<CoachAcademy>>();
            coachAcademyRepoMock.Setup(r => r.GetQueryable()).Returns(new List<CoachAcademy>().BuildMock());
            _unitOfWorkMock.Setup(u => u.Repository<CoachAcademy>()).Returns(coachAcademyRepoMock.Object);

            // --- ACT ---
            var result = await _service.DetectCoachBiasAsync(targetCoachId: 1, academyId: 1, currentUserId: 1, currentUserRole: "Coach");

            // --- ASSERT ---
            // Ensure Math.Max(0, ...) did its job and stopped it at 0
            Assert.Equal(0m, result.TrustPercentage);
        }

        // ================================================================
        // Helper Method
        // ================================================================
        private Domain.Entities.Drill.DrillResult CreateDrillResultForCategory(int teamId, string categoryName, decimal score)
        {
            return new Domain.Entities.Drill.DrillResult
            {
                FinalScore = score,
                Drill = new Domain.Entities.Drill.Drill
                {
                    DrillSession = new Domain.Entities.Drill.DrillSession { TeamId = teamId },
                    DrillTemplate = new Domain.Entities.Drill.DrillTemplate
                    {
                        DrillCategory = new Domain.Entities.Drill.DrillCategory { Name = categoryName }
                    }
                }
            };
        }
    }
}