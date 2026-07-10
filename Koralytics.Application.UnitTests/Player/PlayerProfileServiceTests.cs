using AutoMapper;
using Koralytics.Application.DTOs.Player;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Services.Player.PlayerCardService;
using Koralytics.Application.Services.Player.PlayerProfileServices;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Drill;
using Koralytics.Domain.Entities.Match;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using MatchTypeEnum = Koralytics.Domain.Enums.MatchType;
using DrillEntity = Koralytics.Domain.Entities.Drill.Drill;
using MatchEntity = Koralytics.Domain.Entities.Match.Match;
using PlayerAchievementEntity = Koralytics.Domain.Entities.Player.PlayerAchievement;
using PlayerEntity = Koralytics.Domain.Entities.Player.Player;

namespace Koralytics.Application.UnitTests.Player
{
    public class PlayerProfileServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IPlayerCardService> _playerCardServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<PlayerProfileService>> _loggerMock;
        private readonly PlayerProfileService _service;

        public PlayerProfileServiceTests()
        {
            _unitOfWorkMock = new();
            _playerCardServiceMock = new();
            _mapperMock = new();
            _loggerMock = new();

            _service = new PlayerProfileService(
                _unitOfWorkMock.Object,
                _playerCardServiceMock.Object,
                _loggerMock.Object,
                _mapperMock.Object);
        }

        // ================================================================
        // GetDrillTimelineAsync
        // ================================================================

