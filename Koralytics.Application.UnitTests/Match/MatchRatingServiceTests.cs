using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using Koralytics.Application.DTOs.Match;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Match;
using Koralytics.Application.Services.Match;
using Koralytics.Application.Services.Player.Helpers;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Coach;
using Koralytics.Domain.Entities.Drill;
using Koralytics.Domain.Entities.Match;
using DomainEnums = Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using MatchEntity = Koralytics.Domain.Entities.Match.Match;
using MatchLineupEntity = Koralytics.Domain.Entities.Match.MatchLineup;
using PlayerEntity = Koralytics.Domain.Entities.Player.Player;

namespace Koralytics.Application.UnitTests.Match
{
    public class MatchRatingServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<MatchRatingService>> _loggerMock;
        private readonly Mock<ICardInvalidationList> _invalidationListMock;
        private readonly MatchRatingService _service;

        public MatchRatingServiceTests()
        {
            _unitOfWorkMock = new();
            _mapperMock = new();
            _loggerMock = new();
            _invalidationListMock = new();
            _service = new MatchRatingService(
                _unitOfWorkMock.Object, _mapperMock.Object, _loggerMock.Object, _invalidationListMock.Object);
        }

        private void SetupRepository<T>(Mock<IRepository<T>> repo) where T : class, Koralytics.Domain.Interfaces.ISoftDelete
        {
            _unitOfWorkMock.Setup(u => u.Repository<T>()).Returns(repo.Object);
        }

        #region SubmitLineupAsync

