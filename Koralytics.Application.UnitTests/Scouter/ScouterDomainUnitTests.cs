using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Moq;
using Xunit;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Koralytics.Application.DTOs.Player;
using Koralytics.Application.DTOs.Scouter;
using Koralytics.Application.DTOs.ScouterDtos;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Services.Player.Helpers;
using Koralytics.Application.Services.Player.PlayerCardService;

using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Identity;
using Koralytics.Domain.Entities.Match;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Entities.Scouter;
using Koralytics.Domain.Exceptions;
using Koralytics.Application.Services.Scouter.ScouterFollowService;
using Koralytics.Application.Services.Scouter.ScouterShortlistService;
using Koralytics.Application.Services.Scouter.ScouterSearchService;
using Koralytics.Application.Services.Scouter.ScouterReportService;

namespace Koralytics.Tests.ScouterTests
{
    public class ScouterDomainUnitTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IPlayerCardService> _mockPlayerCardService;
        private readonly CardInvalidationList _invalidationList;

        // Loggers
        private readonly Mock<ILogger<ScouterFollowService>> _mockFollowLogger;
        private readonly Mock<ILogger<ScouterShortlistService>> _mockShortlistLogger;
        private readonly Mock<ILogger<ScouterSearchService>> _mockSearchLogger;
        private readonly Mock<ILogger<ScouterReportService>> _mockReportLogger;

        // Repositories
        private readonly Mock<IRepository<Scouter>> _mockScouterRepo;
        private readonly Mock<IRepository<Player>> _mockPlayerRepo;
        private readonly Mock<IRepository<ScouterFollow>> _mockFollowRepo;
        private readonly Mock<IRepository<ScouterView>> _mockViewRepo;
        private readonly Mock<IRepository<ScouterShortlist>> _mockShortlistRepo;
        private readonly Mock<IRepository<ScouterReport>> _mockReportRepo;
        private readonly Mock<IRepository<MatchLineup>> _mockMatchLineupRepo;
        private readonly Mock<IRepository<PlayerCard>> _mockPlayerCardRepo;

        public ScouterDomainUnitTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockPlayerCardService = new Mock<IPlayerCardService>();

            _mockFollowLogger = new Mock<ILogger<ScouterFollowService>>();
            _mockShortlistLogger = new Mock<ILogger<ScouterShortlistService>>();
            _mockSearchLogger = new Mock<ILogger<ScouterSearchService>>();
            _mockReportLogger = new Mock<ILogger<ScouterReportService>>();

            var mockScopeFactory = new Mock<IServiceScopeFactory>();
            var mockLogger = new Mock<ILogger<CardInvalidationList>>();
            _invalidationList = new CardInvalidationList(mockScopeFactory.Object, mockLogger.Object);

            _mockScouterRepo = new Mock<IRepository<Scouter>>();
            _mockPlayerRepo = new Mock<IRepository<Player>>();
            _mockFollowRepo = new Mock<IRepository<ScouterFollow>>();
            _mockViewRepo = new Mock<IRepository<ScouterView>>();
            _mockShortlistRepo = new Mock<IRepository<ScouterShortlist>>();
            _mockReportRepo = new Mock<IRepository<ScouterReport>>();
            _mockMatchLineupRepo = new Mock<IRepository<MatchLineup>>();
            _mockPlayerCardRepo = new Mock<IRepository<PlayerCard>>();

            _mockUnitOfWork.Setup(uow => uow.Repository<Scouter>()).Returns(_mockScouterRepo.Object);
            _mockUnitOfWork.Setup(uow => uow.Repository<Player>()).Returns(_mockPlayerRepo.Object);
            _mockUnitOfWork.Setup(uow => uow.Repository<ScouterFollow>()).Returns(_mockFollowRepo.Object);
            _mockUnitOfWork.Setup(uow => uow.Repository<ScouterView>()).Returns(_mockViewRepo.Object);
            _mockUnitOfWork.Setup(uow => uow.Repository<ScouterShortlist>()).Returns(_mockShortlistRepo.Object);
            _mockUnitOfWork.Setup(uow => uow.Repository<ScouterReport>()).Returns(_mockReportRepo.Object);
            _mockUnitOfWork.Setup(uow => uow.Repository<MatchLineup>()).Returns(_mockMatchLineupRepo.Object);
            _mockUnitOfWork.Setup(uow => uow.Repository<PlayerCard>()).Returns(_mockPlayerCardRepo.Object);

