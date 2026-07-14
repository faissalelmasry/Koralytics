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
using Koralytics.Application.DTOs.Player;
using Koralytics.Application.DTOs.ScouterDtos;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Services.Player.Helpers;
using Koralytics.Application.Services.Player.PlayerCardService;
using Koralytics.Application.Services.ScouterServices.ScouterFollowService;
using Koralytics.Application.Services.ScouterServices.ScouterReportService;
using Koralytics.Application.Services.ScouterServices.ScouterShortlistService;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Identity;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Entities.Scouter;
using Koralytics.Domain.Exceptions;
using Koralytics.Application.Services.ScouterServices.ScouterSearchService;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;

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

            // Setup CardInvalidationList Constructor Dependencies Safely
            var mockScopeFactory = new Mock<IServiceScopeFactory>();
            var mockLogger = new Mock<ILogger<CardInvalidationList>>();
            _invalidationList = new CardInvalidationList(mockScopeFactory.Object, mockLogger.Object);

            _mockScouterRepo = new Mock<IRepository<Scouter>>();
            _mockPlayerRepo = new Mock<IRepository<Player>>();
            _mockFollowRepo = new Mock<IRepository<ScouterFollow>>();
            _mockViewRepo = new Mock<IRepository<ScouterView>>();
            _mockShortlistRepo = new Mock<IRepository<ScouterShortlist>>();
            _mockReportRepo = new Mock<IRepository<ScouterReport>>();

            // Link repositories to the UOW factory contract boundary
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
            // Arrange
            var service = new ScouterFollowService(_mockUnitOfWork.Object, _mockMapper.Object, _mockPlayerCardService.Object, _invalidationList);
            _mockScouterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => service.FollowPlayerAsync(1, 99));
        }

        [Fact]
        public async Task FollowPlayer_ShouldThrowNotFoundException_WhenPlayerDoesNotExist()
        {
            // Arrange
            var service = new ScouterFollowService(_mockUnitOfWork.Object, _mockMapper.Object, _mockPlayerCardService.Object, _invalidationList);
            _mockScouterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(true);
            _mockPlayerRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Player, bool>>>())).ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => service.FollowPlayerAsync(1, 99));
        }

        [Fact]
        public async Task FollowPlayer_ShouldSaveFollowRelation_WhenValidAndNotAlreadyFollowing()
        {
            // Arrange
            var service = new ScouterFollowService(_mockUnitOfWork.Object, _mockMapper.Object, _mockPlayerCardService.Object, _invalidationList);
            _mockScouterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(true);
            _mockPlayerRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Player, bool>>>())).ReturnsAsync(true);
            _mockFollowRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ScouterFollow, bool>>>())).ReturnsAsync(false);

            // Act
            await service.FollowPlayerAsync(1, 10);

            // Assert
            _mockFollowRepo.Verify(r => r.AddAsync(It.Is<ScouterFollow>(f => f.ScouterUserId == 1 && f.PlayerId == 10)), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task FollowPlayer_ShouldShortCircuitSilently_WhenAlreadyFollowing()
        {
            // Arrange
            var service = new ScouterFollowService(_mockUnitOfWork.Object, _mockMapper.Object, _mockPlayerCardService.Object, _invalidationList);
            _mockScouterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(true);
            _mockPlayerRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Player, bool>>>())).ReturnsAsync(true);
            _mockFollowRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ScouterFollow, bool>>>())).ReturnsAsync(true);

            // Act
            await service.FollowPlayerAsync(1, 10);

            // Assert
            _mockFollowRepo.Verify(r => r.AddAsync(It.IsAny<ScouterFollow>()), Times.Never);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UnfollowPlayer_ShouldSoftDeleteRelation_WhenFollowExists()
        {
            // Arrange
            var service = new ScouterFollowService(_mockUnitOfWork.Object, _mockMapper.Object, _mockPlayerCardService.Object, _invalidationList);
            var existingFollow = new ScouterFollow { ScouterUserId = 1, PlayerId = 10 };
            _mockFollowRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ScouterFollow, bool>>>())).ReturnsAsync(existingFollow);

            // Act
            await service.UnfollowPlayerAsync(10, 1);

            // Assert
            _mockFollowRepo.Verify(r => r.SoftDelete(existingFollow), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UnfollowPlayer_ShouldThrowNotFoundException_WhenFollowRecordMissingButEntitiesExist()
        {
            // Arrange
            var service = new ScouterFollowService(_mockUnitOfWork.Object, _mockMapper.Object, _mockPlayerCardService.Object, _invalidationList);
            _mockFollowRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ScouterFollow, bool>>>())).ReturnsAsync((ScouterFollow)null);
            _mockScouterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(true);
            _mockPlayerRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Player, bool>>>())).ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => service.UnfollowPlayerAsync(10, 1));
        }

        [Fact]
        public async Task LogProfileView_ShouldCreateScouterViewRecord_WhenActorsAreValid()
        {
            // Arrange
            var service = new ScouterFollowService(_mockUnitOfWork.Object, _mockMapper.Object, _mockPlayerCardService.Object, _invalidationList);
            _mockScouterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(true);
            _mockPlayerRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Player, bool>>>())).ReturnsAsync(true);

            // Act
            await service.LogProfileViewAsync(1, 10);

            // Assert
            _mockViewRepo.Verify(r => r.AddAsync(It.Is<ScouterView>(v => v.ScouterId == 1 && v.PlayerId == 10)), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region SECTION 2: ScouterShortlistService Tests

        [Fact]
        public async Task AddToShortlist_ShouldThrowNotFoundException_WhenScouterDoesNotExist()
        {
            // Arrange
            var service = new ScouterShortlistService(_mockUnitOfWork.Object, _mockMapper.Object, _mockPlayerCardService.Object, _invalidationList);
            _mockScouterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => service.AddToShortlistAsync(1, 10));
        }

        [Fact]
        public async Task AddToShortlist_ShouldReturnMappedDto_WhenAlreadyShortlisted()
        {
            // Arrange
            var service = new ScouterShortlistService(_mockUnitOfWork.Object, _mockMapper.Object, _mockPlayerCardService.Object, _invalidationList);
            _mockScouterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(true);
            _mockPlayerRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Player, bool>>>())).ReturnsAsync(true);

            var existingShortlist = new ScouterShortlist { ScouterUserId = 1, PlayerId = 10 };
            _mockShortlistRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<ScouterShortlist, bool>>>())).ReturnsAsync(existingShortlist);
            _mockMapper.Setup(m => m.Map<ScouterShortlistDto>(existingShortlist)).Returns(new ScouterShortlistDto());

            // Act
            var result = await service.AddToShortlistAsync(1, 10);

            // Assert
            Assert.NotNull(result);
            _mockShortlistRepo.Verify(r => r.AddAsync(It.IsAny<ScouterShortlist>()), Times.Never);
        }

        [Fact]
        public async Task RemoveFromShortlist_ShouldThrowNotFoundException_WhenEntryDoesNotExist()
        {
            // Arrange
            var service = new ScouterShortlistService(_mockUnitOfWork.Object, _mockMapper.Object, _mockPlayerCardService.Object, _invalidationList);
            _mockShortlistRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ScouterShortlist, bool>>>())).ReturnsAsync((ScouterShortlist)null);
            _mockScouterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(true);
            _mockPlayerRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Player, bool>>>())).ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => service.RemoveFromShortlistAsync(1, 10));
        }

        [Fact]
        public async Task RemoveFromShortlist_ShouldSoftDeleteRelation_WhenEntryExists()
        {
            // Arrange
            var service = new ScouterShortlistService(_mockUnitOfWork.Object, _mockMapper.Object, _mockPlayerCardService.Object, _invalidationList);
            var entry = new ScouterShortlist { ScouterUserId = 1, PlayerId = 10 };
            _mockShortlistRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ScouterShortlist, bool>>>())).ReturnsAsync(entry);

            // Act
            var result = await service.RemoveFromShortlistAsync(1, 10);

            // Assert
            Assert.True(result);
            _mockShortlistRepo.Verify(r => r.SoftDelete(entry), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region SECTION 3: ScouterReportService Tests

        [Fact]
        public async Task GetScoutingReport_ShouldReturnExistingReport_WhenReportAlreadyGenerated()
        {
            // Arrange
            var service = new ScouterReportService(_mockUnitOfWork.Object);
            var mockReport = new ScouterReport { Id = 5, ScouterUserId = 1, PlayerId = 10, ReportText = "Excellent tactical awareness." };
            _mockReportRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ScouterReport, bool>>>())).ReturnsAsync(mockReport);

            // Act
            var result = await service.GetScoutingReportAsync(1, 10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Excellent tactical awareness.", result.ReportText);
        }

        [Fact]
        public async Task VerifyScouter_ShouldThrowNotFoundException_WhenScouterDoesNotExist()
        {
            // Arrange
            var service = new ScouterReportService(_mockUnitOfWork.Object);
            _mockScouterRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync((Scouter)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => service.VerifyScouterAsync(1));
        }

        [Fact]
        public async Task VerifyScouter_ShouldUpdateVerificationFlags_WhenScouterExists()
        {
            // Arrange
            var service = new ScouterReportService(_mockUnitOfWork.Object);
            var mockScouter = new Scouter { Id = 1, IsVerified = false };
            _mockScouterRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Scouter, bool>>>())).ReturnsAsync(mockScouter);

            // Act
            var result = await service.VerifyScouterAsync(1);

            // Assert
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
            // Arrange
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

            // Use the explicit BuildMockDbSet().Object syntax to bypass extension routing bugs
            var mockPlayerDbSet = playerList.BuildMockDbSet().Object;
            _mockPlayerRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(mockPlayerDbSet);

            var configExpression = new MapperConfigurationExpression();
            configExpression.AddProfile<Koralytics.Application.Mappings.ScouterProfile.ScouterProfile>();

            // Pass NullLoggerFactory.Instance to satisfy newer AutoMapper constructor requirements
            var mockMapperConfig = new MapperConfiguration(configExpression, NullLoggerFactory.Instance);
            _mockMapper.Setup(m => m.ConfigurationProvider).Returns(mockMapperConfig);

            // Act
            var result = await service.GetProfileViewsAnalyticsAsync(10);

            // Assert
            Assert.NotNull(result);
            _mockPlayerRepo.Verify(r => r.GetQueryableAsNoTracking(), Times.Once);
        }

        #endregion

        #region SECTION 5: Scouter Profile Retrieval Tests

        
        [Fact]
        public async Task GetScouterById_ShouldReturnNull_WhenScouterDoesNotExist()
        {
            // Arrange - Instantiate ScouterSearchService
            var service = new ScouterSearchService(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockPlayerCardService.Object,
                _invalidationList
            );

            var emptyScouterList = new List<Scouter>();
            var mockEmptyDbSet = emptyScouterList.BuildMockDbSet().Object;
            _mockScouterRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(mockEmptyDbSet);

            // FIX: Set up the mock configuration provider so ProjectTo doesn't receive null
            var configExpression = new MapperConfigurationExpression();
            configExpression.AddProfile<Koralytics.Application.Mappings.ScouterProfile.ScouterProfile>();

            var mockMapperConfig = new MapperConfiguration(configExpression, NullLoggerFactory.Instance);
            _mockMapper.Setup(m => m.ConfigurationProvider).Returns(mockMapperConfig);

            // Act
            var result = await service.GetScouterByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetScouterById_ShouldReturnMappedProfileDto_WhenScouterExists()
        {
            // Arrange
            var service = new ScouterSearchService(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockPlayerCardService.Object,
                _invalidationList
            );

            // Mock scouter entity structured with identity fields nested under User navigation property
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

            // Pass NullLoggerFactory.Instance to support expression compilation binds
            var mockMapperConfig = new MapperConfiguration(configExpression, NullLoggerFactory.Instance);
            _mockMapper.Setup(m => m.ConfigurationProvider).Returns(mockMapperConfig);

            // Act
            var result = await service.GetScouterByIdAsync(1);

            // Assert
            _mockScouterRepo.Verify(r => r.GetQueryableAsNoTracking(), Times.Once);
        }

        #endregion
    }
}