        [Fact]
        public async Task SubmitLineupAsync_MatchNotFound_ThrowsNotFoundException()
        {
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .ReturnsAsync((MatchEntity?)null);
            SetupRepository(matchRepo);

            var dto = new SubmitLineupDto { Players = new List<SubmitLineupPlayerDto>() };

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.SubmitLineupAsync(1, 10, dto));
        }

        [Fact]
        public async Task SubmitLineupAsync_SessionMatch_ThrowsBadRequestException()
        {
            var match = new MatchEntity { Id = 1, Type = DomainEnums.MatchType.Session, Format = DomainEnums.MatchFormat.ElevenSide };
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .ReturnsAsync(match);
            SetupRepository(matchRepo);

            var dto = new SubmitLineupDto { Players = new List<SubmitLineupPlayerDto>() };

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.SubmitLineupAsync(1, 10, dto));
        }

        [Fact]
        public async Task SubmitLineupAsync_CoachNotAssigned_ThrowsForbiddenException()
        {
            var match = new MatchEntity { Id = 1, Type = DomainEnums.MatchType.Friendly, HomeTeamId = 1, AwayTeamId = 2, Format = DomainEnums.MatchFormat.FiveSide };
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .ReturnsAsync(match);
            SetupRepository(matchRepo);

            var coachTeams = new List<CoachTeam>();
            var coachTeamQueryable = coachTeams.BuildMock();
            var coachTeamRepo = new Mock<IRepository<CoachTeam>>();
            coachTeamRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(coachTeamQueryable);
            SetupRepository(coachTeamRepo);

            var dto = new SubmitLineupDto { Players = new List<SubmitLineupPlayerDto>() };

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                _service.SubmitLineupAsync(1, 10, dto));
        }

        [Fact]
        public async Task SubmitLineupAsync_WrongStartingCount_ThrowsBadRequestException()
        {
            var match = new MatchEntity { Id = 1, Type = DomainEnums.MatchType.Friendly, HomeTeamId = 1, AwayTeamId = 2, Format = DomainEnums.MatchFormat.FiveSide };
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .ReturnsAsync(match);
            SetupRepository(matchRepo);

            var coachTeam = new CoachTeam { CoachUserId = 10, TeamId = 1 };
            var coachTeams = new List<CoachTeam> { coachTeam };
            var coachTeamQueryable = coachTeams.BuildMock();
            var coachTeamRepo = new Mock<IRepository<CoachTeam>>();
            coachTeamRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(coachTeamQueryable);
            SetupRepository(coachTeamRepo);

            var dto = new SubmitLineupDto
            {
                Players = new List<SubmitLineupPlayerDto>
                {
                    new SubmitLineupPlayerDto { PlayerId = 1, TeamId = 1, IsStarting = true },
                    new SubmitLineupPlayerDto { PlayerId = 2, TeamId = 1, IsStarting = true }
                }
            };

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.SubmitLineupAsync(1, 10, dto));
        }

        [Fact]
        public async Task SubmitLineupAsync_PlayerFromWrongTeam_ThrowsBadRequestException()
        {
            var match = new MatchEntity { Id = 1, Type = DomainEnums.MatchType.Friendly, HomeTeamId = 1, AwayTeamId = 2, Format = DomainEnums.MatchFormat.FiveSide };
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .ReturnsAsync(match);
            SetupRepository(matchRepo);

            var coachTeam = new CoachTeam { CoachUserId = 10, TeamId = 1 };
            var coachTeams = new List<CoachTeam> { coachTeam };
            var coachTeamQueryable = coachTeams.BuildMock();
            var coachTeamRepo = new Mock<IRepository<CoachTeam>>();
            coachTeamRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(coachTeamQueryable);
            SetupRepository(coachTeamRepo);

            var dto = new SubmitLineupDto
            {
                Players = new List<SubmitLineupPlayerDto>
                {
                    new SubmitLineupPlayerDto { PlayerId = 1, TeamId = 1, IsStarting = true },
                    new SubmitLineupPlayerDto { PlayerId = 2, TeamId = 1, IsStarting = true },
                    new SubmitLineupPlayerDto { PlayerId = 3, TeamId = 1, IsStarting = true },
                    new SubmitLineupPlayerDto { PlayerId = 4, TeamId = 1, IsStarting = true },
                    new SubmitLineupPlayerDto { PlayerId = 5, TeamId = 2, IsStarting = true }
                }
            };

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.SubmitLineupAsync(1, 10, dto));
        }

        [Fact]
        public async Task SubmitLineupAsync_MissingPlayers_ThrowsNotFoundException()
        {
            var match = new MatchEntity { Id = 1, Type = DomainEnums.MatchType.Friendly, HomeTeamId = 1, AwayTeamId = 2, Format = DomainEnums.MatchFormat.FiveSide };
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .ReturnsAsync(match);
            SetupRepository(matchRepo);

            var coachTeam = new CoachTeam { CoachUserId = 10, TeamId = 1 };
            var coachTeams = new List<CoachTeam> { coachTeam };
            var coachTeamQueryable = coachTeams.BuildMock();
            var coachTeamRepo = new Mock<IRepository<CoachTeam>>();
            coachTeamRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(coachTeamQueryable);
            SetupRepository(coachTeamRepo);

            var dto = new SubmitLineupDto
            {
                Players = Enumerable.Range(1, 5)
                    .Select(i => new SubmitLineupPlayerDto { PlayerId = i, TeamId = 1, IsStarting = true }).ToList()
            };

            var existingLineups = new List<MatchLineupEntity>();
            var lineupQueryable = existingLineups.BuildMock();
            var lineupRepo = new Mock<IRepository<MatchLineupEntity>>();
            lineupRepo.Setup(r => r.GetQueryable()).Returns(lineupQueryable);
            SetupRepository(lineupRepo);

            var players = new List<PlayerEntity>();
            var playerQueryable = players.BuildMock();
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(playerQueryable);
            SetupRepository(playerRepo);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.SubmitLineupAsync(1, 10, dto));
        }

        [Fact]
        public async Task SubmitLineupAsync_ValidRequest_SoftDeletesOldAndAddsNew()
        {
            var match = new MatchEntity { Id = 1, Type = DomainEnums.MatchType.Friendly, HomeTeamId = 1, AwayTeamId = 2, Format = DomainEnums.MatchFormat.FiveSide };
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .ReturnsAsync(match);
            SetupRepository(matchRepo);

            var coachTeam = new CoachTeam { CoachUserId = 10, TeamId = 1 };
            var coachTeams = new List<CoachTeam> { coachTeam };
            var coachTeamQueryable = coachTeams.BuildMock();
            var coachTeamRepo = new Mock<IRepository<CoachTeam>>();
            coachTeamRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(coachTeamQueryable);
            SetupRepository(coachTeamRepo);

            var dto = new SubmitLineupDto
            {
                Players = Enumerable.Range(1, 5)
                    .Select(i => new SubmitLineupPlayerDto { PlayerId = i, TeamId = 1, IsStarting = true }).ToList()
            };

            var oldLineup = new MatchLineupEntity { Id = 100, MatchId = 1, TeamId = 1, PlayerId = 99 };
            var existingLineups = new List<MatchLineupEntity> { oldLineup };
            var lineupQueryable = existingLineups.BuildMock();
            var lineupRepo = new Mock<IRepository<MatchLineupEntity>>();
            lineupRepo.Setup(r => r.GetQueryable()).Returns(lineupQueryable);
            SetupRepository(lineupRepo);

            var players = Enumerable.Range(1, 5)
                .Select(i => new PlayerEntity { Id = i, FirstName = $"P{i}", LastName = $"L{i}" }).ToList();
            var playerQueryable = players.BuildMock();
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(playerQueryable);
            SetupRepository(playerRepo);

            _mapperMock.Setup(m => m.Map<MatchLineupEntity>(It.IsAny<SubmitLineupPlayerDto>()))
                .Returns((SubmitLineupPlayerDto p) => new MatchLineupEntity { PlayerId = p.PlayerId, TeamId = p.TeamId, IsStarting = p.IsStarting });

            await _service.SubmitLineupAsync(1, 10, dto);

            lineupRepo.Verify(r => r.SoftDeleteRange(It.Is<IEnumerable<MatchLineupEntity>>(e => e.Contains(oldLineup))), Times.Once);
            lineupRepo.Verify(r => r.AddAsync(It.IsAny<MatchLineupEntity>()), Times.Exactly(5));
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(1));
        }

        #endregion

        #region GetLineupAsync

        [Fact]
        public async Task GetLineupAsync_MatchNotFound_ThrowsNotFoundException()
        {
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .ReturnsAsync(false);
            SetupRepository(matchRepo);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.GetLineupAsync(1));
        }

        [Fact]
        public async Task GetLineupAsync_ReturnsLineup()
        {
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .ReturnsAsync(true);
            SetupRepository(matchRepo);

            var lineups = new List<MatchLineupEntity>
            {
                new MatchLineupEntity { Id = 1, MatchId = 1, PlayerId = 1, TeamId = 1, Player = new PlayerEntity { FirstName = "A", LastName = "B" }, Team = new Team { Name = "T" } }
            };
            var lineupQueryable = lineups.BuildMock();
            var lineupRepo = new Mock<IRepository<MatchLineupEntity>>();
            lineupRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(lineupQueryable);
            SetupRepository(lineupRepo);

            var dtoList = new List<LineupResponseDto> { new LineupResponseDto { Id = 1 } };
            _mapperMock.Setup(m => m.Map<List<LineupResponseDto>>(It.IsAny<List<MatchLineupEntity>>())).Returns(dtoList);

            var result = await _service.GetLineupAsync(1);

            Assert.NotNull(result);
            Assert.Single(result);
        }

        #endregion

        #region SubmitMatchRatingsAsync

        [Fact]
        public async Task SubmitMatchRatingsAsync_DuplicatePlayers_ThrowsBadRequestException()
        {
            var dto = new SubmitMatchRatingsDto
            {
                Ratings = new List<SubmitMatchRatingPlayerDto>
                {
                    new SubmitMatchRatingPlayerDto { PlayerId = 1, IsMOTM = true, CategoryRatings = new List<CategoryRatingDto>() },
                    new SubmitMatchRatingPlayerDto { PlayerId = 1, IsMOTM = false, CategoryRatings = new List<CategoryRatingDto>() }
                }
            };

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.SubmitMatchRatingsAsync(1, 10, dto));
        }

        [Fact]
        public async Task SubmitMatchRatingsAsync_DuplicateCategories_ThrowsBadRequestException()
        {
            var dto = new SubmitMatchRatingsDto
            {
                Ratings = new List<SubmitMatchRatingPlayerDto>
                {
                    new SubmitMatchRatingPlayerDto
                    {
                        PlayerId = 1, IsMOTM = true,
                        CategoryRatings = new List<CategoryRatingDto>
                        {
                            new CategoryRatingDto { DrillCategoryId = 1, Rating = 5 },
                            new CategoryRatingDto { DrillCategoryId = 1, Rating = 4 }
                        }
                    }
                }
            };

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.SubmitMatchRatingsAsync(1, 10, dto));
        }

        [Fact]
        public async Task SubmitMatchRatingsAsync_MatchNotFound_ThrowsNotFoundException()
        {
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .ReturnsAsync((MatchEntity?)null);
            SetupRepository(matchRepo);

            var dto = new SubmitMatchRatingsDto
            {
                Ratings = new List<SubmitMatchRatingPlayerDto>
                {
                    new SubmitMatchRatingPlayerDto { PlayerId = 1, IsMOTM = true, CategoryRatings = new List<CategoryRatingDto>() }
                }
            };

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.SubmitMatchRatingsAsync(1, 10, dto));
        }

        [Fact]
        public async Task SubmitMatchRatingsAsync_MatchNotCompleted_ThrowsBadRequestException()
        {
            var match = new MatchEntity { Id = 1, Status = DomainEnums.MatchStatus.Scheduled, Type = DomainEnums.MatchType.Friendly, HomeTeamId = 1, AwayTeamId = 2 };
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .ReturnsAsync(match);
            SetupRepository(matchRepo);

            var dto = new SubmitMatchRatingsDto
            {
                Ratings = new List<SubmitMatchRatingPlayerDto>
                {
                    new SubmitMatchRatingPlayerDto { PlayerId = 1, IsMOTM = true, CategoryRatings = new List<CategoryRatingDto>() }
                }
            };

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.SubmitMatchRatingsAsync(1, 10, dto));
        }

        [Fact]
        public async Task SubmitMatchRatingsAsync_InvalidMotmCount_ThrowsBadRequestException()
        {
            var match = new MatchEntity { Id = 1, Status = DomainEnums.MatchStatus.Completed, Type = DomainEnums.MatchType.Friendly, HomeTeamId = 1, AwayTeamId = 2 };
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .ReturnsAsync(match);
            SetupRepository(matchRepo);

            var lineups = new List<MatchLineupEntity>
            {
                new MatchLineupEntity { MatchId = 1, PlayerId = 1, TeamId = 1 },
                new MatchLineupEntity { MatchId = 1, PlayerId = 2, TeamId = 1 }
            };
            var lineupQueryable = lineups.BuildMock();
            var lineupRepo = new Mock<IRepository<MatchLineupEntity>>();
            lineupRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(lineupQueryable);
            SetupRepository(lineupRepo);

            var coachTeam = new CoachTeam { CoachUserId = 10, TeamId = 1 };
            var coachTeams = new List<CoachTeam> { coachTeam };
            var coachTeamQueryable = coachTeams.BuildMock();
            var coachTeamRepo = new Mock<IRepository<CoachTeam>>();
            coachTeamRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(coachTeamQueryable);
            SetupRepository(coachTeamRepo);

            var dto = new SubmitMatchRatingsDto
            {
                Ratings = new List<SubmitMatchRatingPlayerDto>
                {
                    new SubmitMatchRatingPlayerDto { PlayerId = 1, IsMOTM = true, CategoryRatings = new List<CategoryRatingDto>() },
                    new SubmitMatchRatingPlayerDto { PlayerId = 2, IsMOTM = true, CategoryRatings = new List<CategoryRatingDto>() }
                }
            };

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.SubmitMatchRatingsAsync(1, 10, dto));
        }

        [Fact]
        public async Task SubmitMatchRatingsAsync_NotInLineup_ThrowsBadRequestException()
        {
            var match = new MatchEntity { Id = 1, Status = DomainEnums.MatchStatus.Completed, Type = DomainEnums.MatchType.Friendly, HomeTeamId = 1, AwayTeamId = 2 };
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .ReturnsAsync(match);
            SetupRepository(matchRepo);

            var lineups = new List<MatchLineupEntity>
            {
                new MatchLineupEntity { MatchId = 1, PlayerId = 1, TeamId = 1 }
            };
            var lineupQueryable = lineups.BuildMock();
            var lineupRepo = new Mock<IRepository<MatchLineupEntity>>();
            lineupRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(lineupQueryable);
            SetupRepository(lineupRepo);

            var coachTeam = new CoachTeam { CoachUserId = 10, TeamId = 1 };
            var coachTeams = new List<CoachTeam> { coachTeam };
            var coachTeamQueryable = coachTeams.BuildMock();
            var coachTeamRepo = new Mock<IRepository<CoachTeam>>();
            coachTeamRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(coachTeamQueryable);
            SetupRepository(coachTeamRepo);

            var dto = new SubmitMatchRatingsDto
            {
                Ratings = new List<SubmitMatchRatingPlayerDto>
                {
                    new SubmitMatchRatingPlayerDto { PlayerId = 999, IsMOTM = true, CategoryRatings = new List<CategoryRatingDto>() }
                }
            };

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.SubmitMatchRatingsAsync(1, 10, dto));
        }

        [Fact]
        public async Task SubmitMatchRatingsAsync_ValidRequest_SubmitsRatingsAndInvalidatesCards()
        {
            var match = new MatchEntity { Id = 1, Status = DomainEnums.MatchStatus.Completed, Type = DomainEnums.MatchType.Friendly, HomeTeamId = 1, AwayTeamId = 2 };
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .ReturnsAsync(match);
            SetupRepository(matchRepo);

            var lineups = new List<MatchLineupEntity>
            {
                new MatchLineupEntity { MatchId = 1, PlayerId = 1, TeamId = 1 },
                new MatchLineupEntity { MatchId = 1, PlayerId = 2, TeamId = 1 }
            };
            var lineupQueryable = lineups.BuildMock();
            var lineupRepo = new Mock<IRepository<MatchLineupEntity>>();
            lineupRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(lineupQueryable);
            SetupRepository(lineupRepo);

            var coachTeam = new CoachTeam { CoachUserId = 10, TeamId = 1 };
            var coachTeams = new List<CoachTeam> { coachTeam };
            var coachTeamQueryable = coachTeams.BuildMock();
            var coachTeamRepo = new Mock<IRepository<CoachTeam>>();
            coachTeamRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(coachTeamQueryable);
            SetupRepository(coachTeamRepo);

            var matchEvents = new List<MatchEvent>();
            var eventQueryable = matchEvents.BuildMock();
            var eventRepo = new Mock<IRepository<MatchEvent>>();
            eventRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(eventQueryable);
            SetupRepository(eventRepo);

            var categories = new List<DrillCategory>
            {
                new DrillCategory { Id = 1, Name = "Passing" },
                new DrillCategory { Id = 2, Name = "Shooting" }
            };
            var categoryQueryable = categories.BuildMock();
            var categoryRepo = new Mock<IRepository<DrillCategory>>();
            categoryRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(categoryQueryable);
            SetupRepository(categoryRepo);

            var transactionMock = new Mock<IDbContextTransaction>();
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(transactionMock.Object);

            var ratingRepo = new Mock<IRepository<MatchPlayerRating>>();
            SetupRepository(ratingRepo);

            var dto = new SubmitMatchRatingsDto
            {
                Ratings = new List<SubmitMatchRatingPlayerDto>
                {
                    new SubmitMatchRatingPlayerDto
                    {
                        PlayerId = 1, IsMOTM = true, MinutesPlayed = 90,
                        CategoryRatings = new List<CategoryRatingDto>
                        {
                            new CategoryRatingDto { DrillCategoryId = 1, Rating = 8.5m }
                        }
                    },
                    new SubmitMatchRatingPlayerDto
                    {
                        PlayerId = 2, IsMOTM = false, MinutesPlayed = 85,
                        CategoryRatings = new List<CategoryRatingDto>
                        {
                            new CategoryRatingDto { DrillCategoryId = 2, Rating = 7.0m }
                        }
                    }
                }
            };

            await _service.SubmitMatchRatingsAsync(1, 10, dto);

            ratingRepo.Verify(r => r.AddAsync(It.IsAny<MatchPlayerRating>()), Times.Exactly(2));
            _invalidationListMock.Verify(i => i.Invalidate(1), Times.Once);
            _invalidationListMock.Verify(i => i.Invalidate(2), Times.Once);
            transactionMock.Verify(t => t.CommitAsync(default), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region GetMatchRatingsAsync

        [Fact]
        public async Task GetMatchRatingsAsync_MatchNotFound_ThrowsNotFoundException()
        {
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .ReturnsAsync(false);
            SetupRepository(matchRepo);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.GetMatchRatingsAsync(1));
        }

        [Fact]
        public async Task GetMatchRatingsAsync_ReturnsRatingsWithPlayerAndCoachNames()
        {
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .ReturnsAsync(true);
            SetupRepository(matchRepo);

            var ratings = new List<MatchPlayerRating>
            {
                new MatchPlayerRating
                {
                    Id = 1, MatchId = 1, PlayerId = 1, CoachId = 10,
                    Player = new PlayerEntity { FirstName = "John", LastName = "Doe" },
                    Coach = new Coach { FirstName = "Coach", LastName = "Smith" },
                    CategoryRatings = new List<MatchPlayerCategoryRating>
                    {
                        new MatchPlayerCategoryRating { DrillCategoryId = 1, Rating = 8 }
                    }
                }
            };
            var ratingQueryable = ratings.BuildMock();
            var ratingRepo = new Mock<IRepository<MatchPlayerRating>>();
            ratingRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(ratingQueryable);
            SetupRepository(ratingRepo);

            _mapperMock.Setup(m => m.Map<MatchPlayerRatingDto>(It.IsAny<MatchPlayerRating>()))
                .Returns(new MatchPlayerRatingDto());

            var result = await _service.GetMatchRatingsAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.MatchId);
            Assert.Single(result.Ratings);
            Assert.Equal("John Doe", result.Ratings[0].PlayerName);
            Assert.Equal("Coach Smith", result.Ratings[0].CoachName);
            Assert.Single(result.Ratings[0].CategoryRatings);
        }

        #endregion
    }
}
