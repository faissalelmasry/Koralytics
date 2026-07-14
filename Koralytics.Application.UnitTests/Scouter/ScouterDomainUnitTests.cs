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
using Koralytics.Application.DTOs.ScouterDtos;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Services.Player.Helpers;
using Koralytics.Application.Services.Player.PlayerCardService;
using Koralytics.Application.Services.ScouterServices.ScouterFollowService;
using Koralytics.Application.Services.ScouterServices.ScouterSearchService;
using Koralytics.Application.Services.ScouterServices.ScouterReportService;
using Koralytics.Application.Services.ScouterServices.ScouterShortlistService;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Identity;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Entities.Scouter;
using Koralytics.Domain.Exceptions;

namespace Koralytics.Tests.ScouterTests
{
    public class ScouterDomainUnitTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IPlayerCardService> _mockPlayerCardService;
        private readonly CardInvalidationList _invalidationList;

        // Repositories
        private readonly Mock<IRepository<Scouter>> _mockScouterRepo;
        private readonly Mock<IRepository<Player>> _mockPlayerRepo;
        private readonly Mock<IRepository<ScouterFollow>> _mockFollowRepo;
        private readonly Mock<IRepository<ScouterView>> _mockViewRepo;
        private readonly Mock<IRepository<ScouterShortlist>> _mockShortlistRepo;
        private readonly Mock<IRepository<ScouterReport>> _mockReportRepo;

        public ScouterDomainUnitTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockPlayerCardService = new Mock<IPlayerCardService>();

            // Setup CardInvalidationList Dependencies
            var mockScopeFactory = new Mock<IServiceScopeFactory>();
            var mockLogger = new Mock<ILogger<CardInvalidationList>>();
            _invalidationList = new CardInvalidationList(mockScopeFactory.Object, mockLogger.Object);

            _mockScouterRepo = new Mock<IRepository<Scouter>>();
            _mockPlayerRepo = new Mock<IRepository<Player>>();
            _mockFollowRepo = new Mock<IRepository<ScouterFollow>>();
            _mockViewRepo = new Mock<IRepository<ScouterView>>();
            _mockShortlistRepo = new Mock<IRepository<ScouterShortlist>>();
            _mockReportRepo = new Mock<IRepository<ScouterReport>>();

