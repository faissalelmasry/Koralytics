using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Koralytics.Application.DTOs.Notification;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Notification;
using Koralytics.Application.Services.Notification.AnnouncementNotificationService;
using Koralytics.Application.Services.Notification.PlayerNotificationService;
using Koralytics.Application.Services.Notification.ScouterNotificationService;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Entities.Parents;
using Koralytics.Domain.Entities.Scouter;
using Koralytics.Domain.Entities.Identity; // Added for User/Parent entity access
using Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;

namespace Koralytics.Tests.NotificationTests
{
    public class NotificationSystemUnitTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IRealTimeBridge> _mockRealTimeBridge;

        // Repositories
        private readonly Mock<IRepository<Academy>> _mockAcademyRepo;
        private readonly Mock<IRepository<Player>> _mockPlayerRepo;
        private readonly Mock<IRepository<Team>> _mockTeamRepo;
        private readonly Mock<IRepository<AgeGroup>> _mockAgeGroupRepo;
        private readonly Mock<IRepository<ParentPlayer>> _mockParentPlayerRepo;
        private readonly Mock<IRepository<ScouterFollow>> _mockScouterFollowRepo;

        public NotificationSystemUnitTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockRealTimeBridge = new Mock<IRealTimeBridge>();

            _mockAcademyRepo = new Mock<IRepository<Academy>>();
            _mockPlayerRepo = new Mock<IRepository<Player>>();
            _mockTeamRepo = new Mock<IRepository<Team>>();
            _mockAgeGroupRepo = new Mock<IRepository<AgeGroup>>();
            _mockParentPlayerRepo = new Mock<IRepository<ParentPlayer>>();
            _mockScouterFollowRepo = new Mock<IRepository<ScouterFollow>>();