            // Default empty backing sets so older tests that don't touch these repos don't NRE.
            _mockScouterRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(new List<Scouter>().BuildMockDbSet().Object);
            _mockPlayerRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(new List<Player>().BuildMockDbSet().Object);
            _mockFollowRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(new List<ScouterFollow>().BuildMockDbSet().Object);
            _mockShortlistRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(new List<ScouterShortlist>().BuildMockDbSet().Object);
            _mockMatchLineupRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(new List<MatchLineup>().BuildMockDbSet().Object);
            _mockPlayerCardRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(new List<PlayerCard>().BuildMockDbSet().Object);

            SetupRealMapperConfiguration();
        }

        // IMPORTANT: this now loads the REAL Koralytics.Application.Mappings.ScouterProfile.ScouterProfile
        // instead of a hand-rolled, null-guarded stand-in. The previous local mapping was safer than
        // production and let tests pass against behavior the app doesn't actually have. ProjectTo in
        // every service call uses whatever _mapper.ConfigurationProvider returns, so the test config
        // must be the real one or these tests prove nothing about production correctness.
        private void SetupRealMapperConfiguration()
        {
            var configExpression = new MapperConfigurationExpression();
            configExpression.AddProfile<Koralytics.Application.Mappings.ScouterProfile.ScouterProfile>();

            var config = new MapperConfiguration(configExpression, NullLoggerFactory.Instance);

            _mockMapper.Setup(m => m.ConfigurationProvider).Returns(config);
        }

        // Kept as an alias so existing test bodies that call SetupPlayerCardProjection() still compile;
        // it now just (re)applies the real mapping configuration.
        private void SetupPlayerCardProjection() => SetupRealMapperConfiguration();

        // The real PlayerCard -> PlayerCardDto map does not null-guard the root PlayerCard row, s.Player,
        // or s.Player.PlayerPositions.FirstOrDefault(p => p.IsPrimary). Every Player fixture that will be
        // projected through that map must carry at least one primary PlayerPosition, or ProjectTo throws
        // a NullReferenceException the instant AutoMapper evaluates ".Position" on a null FirstOrDefault().
        private static Player CreateProjectablePlayer(int id, string firstName = "Test", string lastName = "Player", DateTime? dateOfBirth = null)
        {
            return new Player
            {
                Id = id,
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = dateOfBirth ?? DateTime.UtcNow.AddYears(-20),
                PlayerPositions = new List<PlayerPosition>
                {
                    new PlayerPosition { IsPrimary = true }
                }
            };
        }

        // Every player that survives a filter/pagination step in the service methods under test must have
        // a matching PlayerCard row, or the service's FirstOrDefault() returns null and ProjectTo crashes
        // trying to read a member off that null row. This helper keeps that pairing explicit and correct.
        private static PlayerCard CreateProjectableCard(Player player, decimal overallRating = 70)
        {
            return new PlayerCard
            {
                PlayerId = player.Id,
                Player = player,
                OverallRating = overallRating
            };
        }

        private ScouterSearchService CreateSearchService() =>
            new ScouterSearchService(_mockUnitOfWork.Object, _mockMapper.Object, _mockSearchLogger.Object);

        private ScouterShortlistService CreateShortlistService() =>
            new ScouterShortlistService(_mockUnitOfWork.Object, _mockMapper.Object, _mockShortlistLogger.Object);

        private ScouterFollowService CreateFollowService() =>
            new ScouterFollowService(_mockUnitOfWork.Object, _mockMapper.Object, _mockFollowLogger.Object);

        private ScouterReportService CreateReportService() =>
            new ScouterReportService(_mockUnitOfWork.Object, _mockReportLogger.Object);

        #region SECTION 1: ScouterFollowService Tests

        [Fact]
        public async Task FollowPlayer_ShouldThrowNotFoundException_WhenScouterDoesNotExist()
        {
            var service = CreateFollowService();

            var emptyScouterDbSet = new List<Scouter>().BuildMockDbSet().Object;
            var playerDbSet = new List<Player> { new Player { Id = 99 } }.BuildMockDbSet().Object;

            _mockScouterRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(emptyScouterDbSet);
            _mockPlayerRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(playerDbSet);

            await Assert.ThrowsAsync<NotFoundException>(() => service.FollowPlayerAsync(1, 99));
        }

        [Fact]
        public async Task FollowPlayer_ShouldThrowNotFoundException_WhenPlayerDoesNotExist()
        {
            var service = CreateFollowService();

            var scouterDbSet = new List<Scouter> { new Scouter { Id = 1 } }.BuildMockDbSet().Object;
            var emptyPlayerDbSet = new List<Player>().BuildMockDbSet().Object;

            _mockScouterRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(scouterDbSet);
            _mockPlayerRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(emptyPlayerDbSet);

            await Assert.ThrowsAsync<NotFoundException>(() => service.FollowPlayerAsync(1, 99));
        }

        [Fact]
        public async Task FollowPlayer_ShouldSaveFollowRelation_WhenValidAndNotAlreadyFollowing()
        {
            var service = CreateFollowService();

            var scouterDbSet = new List<Scouter> { new Scouter { Id = 1 } }.BuildMockDbSet().Object;
            var playerDbSet = new List<Player> { new Player { Id = 10 } }.BuildMockDbSet().Object;

            _mockScouterRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(scouterDbSet);
            _mockPlayerRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(playerDbSet);
            _mockFollowRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ScouterFollow, bool>>>())).ReturnsAsync(false);

            await service.FollowPlayerAsync(1, 10);

            _mockFollowRepo.Verify(r => r.AddAsync(It.Is<ScouterFollow>(f => f.ScouterUserId == 1 && f.PlayerId == 10)), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task FollowPlayer_ShouldShortCircuitSilently_WhenAlreadyFollowing()
        {
            var service = CreateFollowService();

            var scouterDbSet = new List<Scouter> { new Scouter { Id = 1 } }.BuildMockDbSet().Object;
            var playerDbSet = new List<Player> { new Player { Id = 10 } }.BuildMockDbSet().Object;

            _mockScouterRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(scouterDbSet);
            _mockPlayerRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(playerDbSet);
            _mockFollowRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ScouterFollow, bool>>>())).ReturnsAsync(true);

            await service.FollowPlayerAsync(1, 10);

            _mockFollowRepo.Verify(r => r.AddAsync(It.IsAny<ScouterFollow>()), Times.Never);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UnfollowPlayer_ShouldSoftDeleteRelation_WhenFollowExists()
        {
            var service = CreateFollowService();

            var existingFollow = new ScouterFollow { ScouterUserId = 1, PlayerId = 10 };
            _mockFollowRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ScouterFollow, bool>>>())).ReturnsAsync(existingFollow);

            await service.UnfollowPlayerAsync(1, 10);

            _mockFollowRepo.Verify(r => r.SoftDelete(existingFollow), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UnfollowPlayer_ShouldThrowNotFoundException_WhenScouterDoesNotExistAndFollowMissing()
        {
            var service = CreateFollowService();

            _mockFollowRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ScouterFollow, bool>>>())).ReturnsAsync((ScouterFollow)null);

            var emptyScouterDbSet = new List<Scouter>().BuildMockDbSet().Object;
            var playerDbSet = new List<Player> { new Player { Id = 10 } }.BuildMockDbSet().Object;

            _mockScouterRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(emptyScouterDbSet);
            _mockPlayerRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(playerDbSet);

            await Assert.ThrowsAsync<NotFoundException>(() => service.UnfollowPlayerAsync(1, 10));
        }

        [Fact]
        public async Task UnfollowPlayer_ShouldThrowNotFoundException_WhenPlayerDoesNotExistAndFollowMissing()
        {
            var service = CreateFollowService();

            _mockFollowRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ScouterFollow, bool>>>())).ReturnsAsync((ScouterFollow)null);

            var scouterDbSet = new List<Scouter> { new Scouter { Id = 1 } }.BuildMockDbSet().Object;
            var emptyPlayerDbSet = new List<Player>().BuildMockDbSet().Object;

            _mockScouterRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(scouterDbSet);
            _mockPlayerRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(emptyPlayerDbSet);

            await Assert.ThrowsAsync<NotFoundException>(() => service.UnfollowPlayerAsync(1, 10));
        }

        [Fact]
        public async Task UnfollowPlayer_ShouldThrowNotFoundException_WhenFollowRecordMissingButEntitiesExist()
        {
            var service = CreateFollowService();

            _mockFollowRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ScouterFollow, bool>>>())).ReturnsAsync((ScouterFollow)null);

            var scouterDbSet = new List<Scouter> { new Scouter { Id = 1 } }.BuildMockDbSet().Object;
            var playerDbSet = new List<Player> { new Player { Id = 10 } }.BuildMockDbSet().Object;

            _mockScouterRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(scouterDbSet);
            _mockPlayerRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(playerDbSet);

            await Assert.ThrowsAsync<NotFoundException>(() => service.UnfollowPlayerAsync(1, 10));
        }

        [Fact]
        public async Task LogProfileView_ShouldThrowNotFoundException_WhenScouterDoesNotExist()
        {
            var service = CreateFollowService();

            _mockScouterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(false);

            await Assert.ThrowsAsync<NotFoundException>(() => service.LogProfileViewAsync(1, 10));

            _mockViewRepo.Verify(r => r.AddAsync(It.IsAny<ScouterView>()), Times.Never);
        }

        [Fact]
        public async Task LogProfileView_ShouldThrowNotFoundException_WhenPlayerDoesNotExist()
        {
            var service = CreateFollowService();

            _mockScouterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(true);
            _mockPlayerRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Player, bool>>>())).ReturnsAsync(false);

            await Assert.ThrowsAsync<NotFoundException>(() => service.LogProfileViewAsync(1, 10));

            _mockViewRepo.Verify(r => r.AddAsync(It.IsAny<ScouterView>()), Times.Never);
        }

        [Fact]
        public async Task LogProfileView_ShouldCreateScouterViewRecord_WhenActorsAreValid()
        {
            var service = CreateFollowService();

            _mockScouterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(true);
            _mockPlayerRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Player, bool>>>())).ReturnsAsync(true);

            await service.LogProfileViewAsync(1, 10);

            _mockViewRepo.Verify(r => r.AddAsync(It.Is<ScouterView>(v => v.ScouterId == 1 && v.PlayerId == 10)), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetFollowedPlayers_ShouldThrowNotFoundException_WhenScouterDoesNotExist()
        {
            var service = CreateFollowService();

            var emptyFollowDbSet = new List<ScouterFollow>().BuildMockDbSet().Object;
            var emptyScouterDbSet = new List<Scouter>().BuildMockDbSet().Object;

            _mockFollowRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(emptyFollowDbSet);
            _mockScouterRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(emptyScouterDbSet);

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetFollowedPlayersAsync(1));
        }

        [Fact]
        public async Task GetFollowedPlayers_ShouldReturnEmptyResult_WhenScouterExistsButHasNoFollowedPlayers()
        {
            var service = CreateFollowService();

            var emptyFollowDbSet = new List<ScouterFollow>().BuildMockDbSet().Object;
            var scouterDbSet = new List<Scouter> { new Scouter { Id = 1 } }.BuildMockDbSet().Object;

            _mockFollowRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(emptyFollowDbSet);
            _mockScouterRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(scouterDbSet);

            var result = await service.GetFollowedPlayersAsync(1);

            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task GetFollowedPlayers_ShouldFilterBySearchTerm()
        {
            var service = CreateFollowService();
            SetupPlayerCardProjection();

            var scouterDbSet = new List<Scouter> { new Scouter { Id = 1 } }.BuildMockDbSet().Object;
            _mockScouterRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(scouterDbSet);

            // Every player reachable by the query below flows through the real, unguarded
            // PlayerCard -> PlayerCardDto map, so both need PlayerPositions populated, and both
            // need a matching PlayerCard row with Player set (not just PlayerId).
            var matchingPlayer = CreateProjectablePlayer(10, "Leo", "Silva");
            var nonMatchingPlayer = CreateProjectablePlayer(11, "Karim", "Ahmed");

            var follows = new List<ScouterFollow>
            {
                new ScouterFollow { Id = 1, ScouterUserId = 1, PlayerId = 10, Player = matchingPlayer, FollowedAt = DateTime.UtcNow, IsDeleted = false },
                new ScouterFollow { Id = 2, ScouterUserId = 1, PlayerId = 11, Player = nonMatchingPlayer, FollowedAt = DateTime.UtcNow, IsDeleted = false }
            };
            _mockFollowRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(follows.BuildMockDbSet().Object);

            var cards = new List<PlayerCard>
            {
                CreateProjectableCard(matchingPlayer),
                CreateProjectableCard(nonMatchingPlayer)
            };
            _mockPlayerCardRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(cards.BuildMockDbSet().Object);

            var result = await service.GetFollowedPlayersAsync(1, searchTerm: "Leo");

            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public async Task GetFollowedPlayers_ShouldReturnPaginatedResults()
        {
            var service = CreateFollowService();
            SetupPlayerCardProjection();

            var players = Enumerable.Range(1, 15)
                .Select(i => CreateProjectablePlayer(i, $"Player{i}", "Test"))
                .ToList();

            var follows = players.Select(p => new ScouterFollow
            {
                Id = p.Id,
                ScouterUserId = 1,
                PlayerId = p.Id,
                Player = p,
                FollowedAt = DateTime.UtcNow.AddMinutes(-p.Id),
                IsDeleted = false
            }).ToList();

            var cards = players.Select(p => CreateProjectableCard(p, 80)).ToList();

            _mockFollowRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(follows.BuildMockDbSet().Object);
            _mockPlayerCardRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(cards.BuildMockDbSet().Object);

            var result = await service.GetFollowedPlayersAsync(1, pageNumber: 2, pageSize: 10);

            Assert.Equal(15, result.TotalCount);
            Assert.Equal(5, result.Items.Count);
            Assert.Equal(2, result.PageNumber);
        }

        [Fact]
        public async Task GetProfileViewsAnalytics_ShouldReturnEmptyAnalytics_WhenPlayerHasNoViews()
        {
            var service = CreateFollowService();

            var playerList = new List<Player>
            {
                new Player { Id = 10, ScouterViews = new List<ScouterView>() }
            };

            var mockPlayerDbSet = playerList.BuildMockDbSet().Object;
            _mockPlayerRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(mockPlayerDbSet);

            SetupRealMapperConfiguration();

            var result = await service.GetProfileViewsAnalyticsAsync(10);

            Assert.NotNull(result);
            _mockPlayerRepo.Verify(r => r.GetQueryableAsNoTracking(), Times.Once);
        }

        [Fact]
        public async Task GetProfileViewsAnalytics_ShouldReturnSafeDefaults_WhenPlayerDoesNotExist()
        {
            var service = CreateFollowService();

            var emptyPlayerList = new List<Player>();
            var mockPlayerDbSet = emptyPlayerList.BuildMockDbSet().Object;
            _mockPlayerRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(mockPlayerDbSet);

            SetupRealMapperConfiguration();

            var result = await service.GetProfileViewsAnalyticsAsync(999);

            Assert.NotNull(result);
            Assert.Equal(0, result.TotalViewsCount);
            Assert.Empty(result.RecentViews);
        }

        #endregion

        #region SECTION 2: ScouterShortlistService Tests

        [Fact]
        public async Task AddToShortlist_ShouldThrowNotFoundException_WhenScouterDoesNotExist()
        {
            var service = CreateShortlistService();

            _mockScouterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(false);

            await Assert.ThrowsAsync<NotFoundException>(() => service.AddToShortlistAsync(1, 10));
        }

        [Fact]
        public async Task AddToShortlist_ShouldThrowNotFoundException_WhenPlayerDoesNotExist()
        {
            var service = CreateShortlistService();

            _mockScouterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(true);
            _mockPlayerRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Player, bool>>>())).ReturnsAsync(false);

            await Assert.ThrowsAsync<NotFoundException>(() => service.AddToShortlistAsync(1, 10));
        }

        [Fact]
        public async Task AddToShortlist_ShouldReturnMappedDto_WhenAlreadyShortlisted()
        {
            var service = CreateShortlistService();

            _mockScouterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(true);
            _mockPlayerRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Player, bool>>>())).ReturnsAsync(true);

            var existingShortlist = new ScouterShortlist { ScouterUserId = 1, PlayerId = 10 };
            _mockShortlistRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<ScouterShortlist, bool>>>())).ReturnsAsync(existingShortlist);
            _mockMapper.Setup(m => m.Map<ScouterShortlistDto>(existingShortlist)).Returns(new ScouterShortlistDto());

            var result = await service.AddToShortlistAsync(1, 10);

            Assert.NotNull(result);
            _mockShortlistRepo.Verify(r => r.AddAsync(It.IsAny<ScouterShortlist>()), Times.Never);
        }

        [Fact]
        public async Task AddToShortlist_ShouldCreateNewEntry_WhenValidAndNotAlreadyShortlisted()
        {
            var service = CreateShortlistService();

            _mockScouterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(true);
            _mockPlayerRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Player, bool>>>())).ReturnsAsync(true);
            _mockShortlistRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<ScouterShortlist, bool>>>())).ReturnsAsync((ScouterShortlist)null);
            _mockMapper.Setup(m => m.Map<ScouterShortlistDto>(It.IsAny<ScouterShortlist>())).Returns(new ScouterShortlistDto());

            var result = await service.AddToShortlistAsync(1, 10);

            Assert.NotNull(result);
            _mockShortlistRepo.Verify(r => r.AddAsync(It.Is<ScouterShortlist>(s => s.ScouterUserId == 1 && s.PlayerId == 10)), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task RemoveFromShortlist_ShouldThrowNotFoundException_WhenScouterDoesNotExistAndEntryMissing()
        {
            var service = CreateShortlistService();

            _mockShortlistRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ScouterShortlist, bool>>>())).ReturnsAsync((ScouterShortlist)null);
            _mockScouterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(false);

            await Assert.ThrowsAsync<NotFoundException>(() => service.RemoveFromShortlistAsync(1, 10));
        }

        [Fact]
        public async Task RemoveFromShortlist_ShouldThrowNotFoundException_WhenPlayerDoesNotExistAndEntryMissing()
        {
            var service = CreateShortlistService();

            _mockShortlistRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ScouterShortlist, bool>>>())).ReturnsAsync((ScouterShortlist)null);
            _mockScouterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(true);
            _mockPlayerRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Player, bool>>>())).ReturnsAsync(false);

            await Assert.ThrowsAsync<NotFoundException>(() => service.RemoveFromShortlistAsync(1, 10));
        }

        [Fact]
        public async Task RemoveFromShortlist_ShouldThrowNotFoundException_WhenEntryDoesNotExist()
        {
            var service = CreateShortlistService();

            _mockShortlistRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ScouterShortlist, bool>>>())).ReturnsAsync((ScouterShortlist)null);
            _mockScouterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(true);
            _mockPlayerRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Player, bool>>>())).ReturnsAsync(true);

            await Assert.ThrowsAsync<NotFoundException>(() => service.RemoveFromShortlistAsync(1, 10));
        }

        [Fact]
        public async Task RemoveFromShortlist_ShouldSoftDeleteRelation_WhenEntryExists()
        {
            var service = CreateShortlistService();

            var entry = new ScouterShortlist { ScouterUserId = 1, PlayerId = 10 };
            _mockShortlistRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ScouterShortlist, bool>>>())).ReturnsAsync(entry);

            var result = await service.RemoveFromShortlistAsync(1, 10);

            Assert.True(result);
            _mockShortlistRepo.Verify(r => r.SoftDelete(entry), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetShortlist_ShouldThrowNotFoundException_WhenScouterDoesNotExist()
        {
            var service = CreateShortlistService();

            var emptyScouterDbSet = new List<Scouter>().BuildMockDbSet().Object;

            _mockScouterRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(emptyScouterDbSet);

            var emptyShortlistDbSet = new List<ScouterShortlist>().BuildMockDbSet().Object;
            _mockShortlistRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(emptyShortlistDbSet);
            await Assert.ThrowsAsync<NotFoundException>(() => service.GetShortlistAsync(1));
        }

        [Fact]
        public async Task GetShortlist_ShouldReturnEmptyResult_WhenNoShortlistedPlayers()
        {
            var service = CreateShortlistService();

            var scouterDbSet = new List<Scouter> { new Scouter { Id = 1 } }.BuildMockDbSet().Object;
            var emptyShortlistDbSet = new List<ScouterShortlist>().BuildMockDbSet().Object;

            _mockScouterRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(scouterDbSet);
            _mockShortlistRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(emptyShortlistDbSet);

            var result = await service.GetShortlistAsync(1);

            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task GetShortlist_ShouldFilterBySearchTerm()
        {
            var service = CreateShortlistService();
            SetupPlayerCardProjection();

            var scouterDbSet = new List<Scouter> { new Scouter { Id = 1 } }.BuildMockDbSet().Object;
            _mockScouterRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(scouterDbSet);

            var matchingPlayer = CreateProjectablePlayer(10, "Cristiano", "Ronaldo");
            var nonMatchingPlayer = CreateProjectablePlayer(11, "Lionel", "Messi");

            var shortlist = new List<ScouterShortlist>
            {
                new ScouterShortlist { Id = 1, ScouterUserId = 1, PlayerId = 10, Player = matchingPlayer, AddedAt = DateTime.UtcNow, IsDeleted = false },
                new ScouterShortlist { Id = 2, ScouterUserId = 1, PlayerId = 11, Player = nonMatchingPlayer, AddedAt = DateTime.UtcNow, IsDeleted = false }
            };

            var cards = new List<PlayerCard>
            {
                CreateProjectableCard(matchingPlayer, 90),
                CreateProjectableCard(nonMatchingPlayer, 80)
            };

            _mockShortlistRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(shortlist.BuildMockDbSet().Object);
            _mockPlayerCardRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(cards.BuildMockDbSet().Object);

            var result = await service.GetShortlistAsync(1, searchTerm: "Ronaldo");

            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public async Task GetShortlist_ShouldReturnPaginatedResults()
        {
            var service = CreateShortlistService();
            SetupPlayerCardProjection();

            var scouterDbSet = new List<Scouter> { new Scouter { Id = 1 } }.BuildMockDbSet().Object;
            _mockScouterRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(scouterDbSet);

            var players = Enumerable.Range(1, 25)
                .Select(i => CreateProjectablePlayer(i, $"Player{i}", "Test"))
                .ToList();

            var shortlist = players.Select(p => new ScouterShortlist
            {
                Id = p.Id,
                ScouterUserId = 1,
                PlayerId = p.Id,
                Player = p,
                AddedAt = DateTime.UtcNow,
                IsDeleted = false
            }).ToList();

            var cards = players.Select(p => CreateProjectableCard(p, 80)).ToList();

            _mockShortlistRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(shortlist.BuildMockDbSet().Object);
            _mockPlayerCardRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(cards.BuildMockDbSet().Object);

            var result = await service.GetShortlistAsync(1, pageNumber: 3, pageSize: 10);

            Assert.Equal(25, result.TotalCount);
            Assert.Equal(5, result.Items.Count);
            Assert.Equal(3, result.PageNumber);
        }

        #endregion

        #region SECTION 3: ScouterReportService Tests

        [Fact]
        public async Task GenerateScoutingReport_ShouldThrowNotImplementedException_BecauseAiServiceIntegrationIsPending()
        {
            var service = CreateReportService();

            await Assert.ThrowsAsync<NotImplementedException>(() => service.GenerateScoutingReportAsync(1, 10));
        }

        [Fact]
        public async Task GetScoutingReport_ShouldReturnExistingReport_WhenReportAlreadyGenerated()
        {
            var service = CreateReportService();
            var mockReport = new ScouterReport { Id = 5, ScouterUserId = 1, PlayerId = 10, ReportText = "Excellent tactical awareness." };
            _mockReportRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ScouterReport, bool>>>())).ReturnsAsync(mockReport);

            var result = await service.GetScoutingReportAsync(1, 10);

            Assert.NotNull(result);
            Assert.Equal("Excellent tactical awareness.", result.ReportText);
        }

        [Fact]
        public async Task GetScoutingReport_ShouldPropagateNotImplementedException_WhenNoExistingReportAndGenerationIsStubbed()
        {
            var service = CreateReportService();
            _mockReportRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ScouterReport, bool>>>())).ReturnsAsync((ScouterReport)null);

            await Assert.ThrowsAsync<NotImplementedException>(() => service.GetScoutingReportAsync(1, 10));
        }

        [Fact]
        public async Task VerifyScouter_ShouldThrowNotFoundException_WhenScouterDoesNotExist()
        {
            var service = CreateReportService();
            _mockScouterRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync((Scouter)null);

            await Assert.ThrowsAsync<NotFoundException>(() => service.VerifyScouterAsync(1));
        }

        [Fact]
        public async Task VerifyScouter_ShouldUpdateVerificationFlags_WhenScouterExists()
        {
            var service = CreateReportService();
            var mockScouter = new Scouter { Id = 1, IsVerified = false };
            _mockScouterRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(mockScouter);

            var result = await service.VerifyScouterAsync(1);

            Assert.True(result);
            Assert.True(mockScouter.IsVerified);
            Assert.NotNull(mockScouter.VerifiedAt);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task VerifyScouter_ShouldRemainIdempotent_WhenScouterAlreadyVerified()
        {
            var service = CreateReportService();
            var verifiedAt = DateTime.UtcNow.AddDays(-3);
            var mockScouter = new Scouter { Id = 1, IsVerified = true, VerifiedAt = verifiedAt };
            _mockScouterRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(mockScouter);

            var result = await service.VerifyScouterAsync(1);

            Assert.True(result);
            Assert.True(mockScouter.IsVerified);
            Assert.NotEqual(verifiedAt, mockScouter.VerifiedAt);
        }

        #endregion

        #region SECTION 4: ScouterSearchService - SearchPlayersAsync Tests

        [Fact]
        public async Task SearchPlayers_ShouldReturnEmptyResult_WhenFiltersIsNull()
        {
            var service = CreateSearchService();

            var emptyPlayerDbSet = new List<Player>().BuildMockDbSet().Object;
            _mockPlayerRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(emptyPlayerDbSet);

            var result = await service.SearchPlayersAsync(null);

            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
            Assert.Equal(1, result.PageNumber);
            Assert.Equal(10, result.PageSize);
        }

        [Fact]
        public async Task SearchPlayers_ShouldReturnEmptyResult_WhenNoPlayersMatch()
        {
            var service = CreateSearchService();

            var emptyPlayerDbSet = new List<Player>().BuildMockDbSet().Object;
            _mockPlayerRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(emptyPlayerDbSet);

            var filters = new PlayerSearchFiltersDto { PageNumber = 1, PageSize = 10 };

            var result = await service.SearchPlayersAsync(filters);

            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task SearchPlayers_ShouldApplyMinAndMaxAgeFilters()
        {
            var service = CreateSearchService();
            SetupPlayerCardProjection();

            var today = DateTime.UtcNow.Date;
            var players = new List<Player>
            {
                CreateProjectablePlayer(1, dateOfBirth: today.AddYears(-16)),
                CreateProjectablePlayer(2, dateOfBirth: today.AddYears(-20)),
                CreateProjectablePlayer(3, dateOfBirth: today.AddYears(-30))
            };

            var playerDbSet = players.BuildMockDbSet().Object;
            _mockPlayerRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(playerDbSet);

            // Every player the query can return (before OR after filtering) needs a card, because
            // the real map has no null-row guard: add one per player rather than relying on knowing
            // in advance exactly which player(s) survive the filter.
            var cards = players.Select(p => CreateProjectableCard(p)).ToList();
            _mockPlayerCardRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(cards.BuildMockDbSet().Object);

            var filters = new PlayerSearchFiltersDto { MinAge = 18, MaxAge = 25, PageNumber = 1, PageSize = 10 };

            var result = await service.SearchPlayersAsync(filters);

            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public async Task SearchPlayers_ShouldApplyAcademyIdFilter()
        {
            var service = CreateSearchService();
            SetupPlayerCardProjection();

            var player1 = CreateProjectablePlayer(1);
            player1.PlayerAcademies = new List<PlayerAcademy> { new PlayerAcademy { Id = 1, AcademyId = 5 } };

            var player2 = CreateProjectablePlayer(2);
            player2.PlayerAcademies = new List<PlayerAcademy> { new PlayerAcademy { Id = 2, AcademyId = 9 } };

            var players = new List<Player> { player1, player2 };

            var playerDbSet = players.BuildMockDbSet().Object;
            _mockPlayerRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(playerDbSet);

            var cards = players.Select(p => CreateProjectableCard(p)).ToList();
            _mockPlayerCardRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(cards.BuildMockDbSet().Object);

            var filters = new PlayerSearchFiltersDto { AcademyId = 5, PageNumber = 1, PageSize = 10 };

            var result = await service.SearchPlayersAsync(filters);

            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public async Task SearchPlayers_ShouldApplyMinAndMaxRatingFilters()
        {
            var service = CreateSearchService();
            SetupPlayerCardProjection();

            var player1 = CreateProjectablePlayer(1);
            var player2 = CreateProjectablePlayer(2);
            var players = new List<Player> { player1, player2 };
            _mockPlayerRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(players.BuildMockDbSet().Object);

            var cards = new List<PlayerCard>
            {
                CreateProjectableCard(player1, 85),
                CreateProjectableCard(player2, 55)
            };
            _mockPlayerCardRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(cards.BuildMockDbSet().Object);

            var filters = new PlayerSearchFiltersDto { MinRating = 70, MaxRating = 95, PageNumber = 1, PageSize = 10 };

            var result = await service.SearchPlayersAsync(filters);

            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public async Task SearchPlayers_ShouldReturnPaginatedResults()
        {
            var service = CreateSearchService();
            SetupPlayerCardProjection();

            var players = Enumerable.Range(1, 12)
                .Select(i => CreateProjectablePlayer(i))
                .ToList();

            var playerDbSet = players.BuildMockDbSet().Object;
            _mockPlayerRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(playerDbSet);

            var cards = players.Select(p => CreateProjectableCard(p)).ToList();
            _mockPlayerCardRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(cards.BuildMockDbSet().Object);

            var filters = new PlayerSearchFiltersDto { PageNumber = 2, PageSize = 5 };

            var result = await service.SearchPlayersAsync(filters);

            Assert.Equal(12, result.TotalCount);
            Assert.Equal(5, result.Items.Count);
            Assert.Equal(2, result.PageNumber);
        }

        #endregion

        #region SECTION 5: ScouterSearchService - GetScouterByIdAsync Tests

        [Fact]
        public async Task GetScouterById_ShouldThrowNotFoundException_WhenScouterDoesNotExist()
        {
            var service = CreateSearchService();

            var emptyScouterList = new List<Scouter>();
            var mockEmptyDbSet = emptyScouterList.BuildMockDbSet().Object;
            _mockScouterRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(mockEmptyDbSet);

            SetupRealMapperConfiguration();

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetScouterByIdAsync(999));
        }

        [Fact]
        public async Task GetScouterById_ShouldReturnMappedProfileDto_WhenScouterExists()
        {
            var service = CreateSearchService();

            var testScouter = new Scouter
            {
                Id = 1,
                IsVerified = true,
                FirstName = "John",
                LastName = "Scout",
                Email = "scout@test.com"
            };

            var scouterList = new List<Scouter> { testScouter };
            var mockScouterDbSet = scouterList.BuildMockDbSet().Object;
            _mockScouterRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(mockScouterDbSet);

            SetupRealMapperConfiguration();

            var result = await service.GetScouterByIdAsync(1);

            Assert.NotNull(result);
            _mockScouterRepo.Verify(r => r.GetQueryableAsNoTracking(), Times.Once);
        }

        #endregion
    }
}