            _mockUnitOfWork.Setup(uow => uow.Repository<Scouter>()).Returns(_mockScouterRepo.Object);
            _mockUnitOfWork.Setup(uow => uow.Repository<Player>()).Returns(_mockPlayerRepo.Object);
            _mockUnitOfWork.Setup(uow => uow.Repository<ScouterFollow>()).Returns(_mockFollowRepo.Object);
            _mockUnitOfWork.Setup(uow => uow.Repository<ScouterView>()).Returns(_mockViewRepo.Object);
            _mockUnitOfWork.Setup(uow => uow.Repository<ScouterShortlist>()).Returns(_mockShortlistRepo.Object);
            _mockUnitOfWork.Setup(uow => uow.Repository<ScouterReport>()).Returns(_mockReportRepo.Object);
        }

        #region SECTION 1: ScouterFollowService Tests

        [Fact]
        public async Task FollowPlayer_ShouldThrowNotFoundException_WhenScouterDoesNotExist()
        {
            var service = new ScouterFollowService(_mockUnitOfWork.Object, _mockMapper.Object, _mockPlayerCardService.Object, _invalidationList);
            _mockScouterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(false);

            await Assert.ThrowsAsync<NotFoundException>(() => service.FollowPlayerAsync(1, 99));
        }

        [Fact]
        public async Task FollowPlayer_ShouldThrowNotFoundException_WhenPlayerDoesNotExist()
        {
            var service = new ScouterFollowService(_mockUnitOfWork.Object, _mockMapper.Object, _mockPlayerCardService.Object, _invalidationList);
            _mockScouterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(true);
            _mockPlayerRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Player, bool>>>())).ReturnsAsync(false);

            await Assert.ThrowsAsync<NotFoundException>(() => service.FollowPlayerAsync(1, 99));
        }

        [Fact]
        public async Task FollowPlayer_ShouldSaveFollowRelation_WhenValidAndNotAlreadyFollowing()
        {
            var service = new ScouterFollowService(_mockUnitOfWork.Object, _mockMapper.Object, _mockPlayerCardService.Object, _invalidationList);
            _mockScouterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(true);
            _mockPlayerRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Player, bool>>>())).ReturnsAsync(true);
            _mockFollowRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ScouterFollow, bool>>>())).ReturnsAsync(false);

            await service.FollowPlayerAsync(1, 10);

            _mockFollowRepo.Verify(r => r.AddAsync(It.Is<ScouterFollow>(f => f.ScouterUserId == 1 && f.PlayerId == 10)), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task FollowPlayer_ShouldShortCircuitSilently_WhenAlreadyFollowing()
        {
            var service = new ScouterFollowService(_mockUnitOfWork.Object, _mockMapper.Object, _mockPlayerCardService.Object, _invalidationList);
            _mockScouterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(true);
            _mockPlayerRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Player, bool>>>())).ReturnsAsync(true);
            _mockFollowRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ScouterFollow, bool>>>())).ReturnsAsync(true);

            await service.FollowPlayerAsync(1, 10);

            _mockFollowRepo.Verify(r => r.AddAsync(It.IsAny<ScouterFollow>()), Times.Never);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UnfollowPlayer_ShouldSoftDeleteRelation_WhenFollowExists()
        {
            var service = new ScouterFollowService(_mockUnitOfWork.Object, _mockMapper.Object, _mockPlayerCardService.Object, _invalidationList);
            var existingFollow = new ScouterFollow { ScouterUserId = 1, PlayerId = 10 };
            _mockFollowRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ScouterFollow, bool>>>())).ReturnsAsync(existingFollow);

            await service.UnfollowPlayerAsync(1, 10);

            _mockFollowRepo.Verify(r => r.SoftDelete(existingFollow), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UnfollowPlayer_ShouldThrowNotFoundException_WhenFollowRecordMissingButEntitiesExist()
        {
            var service = new ScouterFollowService(_mockUnitOfWork.Object, _mockMapper.Object, _mockPlayerCardService.Object, _invalidationList);
            _mockFollowRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ScouterFollow, bool>>>())).ReturnsAsync((ScouterFollow)null);
            _mockScouterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(true);
            _mockPlayerRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Player, bool>>>())).ReturnsAsync(true);

            await Assert.ThrowsAsync<NotFoundException>(() => service.UnfollowPlayerAsync(1, 10));
        }

        [Fact]
        public async Task LogProfileView_ShouldCreateScouterViewRecord_WhenActorsAreValid()
        {
            var service = new ScouterFollowService(_mockUnitOfWork.Object, _mockMapper.Object, _mockPlayerCardService.Object, _invalidationList);
            _mockScouterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(true);
            _mockPlayerRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Player, bool>>>())).ReturnsAsync(true);

            await service.LogProfileViewAsync(1, 10);

            _mockViewRepo.Verify(r => r.AddAsync(It.Is<ScouterView>(v => v.ScouterId == 1 && v.PlayerId == 10)), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region SECTION 2: ScouterShortlistService Tests

        [Fact]
        public async Task AddToShortlist_ShouldThrowNotFoundException_WhenScouterDoesNotExist()
        {
            var service = new ScouterShortlistService(_mockUnitOfWork.Object, _mockMapper.Object, _mockPlayerCardService.Object, _invalidationList);
            _mockScouterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(false);

            await Assert.ThrowsAsync<NotFoundException>(() => service.AddToShortlistAsync(1, 10));
        }

        [Fact]
        public async Task AddToShortlist_ShouldReturnMappedDto_WhenAlreadyShortlisted()
        {
            var service = new ScouterShortlistService(_mockUnitOfWork.Object, _mockMapper.Object, _mockPlayerCardService.Object, _invalidationList);
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
        public async Task RemoveFromShortlist_ShouldThrowNotFoundException_WhenEntryDoesNotExist()
        {
            var service = new ScouterShortlistService(_mockUnitOfWork.Object, _mockMapper.Object, _mockPlayerCardService.Object, _invalidationList);
            _mockShortlistRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ScouterShortlist, bool>>>())).ReturnsAsync((ScouterShortlist)null);
            _mockScouterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(true);
            _mockPlayerRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Player, bool>>>())).ReturnsAsync(true);

            await Assert.ThrowsAsync<NotFoundException>(() => service.RemoveFromShortlistAsync(1, 10));
        }

        [Fact]
        public async Task RemoveFromShortlist_ShouldSoftDeleteRelation_WhenEntryExists()
        {
            var service = new ScouterShortlistService(_mockUnitOfWork.Object, _mockMapper.Object, _mockPlayerCardService.Object, _invalidationList);
            var entry = new ScouterShortlist { ScouterUserId = 1, PlayerId = 10 };
            _mockShortlistRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ScouterShortlist, bool>>>())).ReturnsAsync(entry);

            var result = await service.RemoveFromShortlistAsync(1, 10);

            Assert.True(result);
            _mockShortlistRepo.Verify(r => r.SoftDelete(entry), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region SECTION 3: ScouterReportService Tests

        [Fact]
        public async Task GetScoutingReport_ShouldReturnExistingReport_WhenReportAlreadyGenerated()
        {
            var service = new ScouterReportService(_mockUnitOfWork.Object);
            var mockReport = new ScouterReport { Id = 5, ScouterUserId = 1, PlayerId = 10, ReportText = "Excellent tactical awareness." };
            _mockReportRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ScouterReport, bool>>>())).ReturnsAsync(mockReport);

            var result = await service.GetScoutingReportAsync(1, 10);

            Assert.NotNull(result);
            Assert.Equal("Excellent tactical awareness.", result.ReportText);
        }

        [Fact]
        public async Task VerifyScouter_ShouldThrowNotFoundException_WhenScouterDoesNotExist()
        {
            var service = new ScouterReportService(_mockUnitOfWork.Object);
            _mockScouterRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync((Scouter)null);

            await Assert.ThrowsAsync<NotFoundException>(() => service.VerifyScouterAsync(1));
        }

        [Fact]
        public async Task VerifyScouter_ShouldUpdateVerificationFlags_WhenScouterExists()
        {
            var service = new ScouterReportService(_mockUnitOfWork.Object);
            var mockScouter = new Scouter { Id = 1, IsVerified = false };
            _mockScouterRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(mockScouter);

            var result = await service.VerifyScouterAsync(1);

            Assert.True(result);
            Assert.True(mockScouter.IsVerified);
            Assert.NotNull(mockScouter.VerifiedAt);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region SECTION 4: Player Profile Views Analytics Tests

        [Fact]
        public async Task GetProfileViewsAnalytics_ShouldReturnEmptyAnalytics_WhenPlayerHasNoViews()
        {
            var service = new ScouterFollowService(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockPlayerCardService.Object,
                _invalidationList
            );

            var playerList = new List<Player>
            {
                new Player { Id = 10, ScouterViews = new List<ScouterView>() }
            };

            var mockPlayerDbSet = playerList.BuildMockDbSet().Object;
            _mockPlayerRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(mockPlayerDbSet);

            var configExpression = new MapperConfigurationExpression();
            configExpression.AddProfile<Koralytics.Application.Mappings.ScouterProfile.ScouterProfile>();

            var mockMapperConfig = new MapperConfiguration(configExpression, NullLoggerFactory.Instance);
            _mockMapper.Setup(m => m.ConfigurationProvider).Returns(mockMapperConfig);

            var result = await service.GetProfileViewsAnalyticsAsync(10);

            Assert.NotNull(result);
            _mockPlayerRepo.Verify(r => r.GetQueryableAsNoTracking(), Times.Once);
        }

        #endregion

        #region SECTION 5: Scouter Profile Retrieval Tests

        [Fact]
        public async Task GetScouterById_ShouldThrowNotFoundException_WhenScouterDoesNotExist()
        {
            // Arrange
            var service = new ScouterSearchService(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockPlayerCardService.Object,
                _invalidationList
            );

            var emptyScouterList = new List<Scouter>();
            var mockEmptyDbSet = emptyScouterList.BuildMockDbSet().Object;
            _mockScouterRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(mockEmptyDbSet);

            var configExpression = new MapperConfigurationExpression();
            configExpression.AddProfile<Koralytics.Application.Mappings.ScouterProfile.ScouterProfile>();

            var mockMapperConfig = new MapperConfiguration(configExpression, NullLoggerFactory.Instance);
            _mockMapper.Setup(m => m.ConfigurationProvider).Returns(mockMapperConfig);

            // Act & Assert
            // FIX: Assert that it throws NotFoundException instead of asserting Null!
            await Assert.ThrowsAsync<NotFoundException>(() => service.GetScouterByIdAsync(999));
        }

        [Fact]
        public async Task GetScouterById_ShouldReturnMappedProfileDto_WhenScouterExists()
        {
            var service = new ScouterSearchService(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockPlayerCardService.Object,
                _invalidationList
            );

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

            var configExpression = new MapperConfigurationExpression();
            configExpression.AddProfile<Koralytics.Application.Mappings.ScouterProfile.ScouterProfile>();

            var mockMapperConfig = new MapperConfiguration(configExpression, NullLoggerFactory.Instance);
            _mockMapper.Setup(m => m.ConfigurationProvider).Returns(mockMapperConfig);

            var result = await service.GetScouterByIdAsync(1);

            Assert.NotNull(result);
            _mockScouterRepo.Verify(r => r.GetQueryableAsNoTracking(), Times.Once);
        }

        #endregion
    }
}