            // Wire repositories up to the Unit of Work factory method
            _mockUnitOfWork.Setup(uow => uow.Repository<Academy>()).Returns(_mockAcademyRepo.Object);
            _mockUnitOfWork.Setup(uow => uow.Repository<Player>()).Returns(_mockPlayerRepo.Object);
            _mockUnitOfWork.Setup(uow => uow.Repository<Team>()).Returns(_mockTeamRepo.Object);
            _mockUnitOfWork.Setup(uow => uow.Repository<AgeGroup>()).Returns(_mockAgeGroupRepo.Object);
            _mockUnitOfWork.Setup(uow => uow.Repository<ParentPlayer>()).Returns(_mockParentPlayerRepo.Object);
            _mockUnitOfWork.Setup(uow => uow.Repository<ScouterFollow>()).Returns(_mockScouterFollowRepo.Object);
        }

        #region SECTION 1: AnnouncementNotificationService Tests

        [Fact]
        public async Task SendAnnouncement_All_ShouldBroadcastToAcademyGroup()
        {
            // Arrange
            var service = new AnnouncementNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);
            _mockAcademyRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Academy, bool>>>())).ReturnsAsync(true);

            var body = new CreateAnnouncementDto { Title = "News", Body = "Hello", TargetType = AnnouncementTargetType.All };

            // Act
            await service.SendAnnouncementNotificationAsync(1, 99, body);

            // Assert
            _mockRealTimeBridge.Verify(b => b.SendToGroupAsync("Academy_1", "ReceiveAnnouncement", It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task SendAnnouncement_Team_ShouldNotifyTeamAndFlattenedParents()
        {
            // Arrange
            var service = new AnnouncementNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);
            _mockAcademyRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Academy, bool>>>())).ReturnsAsync(true);
            _mockTeamRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Team, bool>>>())).ReturnsAsync(true);

            var playerTeams = new List<PlayerTeam>
            {
                new PlayerTeam
                {
                    TeamId = 10,
                    Player = new Player
                    {
                        ParentPlayers = new List<Parent> { new Parent { Id = 55 } }
                    }
                }
            };

            var mockPlayerTeamRepo = new Mock<IRepository<PlayerTeam>>();
            mockPlayerTeamRepo.Setup(r => r.FindAllAsync(It.IsAny<Expression<Func<PlayerTeam, bool>>>())).ReturnsAsync(playerTeams);
            _mockUnitOfWork.Setup(uow => uow.Repository<PlayerTeam>()).Returns(mockPlayerTeamRepo.Object);

            var body = new CreateAnnouncementDto { Title = "Match", Body = "Delayed", TargetType = AnnouncementTargetType.Team, TargetId = 10 };

            // Act
            await service.SendAnnouncementNotificationAsync(1, 99, body);

            // Assert
            _mockRealTimeBridge.Verify(b => b.SendToGroupAsync("Team_10", "ReceiveAnnouncement", It.IsAny<object>()), Times.Once);
            _mockRealTimeBridge.Verify(b => b.SendToGroupAsync("Parent_55", "ReceiveAnnouncement", It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task SendAnnouncement_AgeGroup_ShouldBroadcastToAgeGroupChannel()
        {
            // Arrange
            var service = new AnnouncementNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);
            _mockAcademyRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Academy, bool>>>())).ReturnsAsync(true);
            _mockAgeGroupRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<AgeGroup, bool>>>())).ReturnsAsync(true);

            var body = new CreateAnnouncementDto { Title = "Trials", Body = "U15", TargetType = AnnouncementTargetType.AgeGroup, TargetId = 5 };

            // Act
            await service.SendAnnouncementNotificationAsync(1, 99, body);

            // Assert
            _mockRealTimeBridge.Verify(b => b.SendToGroupAsync("AgeGroup_5", "ReceiveAnnouncement", It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task SendAnnouncement_Role_ShouldBroadcastToAcademyRoleCombinationGroup()
        {
            // Arrange
            var service = new AnnouncementNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);
            _mockAcademyRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Academy, bool>>>())).ReturnsAsync(true);

            var body = new CreateAnnouncementDto { Title = "Meeting", Body = "Staff Only", TargetType = AnnouncementTargetType.Role, Role = "Coach" };

            // Act
            await service.SendAnnouncementNotificationAsync(1, 99, body);

            // Assert
            _mockRealTimeBridge.Verify(b => b.SendToGroupAsync("Academy_1_Role_Coach", "ReceiveAnnouncement", It.IsAny<object>()), Times.Once);
        }

        #endregion

        #region SECTION 2: PlayerNotificationService Tests

        [Fact]
        public async Task NotifyPlayerMilestone_ShouldPushToPlayerChannel_WhenPlayerExists()
        {
            // Arrange
            var service = new PlayerNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);
            _mockPlayerRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Player, bool>>>())).ReturnsAsync(true);

            // Act
            await service.NotifyPlayerMilestoneAsync(7, "HatTrick");

            // Assert
            _mockRealTimeBridge.Verify(b => b.SendToGroupAsync("Player_7", "ReceiveMilestoneNotification", It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task NotifyParent_ShouldAlertLinkedGuardianChannels()
        {
            // Arrange
            var service = new PlayerNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);
            var links = new List<ParentPlayer> { new ParentPlayer { ParentId = 44, PlayerId = 7 } };
            _mockParentPlayerRepo.Setup(r => r.FindAllAsync(It.IsAny<Expression<Func<ParentPlayer, bool>>>())).ReturnsAsync(links);

            // Act
            await service.NotifyParentAsync(7, "InjuryAlert");

            // Assert
            _mockRealTimeBridge.Verify(b => b.SendToGroupAsync("Parent_44", "ReceiveParentNotification", It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task NotifySubscriptionGrace_ShouldAlertBothPlayerAndGuardians()
        {
            // Arrange
            var service = new PlayerNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);
            _mockAcademyRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Academy, bool>>>())).ReturnsAsync(true);
            _mockPlayerRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Player, bool>>>())).ReturnsAsync(true);

            var parents = new List<ParentPlayer> { new ParentPlayer { ParentId = 88, PlayerId = 2 } };
            _mockParentPlayerRepo.Setup(r => r.FindAllAsync(It.IsAny<Expression<Func<ParentPlayer, bool>>>())).ReturnsAsync(parents);

            // Act
            await service.NotifySubscriptionGraceAsync(2, 1);

            // Assert
            _mockRealTimeBridge.Verify(b => b.SendToGroupAsync("Parent_88", "ReceiveParentNotification", It.IsAny<object>()), Times.Once);
            _mockRealTimeBridge.Verify(b => b.SendToGroupAsync("Player_2", "ReceiveSubscriptionGraceNotification", It.IsAny<object>()), Times.Once);
        }

        #endregion

        #region SECTION 3: ScouterNotificationService Tests

        [Fact]
        public async Task NotifyScouterFollowers_ShouldPushToAllActiveFollowerRooms()
        {
            // Arrange
            var service = new ScouterNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);
            _mockPlayerRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Player, bool>>>())).ReturnsAsync(true);

            var followers = new List<ScouterFollow>
            {
                new ScouterFollow { ScouterUserId = 201, PlayerId = 3 },
                new ScouterFollow { ScouterUserId = 202, PlayerId = 3 }
            };
            _mockScouterFollowRepo.Setup(r => r.FindAllAsync(It.IsAny<Expression<Func<ScouterFollow, bool>>>())).ReturnsAsync(followers);

            // Act
            await service.NotifyScouterFollowersAsync(3, "NewHighlightUploaded");

            // Assert
            _mockRealTimeBridge.Verify(b => b.SendToGroupAsync("Scouter_201", "ReceiveScouterNotification", It.IsAny<object>()), Times.Once);
            _mockRealTimeBridge.Verify(b => b.SendToGroupAsync("Scouter_202", "ReceiveScouterNotification", It.IsAny<object>()), Times.Once);
        }

        #endregion
    }
}
