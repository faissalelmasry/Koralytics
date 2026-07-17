using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
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
using Koralytics.Domain.Entities.Coach;
using Koralytics.Domain.Entities.Identity;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;
using Microsoft.EntityFrameworkCore.Query;

namespace Koralytics.Tests.NotificationTests
{
    public class NotificationSystemUnitTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IRealTimeBridge> _mockRealTimeBridge;

        private readonly Mock<IRepository<Academy>> _mockAcademyRepo;
        private readonly Mock<IRepository<Player>> _mockPlayerRepo;
        private readonly Mock<IRepository<Team>> _mockTeamRepo;
        private readonly Mock<IRepository<AgeGroup>> _mockAgeGroupRepo;
        private readonly Mock<IRepository<ParentPlayer>> _mockParentPlayerRepo;
        private readonly Mock<IRepository<ScouterFollow>> _mockScouterFollowRepo;
        private readonly Mock<IRepository<PlayerAcademy>> _mockPlayerAcademyRepo;
        private readonly Mock<IRepository<PlayerTeam>> _mockPlayerTeamRepo;
        private readonly Mock<IRepository<CoachAcademy>> _mockCoachAcademyRepo;

        // Caller identity used across the "happy path" tests: userId 99 acting on academyId 1.
        private const int DefaultCallerUserId = 99;
        private const int DefaultAcademyId = 1;

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
            _mockPlayerAcademyRepo = new Mock<IRepository<PlayerAcademy>>();
            _mockPlayerTeamRepo = new Mock<IRepository<PlayerTeam>>();
            _mockCoachAcademyRepo = new Mock<IRepository<CoachAcademy>>();

            _mockUnitOfWork.Setup(uow => uow.Repository<Academy>()).Returns(_mockAcademyRepo.Object);
            _mockUnitOfWork.Setup(uow => uow.Repository<Player>()).Returns(_mockPlayerRepo.Object);
            _mockUnitOfWork.Setup(uow => uow.Repository<Team>()).Returns(_mockTeamRepo.Object);
            _mockUnitOfWork.Setup(uow => uow.Repository<AgeGroup>()).Returns(_mockAgeGroupRepo.Object);
            _mockUnitOfWork.Setup(uow => uow.Repository<ParentPlayer>()).Returns(_mockParentPlayerRepo.Object);
            _mockUnitOfWork.Setup(uow => uow.Repository<ScouterFollow>()).Returns(_mockScouterFollowRepo.Object);
            _mockUnitOfWork.Setup(uow => uow.Repository<PlayerAcademy>()).Returns(_mockPlayerAcademyRepo.Object);
            _mockUnitOfWork.Setup(uow => uow.Repository<PlayerTeam>()).Returns(_mockPlayerTeamRepo.Object);
            _mockUnitOfWork.Setup(uow => uow.Repository<CoachAcademy>()).Returns(_mockCoachAcademyRepo.Object);