        [Fact]
        public async Task GetDrillTimelineAsync_PlayerNotFound_ThrowsNotFoundException()
        {
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PlayerEntity, bool>>>()))
                .ReturnsAsync(false);
            _unitOfWorkMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.GetDrillTimelineAsync(1));
        }

        [Fact]
        public async Task GetDrillTimelineAsync_EmptyResults_ReturnsEmptyListAndZeroCount()
        {
            SetupPlayerExists();
            var drillResults = new List<DrillResult>();
            var queryable = drillResults.BuildMock();
            var drillRepo = new Mock<IRepository<DrillResult>>();
            drillRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            _unitOfWorkMock.Setup(u => u.Repository<DrillResult>()).Returns(drillRepo.Object);

            var result = await _service.GetDrillTimelineAsync(1);

            Assert.NotNull(result);
            Assert.Empty(result.Events);
            Assert.Equal(0, result.TotalCount);
            Assert.Equal(1, result.Page);
        }

        [Fact]
        public async Task GetDrillTimelineAsync_ReturnsPaginatedWithCorrectOrder()
        {
            SetupPlayerExists();
            var drills = new List<DrillResult>
            {
                CreateDrillResult(drillId: 1, sessionDate: new DateTime(2025, 1, 10)),
                CreateDrillResult(drillId: 2, sessionDate: new DateTime(2025, 3, 15)),
                CreateDrillResult(drillId: 3, sessionDate: new DateTime(2025, 2, 20)),
                CreateDrillResult(drillId: 4, sessionDate: new DateTime(2024, 12, 5)),
            };
            var queryable = drills.BuildMock();
            var drillRepo = new Mock<IRepository<DrillResult>>();
            drillRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            _unitOfWorkMock.Setup(u => u.Repository<DrillResult>()).Returns(drillRepo.Object);

            var result = await _service.GetDrillTimelineAsync(1, page: 1, pageSize: 2);

            Assert.Equal(4, result.TotalCount);
            Assert.Equal(2, result.Events.Count);
            Assert.Equal(new DateTime(2025, 3, 15), result.Events[0].Date);
            Assert.Equal(new DateTime(2025, 2, 20), result.Events[1].Date);
        }

        [Fact]
        public async Task GetDrillTimelineAsync_PageOutOfRange_ReturnsEmptyList()
        {
            SetupPlayerExists();
            var drills = new List<DrillResult>
            {
                CreateDrillResult(drillId: 1, sessionDate: new DateTime(2025, 1, 1)),
            };
            var queryable = drills.BuildMock();
            var drillRepo = new Mock<IRepository<DrillResult>>();
            drillRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            _unitOfWorkMock.Setup(u => u.Repository<DrillResult>()).Returns(drillRepo.Object);

            var result = await _service.GetDrillTimelineAsync(1, page: 99, pageSize: 10);

            Assert.Equal(1, result.TotalCount);
            Assert.Empty(result.Events);
        }

        [Fact]
        public async Task GetDrillTimelineAsync_RespectsPageSize()
        {
            SetupPlayerExists();
            var drills = Enumerable.Range(0, 30).Select(i =>
                CreateDrillResult(drillId: i, sessionDate: new DateTime(2025, 1, 1).AddDays(i)))
                .ToList();
            var queryable = drills.BuildMock();
            var drillRepo = new Mock<IRepository<DrillResult>>();
            drillRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            _unitOfWorkMock.Setup(u => u.Repository<DrillResult>()).Returns(drillRepo.Object);

            var result = await _service.GetDrillTimelineAsync(1, page: 2, pageSize: 5);

            Assert.Equal(30, result.TotalCount);
            Assert.Equal(5, result.Events.Count);
            Assert.Equal(2, result.Page);
            Assert.Equal(5, result.PageSize);
        }

        [Fact]
        public async Task GetDrillTimelineAsync_Projection_VerifyAllFields()
        {
            SetupPlayerExists();
            var drill = CreateDrillResult(
                drillId: 1,
                sessionDate: new DateTime(2025, 6, 15),
                sessionType: SessionType.PreSeason,
                sessionNotes: "Great session",
                categoryName: "Passing",
                templateName: "Short Pass",
                finalScore: 85.5m,
                coachNotes: "Keep improving"
            );
            var drills = new List<DrillResult> { drill };
            var queryable = drills.BuildMock();
            var drillRepo = new Mock<IRepository<DrillResult>>();
            drillRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            _unitOfWorkMock.Setup(u => u.Repository<DrillResult>()).Returns(drillRepo.Object);

            var result = await _service.GetDrillTimelineAsync(1);

            var evt = Assert.Single(result.Events);
            Assert.Equal(new DateTime(2025, 6, 15), evt.Date);
            Assert.Equal("Passing", evt.Title);
            Assert.Equal("Great session", evt.Description);
            Assert.Equal("PreSeason", evt.SessionType);
            Assert.Equal("Passing", evt.DrillCategoryName);
            Assert.Equal("Short Pass", evt.DrillTemplateName);
            Assert.Equal(85.5m, evt.FinalScore);
            Assert.Equal("Keep improving", evt.DrillNotes);
        }

        // ================================================================
        // GetMatchTimelineAsync
        // ================================================================

        [Fact]
        public async Task GetMatchTimelineAsync_PlayerNotFound_ThrowsNotFoundException()
        {
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PlayerEntity, bool>>>()))
                .ReturnsAsync(false);
            _unitOfWorkMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.GetMatchTimelineAsync(1));
        }

        [Fact]
        public async Task GetMatchTimelineAsync_EmptyResults_ReturnsEmptyListAndZeroCount()
        {
            SetupPlayerExists();
            var ratings = new List<MatchPlayerRating>();
            var queryable = ratings.BuildMock();
            var matchRepo = new Mock<IRepository<MatchPlayerRating>>();
            matchRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            _unitOfWorkMock.Setup(u => u.Repository<MatchPlayerRating>()).Returns(matchRepo.Object);

            var result = await _service.GetMatchTimelineAsync(1);

            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Events);
        }

        [Fact]
        public async Task GetMatchTimelineAsync_ReturnsPaginatedWithCorrectOrder()
        {
            SetupPlayerExists();
            var ratings = new List<MatchPlayerRating>
            {
                CreateMatchRating(matchId: 1, matchDate: new DateTime(2025, 1, 10)),
                CreateMatchRating(matchId: 2, matchDate: new DateTime(2025, 5, 20)),
                CreateMatchRating(matchId: 3, matchDate: new DateTime(2025, 3, 15)),
            };
            var queryable = ratings.BuildMock();
            var matchRepo = new Mock<IRepository<MatchPlayerRating>>();
            matchRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            _unitOfWorkMock.Setup(u => u.Repository<MatchPlayerRating>()).Returns(matchRepo.Object);

            var result = await _service.GetMatchTimelineAsync(1, pageSize: 2);

            Assert.Equal(3, result.TotalCount);
            Assert.Equal(2, result.Events.Count);
            Assert.Equal(new DateTime(2025, 5, 20), result.Events[0].Date);
            Assert.Equal(new DateTime(2025, 3, 15), result.Events[1].Date);
        }

        [Fact]
        public async Task GetMatchTimelineAsync_Projection_VerifyTitleAndDescription()
        {
            SetupPlayerExists();
            var rating = CreateMatchRating(
                matchId: 1,
                matchDate: new DateTime(2025, 1, 1),
                homeTeam: "Al Ahly",
                awayTeam: "Zamalek",
                homeScore: 2,
                awayScore: 1,
                matchType: MatchTypeEnum.Tournament
            );
            var ratings = new List<MatchPlayerRating> { rating };
            var queryable = ratings.BuildMock();
            var matchRepo = new Mock<IRepository<MatchPlayerRating>>();
            matchRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            _unitOfWorkMock.Setup(u => u.Repository<MatchPlayerRating>()).Returns(matchRepo.Object);

            var result = await _service.GetMatchTimelineAsync(1);

            var evt = Assert.Single(result.Events);
            Assert.Equal("Al Ahly vs Zamalek", evt.Title);
            Assert.Equal("2 - 1", evt.Description);
            Assert.Equal(2, evt.HomeScore);
            Assert.Equal(1, evt.AwayScore);
            Assert.Equal("Tournament", evt.MatchType);
        }

        [Fact]
        public async Task GetMatchTimelineAsync_MatchWithZeroScore_DescriptionIsNull()
        {
            SetupPlayerExists();
            var rating = CreateMatchRating(
                matchId: 1,
                matchDate: new DateTime(2025, 1, 1),
                homeTeam: "Team A",
                awayTeam: "Team B",
                homeScore: 0,
                awayScore: 0
            );
            var ratings = new List<MatchPlayerRating> { rating };
            var queryable = ratings.BuildMock();
            var matchRepo = new Mock<IRepository<MatchPlayerRating>>();
            matchRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            _unitOfWorkMock.Setup(u => u.Repository<MatchPlayerRating>()).Returns(matchRepo.Object);

            var result = await _service.GetMatchTimelineAsync(1);

            var evt = Assert.Single(result.Events);
            Assert.Null(evt.Description);
        }

        [Fact]
        public async Task GetMatchTimelineAsync_CategoryRatings_AverageIsComputed()
        {
            SetupPlayerExists();
            var rating = CreateMatchRating(
                matchId: 1,
                matchDate: new DateTime(2025, 1, 1),
                homeTeam: "A", awayTeam: "B"
            );
            rating.CategoryRatings = new List<MatchPlayerCategoryRating>
            {
                new() { Rating = 8.5m },
                new() { Rating = 9.0m },
                new() { Rating = 7.0m },
            };
            var ratings = new List<MatchPlayerRating> { rating };
            var queryable = ratings.BuildMock();
            var matchRepo = new Mock<IRepository<MatchPlayerRating>>();
            matchRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            _unitOfWorkMock.Setup(u => u.Repository<MatchPlayerRating>()).Returns(matchRepo.Object);

            var result = await _service.GetMatchTimelineAsync(1);

            var evt = Assert.Single(result.Events);
            Assert.Equal(8.1667m, Math.Round(evt.Rating!.Value, 4));
        }

        [Fact]
        public async Task GetMatchTimelineAsync_NoCategoryRatings_RatingIsNull()
        {
            SetupPlayerExists();
            var rating = CreateMatchRating(
                matchId: 1,
                matchDate: new DateTime(2025, 1, 1),
                homeTeam: "A", awayTeam: "B"
            );
            rating.CategoryRatings = new List<MatchPlayerCategoryRating>();
            var ratings = new List<MatchPlayerRating> { rating };
            var queryable = ratings.BuildMock();
            var matchRepo = new Mock<IRepository<MatchPlayerRating>>();
            matchRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            _unitOfWorkMock.Setup(u => u.Repository<MatchPlayerRating>()).Returns(matchRepo.Object);

            var result = await _service.GetMatchTimelineAsync(1);

            var evt = Assert.Single(result.Events);
            Assert.Null(evt.Rating);
        }

        // ================================================================
        // GetAchievementTimelineAsync
        // ================================================================

        [Fact]
        public async Task GetAchievementTimelineAsync_PlayerNotFound_ThrowsNotFoundException()
        {
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PlayerEntity, bool>>>()))
                .ReturnsAsync(false);
            _unitOfWorkMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.GetAchievementTimelineAsync(1));
        }

        [Fact]
        public async Task GetAchievementTimelineAsync_EmptyResults_ReturnsEmptyListAndZeroCount()
        {
            SetupPlayerExists();
            var achievements = new List<PlayerAchievementEntity>();
            var queryable = achievements.BuildMock();
            var achievementRepo = new Mock<IRepository<PlayerAchievementEntity>>();
            achievementRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            _unitOfWorkMock.Setup(u => u.Repository<PlayerAchievementEntity>()).Returns(achievementRepo.Object);

            var result = await _service.GetAchievementTimelineAsync(1);

            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Events);
        }

        [Fact]
        public async Task GetAchievementTimelineAsync_ReturnsPaginatedWithCorrectOrder()
        {
            SetupPlayerExists();
            var achievements = new List<PlayerAchievementEntity>
            {
                new() { Id = 1, PlayerId = 1, AchievementType = "MVP", ReferenceType = "Match", AwardedAt = new DateTime(2025, 1, 1) },
                new() { Id = 2, PlayerId = 1, AchievementType = "Top Scorer", ReferenceType = "Season", AwardedAt = new DateTime(2025, 6, 1) },
            };
            var queryable = achievements.BuildMock();
            var achievementRepo = new Mock<IRepository<PlayerAchievementEntity>>();
            achievementRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            _unitOfWorkMock.Setup(u => u.Repository<PlayerAchievementEntity>()).Returns(achievementRepo.Object);

            var result = await _service.GetAchievementTimelineAsync(1);

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Events.Count);
            Assert.Equal(new DateTime(2025, 6, 1), result.Events[0].Date);
        }

        [Fact]
        public async Task GetAchievementTimelineAsync_Projection_VerifyAllFields()
        {
            SetupPlayerExists();
            var achievements = new List<PlayerAchievementEntity>
            {
                new()
                {
                    Id = 42, PlayerId = 1, AchievementType = "Golden Boot",
                    ReferenceType = "Tournament", AwardedAt = new DateTime(2025, 3, 1),
                },
            };
            var queryable = achievements.BuildMock();
            var achievementRepo = new Mock<IRepository<PlayerAchievementEntity>>();
            achievementRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            _unitOfWorkMock.Setup(u => u.Repository<PlayerAchievementEntity>()).Returns(achievementRepo.Object);

            var result = await _service.GetAchievementTimelineAsync(1);

            var evt = Assert.Single(result.Events);
            Assert.Equal("Golden Boot", evt.Title);
            Assert.Equal("Tournament", evt.Description);
            Assert.Equal(42, evt.AchievementId);
            Assert.Equal("Golden Boot", evt.AchievementType);
            Assert.Equal(new DateTime(2025, 3, 1), evt.Date);
        }

        // ================================================================
        // GetPlayerProfileAsync
        // ================================================================

        [Fact]
        public async Task GetPlayerProfileAsync_PlayerNotFound_ThrowsNotFoundException()
        {
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(new List<PlayerEntity>().BuildMock());
            _unitOfWorkMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.GetPlayerProfileAsync(1));
        }

        [Fact]
        public async Task GetPlayerProfileAsync_PlayerCardNotFound_ThrowsNotFoundException()
        {
            var player = CreatePlayerEntity();
            var players = new List<PlayerEntity> { player };
            var playerQueryable = players.BuildMock();
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(playerQueryable);
            _unitOfWorkMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            _playerCardServiceMock
                .Setup(s => s.GetPlayerCardAsync(1))
                .ReturnsAsync((PlayerCardDto?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.GetPlayerProfileAsync(1));
        }

        [Fact]
        public async Task GetPlayerProfileAsync_MatchStats_AggregatedCorrectly()
        {
            var player = CreatePlayerEntity();
            var players = new List<PlayerEntity> { player };
            var playerQueryable = players.BuildMock();
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(playerQueryable);
            _unitOfWorkMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            _playerCardServiceMock
                .Setup(s => s.GetPlayerCardAsync(1))
                .ReturnsAsync(new PlayerCardDto());

            var matchRatings = new List<MatchPlayerRating>
            {
                CreateMatchRating(1, new DateTime(2025, 1, 1), matchType: MatchTypeEnum.Session, goals: 2, assists: 1, isMOTM: true),
                CreateMatchRating(2, new DateTime(2025, 2, 1), matchType: MatchTypeEnum.Session, goals: 0, assists: 2, isMOTM: false),
                CreateMatchRating(3, new DateTime(2025, 3, 1), matchType: MatchTypeEnum.Friendly, goals: 1, assists: 0, isMOTM: false),
                CreateMatchRating(4, new DateTime(2025, 4, 1), matchType: MatchTypeEnum.Friendly, goals: 1, assists: 0, isMOTM: true),
                CreateMatchRating(5, new DateTime(2025, 5, 1), matchType: MatchTypeEnum.Tournament, goals: 3, assists: 1, isMOTM: true),
            };
            var matchQueryable = matchRatings.BuildMock();
            var matchRepo = new Mock<IRepository<MatchPlayerRating>>();
            matchRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(matchQueryable);
            _unitOfWorkMock.Setup(u => u.Repository<MatchPlayerRating>()).Returns(matchRepo.Object);

            SetupMapperForProfile(player);

            var result = await _service.GetPlayerProfileAsync(1);

            Assert.Equal(5, result.TotalMatches);
            Assert.Equal(7, result.TotalGoals);
            Assert.Equal(4, result.TotalAssists);
            Assert.Equal(3, result.TotalMOTMs);
            Assert.Equal(2, result.SessionStats.Goals);
            Assert.Equal(2, result.FriendlyStats.Goals);
            Assert.Equal(3, result.TournamentStats.Goals);
        }

        [Fact]
        public async Task GetPlayerProfileAsync_TotalMatches_CountsDistinctMatchIds()
        {
            var player = CreatePlayerEntity();
            var players = new List<PlayerEntity> { player };
            var playerQueryable = players.BuildMock();
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(playerQueryable);
            _unitOfWorkMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            _playerCardServiceMock
                .Setup(s => s.GetPlayerCardAsync(1))
                .ReturnsAsync(new PlayerCardDto());

            var matchRatings = new List<MatchPlayerRating>
            {
                CreateMatchRating(1, new DateTime(2025, 1, 1), matchType: MatchTypeEnum.Session),
                CreateMatchRating(1, new DateTime(2025, 1, 1), matchType: MatchTypeEnum.Session),
                CreateMatchRating(2, new DateTime(2025, 2, 1), matchType: MatchTypeEnum.Friendly),
            };
            var matchQueryable = matchRatings.BuildMock();
            var matchRepo = new Mock<IRepository<MatchPlayerRating>>();
            matchRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(matchQueryable);
            _unitOfWorkMock.Setup(u => u.Repository<MatchPlayerRating>()).Returns(matchRepo.Object);

            SetupMapperForProfile(player);

            var result = await _service.GetPlayerProfileAsync(1);

            Assert.Equal(2, result.TotalMatches);
        }

        // ================================================================
        // GetPlayerVsAcademyAverageAsync
        // ================================================================

        [Fact]
        public async Task GetPlayerVsAcademyAverageAsync_PlayerNotFound_ThrowsNotFoundException()
        {
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(new List<PlayerEntity>().BuildMock());
            _unitOfWorkMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.GetPlayerVsAcademyAverageAsync(1, 1));
        }

        [Fact]
        public async Task GetPlayerVsAcademyAverageAsync_AcademyNotFound_ThrowsNotFoundException()
        {
            var player = CreatePlayerEntity();
            var players = new List<PlayerEntity> { player };
            var playerQueryable = players.BuildMock();
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(playerQueryable);
            _unitOfWorkMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            var academyRepo = new Mock<IRepository<Academy>>();
            academyRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(new List<Academy>().BuildMock());
            _unitOfWorkMock.Setup(u => u.Repository<Academy>()).Returns(academyRepo.Object);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.GetPlayerVsAcademyAverageAsync(1, 999));
        }

        [Fact]
        public async Task GetPlayerVsAcademyAverageAsync_NoMatchingTeams_ReturnsOnlyPlayerCategories()
        {
            var player = CreatePlayerEntity(academyId: 5);
            var players = new List<PlayerEntity> { player };
            var playerQueryable = players.BuildMock();
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(playerQueryable);
            _unitOfWorkMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            var academyRepo = new Mock<IRepository<Academy>>();
            var academies = new List<Academy> { new() { Id = 1, Name = "Other Academy" } };
            academyRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(academies.BuildMock());
            _unitOfWorkMock.Setup(u => u.Repository<Academy>()).Returns(academyRepo.Object);

            var playerCard = new PlayerCard { Id = 1, PlayerId = 1 };
            var categoryRatings = new List<PlayerCategoryRating>
            {
                new() { PlayerCardId = 1, PlayerCard = playerCard, DrillCategoryId = 1, Score = 80m },
                new() { PlayerCardId = 1, PlayerCard = playerCard, DrillCategoryId = 2, Score = 90m },
            };
            var categoryRepo = new Mock<IRepository<PlayerCategoryRating>>();
            categoryRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(categoryRatings.BuildMock());
            _unitOfWorkMock.Setup(u => u.Repository<PlayerCategoryRating>()).Returns(categoryRepo.Object);

            var categoryNames = new List<DrillCategory>
            {
                new() { Id = 1, Name = "Passing" },
                new() { Id = 2, Name = "Shooting" },
            };
            var drillCatRepo = new Mock<IRepository<DrillCategory>>();
            drillCatRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(categoryNames.BuildMock());
            _unitOfWorkMock.Setup(u => u.Repository<DrillCategory>()).Returns(drillCatRepo.Object);

            SetupMapperForVsAcademy();

            var result = await _service.GetPlayerVsAcademyAverageAsync(1, 1);

            Assert.Equal(2, result.Categories.Count);
            Assert.All(result.Categories, c => Assert.Equal(0m, c.AcademyAverage));
        }

        [Fact]
        public async Task GetPlayerVsAcademyAverageAsync_WithMatchingTeams_ReturnsComparisons()
        {
            var team = new Team { Id = 10, AcademyId = 1, AgeGroupId = 1, Name = "U17 Team A" };
            var player = CreatePlayerEntity(academyId: 1, team: team);
            var players = new List<PlayerEntity> { player };
            var playerQueryable = players.BuildMock();
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(playerQueryable);
            _unitOfWorkMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            var academyRepo = new Mock<IRepository<Academy>>();
            var academies = new List<Academy> { new() { Id = 1, Name = "My Academy" } };
            academyRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(academies.BuildMock());
            _unitOfWorkMock.Setup(u => u.Repository<Academy>()).Returns(academyRepo.Object);

            // Player averages
            var playerCard = new PlayerCard { Id = 1, PlayerId = 1 };
            var playerRatings = new List<PlayerCategoryRating>
            {
                new() { PlayerCardId = 1, PlayerCard = playerCard, DrillCategoryId = 1, Score = 80m },
            };
            var playerCatRepo = new Mock<IRepository<PlayerCategoryRating>>();
            playerCatRepo.SetupSequence(r => r.GetQueryableAsNoTracking())
                .Returns(playerRatings.BuildMock())        // first call: player averages
                .Returns(new List<PlayerCategoryRating>().BuildMock()); // second call: academy averages
            _unitOfWorkMock.Setup(u => u.Repository<PlayerCategoryRating>()).Returns(playerCatRepo.Object);

            var teamRepo = new Mock<IRepository<Team>>();
            var teams = new List<Team> { team };
            teamRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(teams.BuildMock());
            _unitOfWorkMock.Setup(u => u.Repository<Team>()).Returns(teamRepo.Object);

            var playerTeams = new List<PlayerTeam>
            {
                new() { PlayerId = 100, TeamId = 10, Team = team },
            };
            var playerTeamRepo = new Mock<IRepository<PlayerTeam>>();
            playerTeamRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(playerTeams.BuildMock());
            _unitOfWorkMock.Setup(u => u.Repository<PlayerTeam>()).Returns(playerTeamRepo.Object);

            var categoryNames = new List<DrillCategory>
            {
                new() { Id = 1, Name = "Passing" },
            };
            var drillCatRepo = new Mock<IRepository<DrillCategory>>();
            drillCatRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(categoryNames.BuildMock());
            _unitOfWorkMock.Setup(u => u.Repository<DrillCategory>()).Returns(drillCatRepo.Object);

            SetupMapperForVsAcademy();

            var result = await _service.GetPlayerVsAcademyAverageAsync(1, 1);

            Assert.Single(result.Categories);
            Assert.Equal(80m, result.Categories[0].PlayerAverage);
        }

        // ================================================================
        // GetScouterViewsCountAsync
        // ================================================================

        [Fact]
        public async Task GetScouterViewsCountAsync_PlayerNotFound_ThrowsNotFoundException()
        {
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PlayerEntity, bool>>>()))
                .ReturnsAsync(false);
            _unitOfWorkMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.GetScouterViewsCountAsync(1, 2025, 1));
        }

        [Fact]
        public async Task GetScouterViewsCountAsync_ReturnsCorrectViewCount()
        {
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PlayerEntity, bool>>>()))
                .ReturnsAsync(true);
            _unitOfWorkMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            var scouterViewsRepo = new Mock<IRepository<ScouterView>>();
            scouterViewsRepo
                .Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ScouterView, bool>>>()))
                .ReturnsAsync(7);
            _unitOfWorkMock.Setup(u => u.Repository<ScouterView>()).Returns(scouterViewsRepo.Object);

            var result = await _service.GetScouterViewsCountAsync(1, 2025, 6);

            Assert.Equal(1, result.PlayerId);
            Assert.Equal(2025, result.Year);
            Assert.Equal(6, result.Month);
            Assert.Equal(7, result.ViewsCount);
        }

        // ================================================================
        // Helpers
        // ================================================================

        private void SetupPlayerExists()
        {
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PlayerEntity, bool>>>()))
                .ReturnsAsync(true);
            _unitOfWorkMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);
        }

        private static DrillResult CreateDrillResult(
            int drillId,
            DateTime sessionDate,
            SessionType sessionType = SessionType.Regular,
            string? sessionNotes = null,
            string categoryName = "Shooting",
            string? templateName = null,
            decimal finalScore = 75m,
            string? coachNotes = null)
        {
            var category = new DrillCategory { Id = 1, Name = categoryName };
            var template = new DrillTemplate { Id = 1, Name = templateName, DrillCategory = category };
            var session = new DrillSession { Id = 1, SessionDate = sessionDate, Type = sessionType, Notes = sessionNotes };
            var drill = new DrillEntity { Id = drillId, SessionId = session.Id, DrillSession = session, DrillTemplate = template, DrillTemplateId = template.Id };
            return new DrillResult { Id = drillId, DrillId = drill.Id, Drill = drill, PlayerId = 1, FinalScore = finalScore, CoachNotes = coachNotes };
        }

        private static MatchPlayerRating CreateMatchRating(
            int matchId,
            DateTime matchDate,
            string homeTeam = "Home FC",
            string awayTeam = "Away FC",
            int homeScore = 1,
            int awayScore = 0,
            MatchTypeEnum matchType = MatchTypeEnum.Session,
            int goals = 0,
            int assists = 0,
            bool isMOTM = false,
            int minutesPlayed = 90,
            string? coachNote = null)
        {
            var ht = new Team { Id = 10, Name = homeTeam, AcademyId = 1, AgeGroupId = 1 };
            var at = new Team { Id = 20, Name = awayTeam, AcademyId = 2, AgeGroupId = 1 };
            var match = new MatchEntity
            {
                Id = matchId, MatchDate = matchDate, Type = matchType,
                HomeScore = homeScore, AwayScore = awayScore,
                HomeTeamId = ht.Id, HomeTeam = ht,
                AwayTeamId = at.Id, AwayTeam = at,
            };
            return new MatchPlayerRating
            {
                Id = matchId, MatchId = matchId, Match = match, PlayerId = 1,
                Goals = goals, Assists = assists, IsMOTM = isMOTM,
                MinutesPlayed = minutesPlayed, CoachNote = coachNote,
                CategoryRatings = new List<MatchPlayerCategoryRating>(),
            };
        }

        private static PlayerEntity CreatePlayerEntity(int academyId = 1, Team? team = null)
        {
            team ??= new Team { Id = 10, AcademyId = academyId, AgeGroupId = 1, Name = "U17 Team A" };
            var ageGroup = new AgeGroup { Id = 1, Name = "U17" };
            team.AgeGroup = ageGroup;

            return new PlayerEntity
            {
                Id = 1,
                FirstName = "Test",
                LastName = "Player",
                DateOfBirth = new DateTime(2008, 1, 1),
                PlayerPositions = new List<PlayerPosition>
                {
                    new() { PlayerId = 1, Position = "ST", IsPrimary = true },
                },
                PlayerAcademies = new List<PlayerAcademy>
                {
                    new()
                    {
                        PlayerId = 1, AcademyId = academyId,
                        Academy = new Academy { Id = academyId, Name = "Test Academy" },
                        Status = PlayerAcademyStatus.Active,
                    },
                },
                PlayerTeams = new List<PlayerTeam>
                {
                    new() { PlayerId = 1, TeamId = team.Id, Team = team },
                },
            };
        }

        private void SetupMapperForProfile(PlayerEntity player)
        {
            var dto = new PlayerProfileDto();
            _mapperMock
                .Setup(m => m.Map<PlayerProfileDto>(player))
                .Returns(dto);
            _mapperMock
                .Setup(m => m.Map<List<PlayerPositionDto>>(player.PlayerPositions))
                .Returns(new List<PlayerPositionDto>());
            _mapperMock
                .Setup(m => m.Map<PlayerAcademyDto>(It.IsAny<PlayerAcademy>()))
                .Returns(new PlayerAcademyDto());
            _mapperMock
                .Setup(m => m.Map<List<PlayerTeamDto>>(It.IsAny<IEnumerable<PlayerTeam>>()))
                .Returns(new List<PlayerTeamDto>());
        }

        private void SetupMapperForVsAcademy()
        {
            _mapperMock
                .Setup(m => m.Map<PlayerProfileDto>(It.IsAny<PlayerEntity>()))
                .Returns(new PlayerProfileDto());
        }
    }
}