            // --- Baseline "authorized caller" defaults ---
            // AnnouncementNotificationService now checks the caller is an active coach
            // of the target academy before doing anything else. Default this to true
            // so existing "happy path" tests don't all need to repeat the setup; the
            // dedicated authorization tests below override it back to false.
            _mockCoachAcademyRepo
                .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<CoachAcademy, bool>>>()))
                .ReturnsAsync(true);

            // PlayerNotificationService.NotifySubscriptionGraceAsync now checks the
            // player actually belongs to the academy. Default to true for the same
            // reason; overridden in the dedicated "not enrolled" test.
            _mockPlayerAcademyRepo
                .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<PlayerAcademy, bool>>>()))
                .ReturnsAsync(true);
        }

        private IQueryable<T> BuildMockQueryable<T>(List<T> list)
        {
            return new TestDbAsyncEnumerable<T>(list);
        }

        /// <summary>
        /// Matches an IEnumerable&lt;int&gt; argument against an expected set of user ids,
        /// order-independent. Used to verify the bounded fan-out call
        /// (SendAndCacheToUsersAsync) received exactly the recipients we expect.
        /// </summary>
        private static Expression<Func<IEnumerable<int>, bool>> MatchesUserIds(params int[] expectedIds)
        {
            var expectedSet = new HashSet<int>(expectedIds);
            return ids => ids != null
                && new HashSet<int>(ids).SetEquals(expectedSet);
        }

        #region SECTION 1: AnnouncementNotificationService Tests

        [Fact]
        public async Task SendAnnouncement_NullPayload_ShouldThrowBadRequestException()
        {
            var service = new AnnouncementNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);
            await Assert.ThrowsAsync<BadRequestException>(() => service.SendAnnouncementNotificationAsync(1, 99, null));
        }

        [Fact]
        public async Task SendAnnouncement_EmptyTitleOrBody_ShouldThrowBadRequestException()
        {
            var service = new AnnouncementNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);
            var body = new CreateAnnouncementDto { Title = " ", Body = "Valid content" };
            await Assert.ThrowsAsync<BadRequestException>(() => service.SendAnnouncementNotificationAsync(1, 99, body));
        }

        [Fact]
        public async Task SendAnnouncement_AcademyDoesNotExist_ShouldThrowNotFoundException()
        {
            var service = new AnnouncementNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);
            _mockAcademyRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Academy, bool>>>())).ReturnsAsync(false);
            var body = new CreateAnnouncementDto { Title = "News", Body = "Hello", TargetType = AnnouncementTargetType.All };

            await Assert.ThrowsAsync<NotFoundException>(() => service.SendAnnouncementNotificationAsync(1, 99, body));
        }

        [Fact]
        public async Task SendAnnouncement_CallerNotAcademyStaff_ShouldThrowForbiddenException()
        {
            var service = new AnnouncementNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);
            _mockAcademyRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Academy, bool>>>())).ReturnsAsync(true);

            // Override the constructor's default "authorized" baseline: this caller
            // has no active CoachAcademy row for the target academy.
            _mockCoachAcademyRepo
                .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<CoachAcademy, bool>>>()))
                .ReturnsAsync(false);

            var body = new CreateAnnouncementDto { Title = "News", Body = "Hello", TargetType = AnnouncementTargetType.All };

            await Assert.ThrowsAsync<ForbiddenException>(() => service.SendAnnouncementNotificationAsync(DefaultAcademyId, DefaultCallerUserId, body));

            // Nothing should have been sent or cached if the caller was rejected.
            _mockRealTimeBridge.Verify(b => b.SendAndCacheToUsersAsync(
                It.IsAny<IEnumerable<int>>(), It.IsAny<string>(), It.IsAny<CachedNotification>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SendAnnouncement_All_ShouldSendAndCacheToAllActivePlayers()
        {
            var service = new AnnouncementNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);
            _mockAcademyRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Academy, bool>>>())).ReturnsAsync(true);

            var playerAcademies = new List<PlayerAcademy>
            {
                new PlayerAcademy { AcademyId = 1, Status = PlayerAcademyStatus.Active, PlayerId = 101 },
                new PlayerAcademy { AcademyId = 1, Status = PlayerAcademyStatus.Active, PlayerId = 102 }
            };

            _mockPlayerAcademyRepo.Setup(r => r.GetQueryableAsNoTracking())
                .Returns(BuildMockQueryable(playerAcademies));

            var body = new CreateAnnouncementDto { Title = "News", Body = "Hello", TargetType = AnnouncementTargetType.All };

            await service.SendAnnouncementNotificationAsync(DefaultAcademyId, DefaultCallerUserId, body);

            // The service now fans out via a single bounded-concurrency call rather
            // than one SendAndCacheToUserAsync per recipient.
            _mockRealTimeBridge.Verify(b => b.SendAndCacheToUsersAsync(
                It.Is(MatchesUserIds(101, 102)),
                "ReceiveAnnouncement",
                It.IsAny<CachedNotification>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SendAnnouncement_Team_InvalidTargetId_ShouldThrowBadRequestException()
        {
            var service = new AnnouncementNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);
            _mockAcademyRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Academy, bool>>>())).ReturnsAsync(true);
            var body = new CreateAnnouncementDto { Title = "Match", Body = "Delayed", TargetType = AnnouncementTargetType.Team, TargetId = 0 };

            await Assert.ThrowsAsync<BadRequestException>(() => service.SendAnnouncementNotificationAsync(1, 99, body));
        }

        [Fact]
        public async Task SendAnnouncement_Team_DoesNotExist_ShouldThrowNotFoundException()
        {
            var service = new AnnouncementNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);
            _mockAcademyRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Academy, bool>>>())).ReturnsAsync(true);
            _mockTeamRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Team, bool>>>())).ReturnsAsync(false);
            var body = new CreateAnnouncementDto { Title = "Match", Body = "Delayed", TargetType = AnnouncementTargetType.Team, TargetId = 10 };

            await Assert.ThrowsAsync<NotFoundException>(() => service.SendAnnouncementNotificationAsync(1, 99, body));
        }

        [Fact]
        public async Task SendAnnouncement_Team_ShouldSendAndCacheToTeamPlayersAndTheirParents()
        {
            var service = new AnnouncementNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);
            _mockAcademyRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Academy, bool>>>())).ReturnsAsync(true);
            _mockTeamRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Team, bool>>>())).ReturnsAsync(true);

            var playerTeams = new List<PlayerTeam>
            {
                new PlayerTeam
                {
                    TeamId = 10,
                    Team = new Team { AcademyId = 1 },
                    PlayerId = 101
                }
            };

            _mockPlayerTeamRepo.Setup(r => r.GetQueryableAsNoTracking())
                .Returns(BuildMockQueryable(playerTeams));

            // Player 101 has one linked parent (55). Team-targeted announcements must
            // now reach both the player and this parent.
            var parentLinks = new List<ParentPlayer>
            {
                new ParentPlayer { PlayerId = 101, ParentId = 55 }
            };

            _mockParentPlayerRepo.Setup(r => r.GetQueryableAsNoTracking())
                .Returns(BuildMockQueryable(parentLinks));

            var body = new CreateAnnouncementDto { Title = "Match", Body = "Delayed", TargetType = AnnouncementTargetType.Team, TargetId = 10 };

            await service.SendAnnouncementNotificationAsync(DefaultAcademyId, DefaultCallerUserId, body);

            _mockRealTimeBridge.Verify(b => b.SendAndCacheToUsersAsync(
                It.Is(MatchesUserIds(101, 55)),
                "ReceiveAnnouncement",
                It.IsAny<CachedNotification>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SendAnnouncement_AgeGroup_DoesNotExist_ShouldThrowNotFoundException()
        {
            var service = new AnnouncementNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);
            _mockAcademyRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Academy, bool>>>())).ReturnsAsync(true);
            _mockAgeGroupRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<AgeGroup, bool>>>())).ReturnsAsync(false);
            var body = new CreateAnnouncementDto { Title = "Trials", Body = "U15", TargetType = AnnouncementTargetType.AgeGroup, TargetId = 5 };

            await Assert.ThrowsAsync<NotFoundException>(() => service.SendAnnouncementNotificationAsync(1, 99, body));
        }

        [Fact]
        public async Task SendAnnouncement_AgeGroup_ShouldSendAndCacheToAgeGroupPlayers()
        {
            var service = new AnnouncementNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);
            _mockAcademyRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Academy, bool>>>())).ReturnsAsync(true);
            _mockAgeGroupRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<AgeGroup, bool>>>())).ReturnsAsync(true);

            var playerTeams = new List<PlayerTeam>
            {
                new PlayerTeam
                {
                    Team = new Team
                    {
                        AcademyId = 1,
                        AgeGroupId = 5
                    },
                    PlayerId = 101
                }
            };

            _mockPlayerTeamRepo.Setup(r => r.GetQueryableAsNoTracking())
                .Returns(BuildMockQueryable(playerTeams));

            var body = new CreateAnnouncementDto { Title = "Trials", Body = "U15", TargetType = AnnouncementTargetType.AgeGroup, TargetId = 5 };

            await service.SendAnnouncementNotificationAsync(DefaultAcademyId, DefaultCallerUserId, body);

            _mockRealTimeBridge.Verify(b => b.SendAndCacheToUsersAsync(
                It.Is(MatchesUserIds(101)),
                "ReceiveAnnouncement",
                It.IsAny<CachedNotification>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SendAnnouncement_Role_EmptyRole_ShouldThrowBadRequestException()
        {
            var service = new AnnouncementNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);
            _mockAcademyRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Academy, bool>>>())).ReturnsAsync(true);
            var body = new CreateAnnouncementDto { Title = "Meeting", Body = "Staff Only", TargetType = AnnouncementTargetType.Role, Role = "" };

            await Assert.ThrowsAsync<BadRequestException>(() => service.SendAnnouncementNotificationAsync(1, 99, body));
        }

        [Fact]
        public async Task SendAnnouncement_Role_UnrecognizedRole_ShouldThrowBadRequestException()
        {
            var service = new AnnouncementNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);
            _mockAcademyRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Academy, bool>>>())).ReturnsAsync(true);
            var body = new CreateAnnouncementDto { Title = "Meeting", Body = "Staff Only", TargetType = AnnouncementTargetType.Role, Role = "Groundskeeper" };

            // Previously this silently produced an empty target list and a 202-style
            // no-op. Any role outside the supported set must now be a hard 400.
            await Assert.ThrowsAsync<BadRequestException>(() => service.SendAnnouncementNotificationAsync(DefaultAcademyId, DefaultCallerUserId, body));

            _mockRealTimeBridge.Verify(b => b.SendAndCacheToUsersAsync(
                It.IsAny<IEnumerable<int>>(), It.IsAny<string>(), It.IsAny<CachedNotification>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SendAnnouncement_Role_Coach_ShouldSendAndCacheToCoaches()
        {
            var service = new AnnouncementNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);
            _mockAcademyRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Academy, bool>>>())).ReturnsAsync(true);

            var coachAcademies = new List<CoachAcademy>
            {
                new CoachAcademy { AcademyId = 1, CoachUserId = 201, LeftAt = null }
            };

            _mockCoachAcademyRepo.Setup(r => r.GetQueryableAsNoTracking())
                .Returns(BuildMockQueryable(coachAcademies));

            var body = new CreateAnnouncementDto { Title = "Meeting", Body = "Staff Only", TargetType = AnnouncementTargetType.Role, Role = "Coach" };

            await service.SendAnnouncementNotificationAsync(DefaultAcademyId, DefaultCallerUserId, body);

            _mockRealTimeBridge.Verify(b => b.SendAndCacheToUsersAsync(
                It.Is(MatchesUserIds(201)),
                "ReceiveAnnouncement",
                It.IsAny<CachedNotification>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SendAnnouncement_Role_Player_ShouldSendAndCacheToPlayers()
        {
            var service = new AnnouncementNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);
            _mockAcademyRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Academy, bool>>>())).ReturnsAsync(true);

            var playerAcademies = new List<PlayerAcademy>
            {
                new PlayerAcademy { AcademyId = 1, Status = PlayerAcademyStatus.Active, PlayerId = 101 }
            };

            _mockPlayerAcademyRepo.Setup(r => r.GetQueryableAsNoTracking())
                .Returns(BuildMockQueryable(playerAcademies));

            var body = new CreateAnnouncementDto { Title = "Announcement", Body = "Hello Players", TargetType = AnnouncementTargetType.Role, Role = "Player" };

            await service.SendAnnouncementNotificationAsync(DefaultAcademyId, DefaultCallerUserId, body);

            _mockRealTimeBridge.Verify(b => b.SendAndCacheToUsersAsync(
                It.Is(MatchesUserIds(101)),
                "ReceiveAnnouncement",
                It.IsAny<CachedNotification>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region SECTION 2: PlayerNotificationService Tests

        [Fact]
        public async Task NotifyPlayerMilestone_ShouldPushToPlayerChannel_WhenPlayerExists()
        {
            var service = new PlayerNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);
            _mockPlayerRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Player, bool>>>())).ReturnsAsync(true);

            await service.NotifyPlayerMilestoneAsync(7, "HatTrick");

            _mockRealTimeBridge.Verify(b => b.SendAndCacheToUserAsync(7, "ReceiveMilestoneNotification", It.IsAny<CachedNotification>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task NotifyParent_ShouldAlertLinkedGuardianChannels()
        {
            var service = new PlayerNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);

            var links = new List<ParentPlayer> { new ParentPlayer { ParentId = 44, PlayerId = 7 } };
            _mockParentPlayerRepo.Setup(r => r.FindAllAsync(It.IsAny<Expression<Func<ParentPlayer, bool>>>())).ReturnsAsync(links);

            await service.NotifyParentAsync(7, "InjuryAlert");

            // Parent fan-out now goes through the bounded/resilient batch method.
            _mockRealTimeBridge.Verify(b => b.SendAndCacheToUsersAsync(
                It.Is(MatchesUserIds(44)),
                "ReceiveParentNotification",
                It.IsAny<CachedNotification>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task NotifyParent_NoLinkedGuardians_ShouldNotCallRealTimeBridge()
        {
            var service = new PlayerNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);

            _mockParentPlayerRepo.Setup(r => r.FindAllAsync(It.IsAny<Expression<Func<ParentPlayer, bool>>>())).ReturnsAsync(new List<ParentPlayer>());

            await service.NotifyParentAsync(7, "InjuryAlert");

            _mockRealTimeBridge.Verify(b => b.SendAndCacheToUsersAsync(
                It.IsAny<IEnumerable<int>>(), It.IsAny<string>(), It.IsAny<CachedNotification>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task NotifySubscriptionGrace_AcademyDoesNotExist_ShouldThrowNotFoundException()
        {
            var service = new PlayerNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);
            _mockAcademyRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Academy, bool>>>())).ReturnsAsync(false);

            await Assert.ThrowsAsync<NotFoundException>(() => service.NotifySubscriptionGraceAsync(2, 1));
        }

        [Fact]
        public async Task NotifySubscriptionGrace_PlayerDoesNotExist_ShouldThrowNotFoundException()
        {
            var service = new PlayerNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);
            _mockAcademyRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Academy, bool>>>())).ReturnsAsync(true);
            _mockPlayerRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Player, bool>>>())).ReturnsAsync(false);

            await Assert.ThrowsAsync<NotFoundException>(() => service.NotifySubscriptionGraceAsync(2, 1));
        }

        [Fact]
        public async Task NotifySubscriptionGrace_PlayerNotEnrolledInAcademy_ShouldThrowBadRequestException()
        {
            var service = new PlayerNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);
            _mockAcademyRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Academy, bool>>>())).ReturnsAsync(true);
            _mockPlayerRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Player, bool>>>())).ReturnsAsync(true);

            // Override the constructor's default "enrolled" baseline: this player has
            // no PlayerAcademy row for the target academy at all.
            _mockPlayerAcademyRepo
                .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<PlayerAcademy, bool>>>()))
                .ReturnsAsync(false);

            await Assert.ThrowsAsync<BadRequestException>(() => service.NotifySubscriptionGraceAsync(2, 1));

            _mockRealTimeBridge.Verify(b => b.SendAndCacheToUserAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CachedNotification>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task NotifySubscriptionGrace_ShouldAlertBothPlayerAndGuardians()
        {
            var service = new PlayerNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);
            _mockAcademyRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Academy, bool>>>())).ReturnsAsync(true);
            _mockPlayerRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Player, bool>>>())).ReturnsAsync(true);
            // PlayerAcademy.ExistsAsync defaults to true via the constructor baseline.

            var parents = new List<ParentPlayer> { new ParentPlayer { ParentId = 88, PlayerId = 2 } };
            _mockParentPlayerRepo.Setup(r => r.FindAllAsync(It.IsAny<Expression<Func<ParentPlayer, bool>>>())).ReturnsAsync(parents);

            await service.NotifySubscriptionGraceAsync(2, 1);

            _mockRealTimeBridge.Verify(b => b.SendAndCacheToUserAsync(2, "ReceiveSubscriptionGraceNotification", It.IsAny<CachedNotification>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockRealTimeBridge.Verify(b => b.SendAndCacheToUsersAsync(
                It.Is(MatchesUserIds(88)),
                "ReceiveParentNotification",
                It.IsAny<CachedNotification>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region SECTION 3: ScouterNotificationService Tests

        [Fact]
        public async Task NotifyScouterFollowers_ShouldPushToAllActiveFollowerRooms()
        {
            var service = new ScouterNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);
            _mockPlayerRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Player, bool>>>())).ReturnsAsync(true);

            var followers = new List<ScouterFollow>
            {
                new ScouterFollow { ScouterUserId = 201, PlayerId = 3 },
                new ScouterFollow { ScouterUserId = 202, PlayerId = 3 }
            };
            _mockScouterFollowRepo.Setup(r => r.FindAllAsync(It.IsAny<Expression<Func<ScouterFollow, bool>>>())).ReturnsAsync(followers);

            await service.NotifyScouterFollowersAsync(3, "NewHighlightUploaded");

            _mockRealTimeBridge.Verify(b => b.SendAndCacheToUsersAsync(
                It.Is(MatchesUserIds(201, 202)),
                "ReceiveScouterNotification",
                It.IsAny<CachedNotification>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task NotifyScouterFollowers_PlayerDoesNotExist_ShouldThrowNotFoundException()
        {
            var service = new ScouterNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);
            _mockPlayerRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Player, bool>>>())).ReturnsAsync(false);

            await Assert.ThrowsAsync<NotFoundException>(() => service.NotifyScouterFollowersAsync(3, "NewHighlightUploaded"));
        }

        [Fact]
        public async Task NotifyScouterFollowers_NoFollowers_ShouldNotCallRealTimeBridge()
        {
            var service = new ScouterNotificationService(_mockUnitOfWork.Object, _mockRealTimeBridge.Object);
            _mockPlayerRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Player, bool>>>())).ReturnsAsync(true);
            _mockScouterFollowRepo.Setup(r => r.FindAllAsync(It.IsAny<Expression<Func<ScouterFollow, bool>>>())).ReturnsAsync(new List<ScouterFollow>());

            await service.NotifyScouterFollowersAsync(3, "NewHighlightUploaded");

            _mockRealTimeBridge.Verify(b => b.SendAndCacheToUsersAsync(
                It.IsAny<IEnumerable<int>>(), It.IsAny<string>(), It.IsAny<CachedNotification>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion
    }

    #region EF Async Queryable Mock Helpers

    internal class TestDbAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestDbAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestDbAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestDbAsyncEnumerable<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var expectedResultType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = typeof(IQueryProvider)
                .GetMethods()
                .First(method => method.Name == nameof(IQueryProvider.Execute) && method.IsGenericMethod)
                .MakeGenericMethod(expectedResultType)
                .Invoke(this, new object[] { expression });

            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
                ?.MakeGenericMethod(expectedResultType)
                .Invoke(null, new[] { executionResult });
        }
    }

    internal class TestDbAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestDbAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
        public TestDbAsyncEnumerable(Expression expression) : base(expression) { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestDbAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IQueryProvider IQueryable.Provider => new TestDbAsyncQueryProvider<T>(this);
    }

    internal class TestDbAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestDbAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public T Current => _inner.Current;

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return ValueTask.FromResult(_inner.MoveNext());
        }
    }

    #endregion
}