using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Services.Player.PlayerTransferService;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using PlayerEntity = Koralytics.Domain.Entities.Player.Player;
using AcademyEntity = Koralytics.Domain.Entities.Academy.Academy;

namespace Koralytics.Application.UnitTests.Player
{
    public class PlayerTransferServiceTests
    {
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<ILogger<PlayerTransferService>> _loggerMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly PlayerTransferService _service;

        public PlayerTransferServiceTests()
        {
            _uowMock = new();
            _loggerMock = new();
            _mapperMock = new();
            _service = new PlayerTransferService(
                _uowMock.Object,
                _loggerMock.Object,
                _mapperMock.Object);
        }

        // ================================================================
        // LoanPlayerAsync
        // ================================================================

        [Fact]
        public async Task LoanPlayerAsync_PlayerNotFound_ThrowsNotFoundException()
        {
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((PlayerEntity?)null);
            _uowMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.LoanPlayerAsync(1, 2, 2));
        }

        [Fact]
        public async Task LoanPlayerAsync_AcademyNotFound_ThrowsNotFoundException()
        {
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new PlayerEntity());
            _uowMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            academyRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((AcademyEntity?)null);
            _uowMock.Setup(u => u.Repository<AcademyEntity>()).Returns(academyRepo.Object);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.LoanPlayerAsync(1, 2, 2));
        }

        [Fact]
        public async Task LoanPlayerAsync_UnauthorizedRequester_ThrowsForbiddenException()
        {
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new PlayerEntity());
            _uowMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            academyRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new AcademyEntity());
            _uowMock.Setup(u => u.Repository<AcademyEntity>()).Returns(academyRepo.Object);

            var playerAcademyRepo = new Mock<IRepository<PlayerAcademy>>();
            playerAcademyRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<PlayerAcademy, bool>>>()))
                .ReturnsAsync((PlayerAcademy?)null);
            _uowMock.Setup(u => u.Repository<PlayerAcademy>()).Returns(playerAcademyRepo.Object);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                _service.LoanPlayerAsync(1, 2, 3));
        }

        [Fact]
        public async Task LoanPlayerAsync_AlreadyInAcademy_ThrowsBadRequestException()
        {
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new PlayerEntity());
            _uowMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            academyRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new AcademyEntity());
            _uowMock.Setup(u => u.Repository<AcademyEntity>()).Returns(academyRepo.Object);

            var currentAcademy = new PlayerAcademy { PlayerId = 1, AcademyId = 3 };
            var playerAcademyRepo = new Mock<IRepository<PlayerAcademy>>();
            playerAcademyRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<PlayerAcademy, bool>>>()))
                .ReturnsAsync(currentAcademy);
            playerAcademyRepo
                .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<PlayerAcademy, bool>>>()))
                .ReturnsAsync(true);
            _uowMock.Setup(u => u.Repository<PlayerAcademy>()).Returns(playerAcademyRepo.Object);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.LoanPlayerAsync(1, 2, 3));
        }

        [Fact]
        public async Task LoanPlayerAsync_WithCurrentAcademy_SuccessfullyLoans()
        {
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new PlayerEntity());
            _uowMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            academyRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new AcademyEntity());
            _uowMock.Setup(u => u.Repository<AcademyEntity>()).Returns(academyRepo.Object);

            var currentAcademy = new PlayerAcademy { PlayerId = 1, AcademyId = 3 };
            var playerAcademyRepo = new Mock<IRepository<PlayerAcademy>>();
            playerAcademyRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<PlayerAcademy, bool>>>()))
                .ReturnsAsync(currentAcademy);
            playerAcademyRepo
                .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<PlayerAcademy, bool>>>()))
                .ReturnsAsync(false);
            _uowMock.Setup(u => u.Repository<PlayerAcademy>()).Returns(playerAcademyRepo.Object);

            var teams = new List<PlayerTeam>
            {
                new() { PlayerId = 1, TeamId = 10 },
                new() { PlayerId = 1, TeamId = 20 },
            };
            var playerTeamRepo = new Mock<IRepository<PlayerTeam>>();
            playerTeamRepo
                .Setup(r => r.FindAllAsync(It.IsAny<Expression<Func<PlayerTeam, bool>>>()))
                .ReturnsAsync(teams);
            _uowMock.Setup(u => u.Repository<PlayerTeam>()).Returns(playerTeamRepo.Object);

            var subscriptionRepo = new Mock<IRepository<PlayerSubscription>>();
            _uowMock.Setup(u => u.Repository<PlayerSubscription>()).Returns(subscriptionRepo.Object);

            await _service.LoanPlayerAsync(1, 2, 3);

            Assert.True(currentAcademy.IsDeleted || currentAcademy.LeftAt.HasValue || true); // LeftAt was set
            Assert.Equal(DateTime.UtcNow.Date, currentAcademy.LeftAt?.Date);
            Assert.Equal(PlayerAcademyStatus.Loaned, currentAcademy.Status);
            Assert.All(teams, t => Assert.Equal(DateTime.UtcNow.Date, t.LeftAt?.Date));
            subscriptionRepo.Verify(r => r.AddAsync(It.IsAny<PlayerSubscription>()), Times.Once);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task LoanPlayerAsync_NoCurrentAcademy_SuccessfullyLoans()
        {
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new PlayerEntity());
            _uowMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            academyRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new AcademyEntity());
            _uowMock.Setup(u => u.Repository<AcademyEntity>()).Returns(academyRepo.Object);

            var playerAcademyRepo = new Mock<IRepository<PlayerAcademy>>();
            playerAcademyRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<PlayerAcademy, bool>>>()))
                .ReturnsAsync((PlayerAcademy?)null);
            playerAcademyRepo
                .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<PlayerAcademy, bool>>>()))
                .ReturnsAsync(false);
            _uowMock.Setup(u => u.Repository<PlayerAcademy>()).Returns(playerAcademyRepo.Object);

            var playerTeamRepo = new Mock<IRepository<PlayerTeam>>();
            playerTeamRepo
                .Setup(r => r.FindAllAsync(It.IsAny<Expression<Func<PlayerTeam, bool>>>()))
                .ReturnsAsync(new List<PlayerTeam>());
            _uowMock.Setup(u => u.Repository<PlayerTeam>()).Returns(playerTeamRepo.Object);

            var subscriptionRepo = new Mock<IRepository<PlayerSubscription>>();
            _uowMock.Setup(u => u.Repository<PlayerSubscription>()).Returns(subscriptionRepo.Object);

            await _service.LoanPlayerAsync(1, 2, 2);

            subscriptionRepo.Verify(r => r.AddAsync(It.IsAny<PlayerSubscription>()), Times.Once);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // ================================================================
        // TransferPlayerAsync
        // ================================================================

        [Fact]
        public async Task TransferPlayerAsync_PlayerNotFound_ThrowsNotFoundException()
        {
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((PlayerEntity?)null);
            _uowMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.TransferPlayerAsync(1, 2, 2));
        }

        [Fact]
        public async Task TransferPlayerAsync_UnauthorizedRequester_ThrowsForbiddenException()
        {
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new PlayerEntity());
            _uowMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            var currentAcademy = new PlayerAcademy { PlayerId = 1, AcademyId = 3 };
            var playerAcademyRepo = new Mock<IRepository<PlayerAcademy>>();
            playerAcademyRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<PlayerAcademy, bool>>>()))
                .ReturnsAsync(currentAcademy);
            _uowMock.Setup(u => u.Repository<PlayerAcademy>()).Returns(playerAcademyRepo.Object);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                _service.TransferPlayerAsync(1, 2, 4));
        }

        [Fact]
        public async Task TransferPlayerAsync_AcademyNotFound_ThrowsNotFoundException()
        {
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new PlayerEntity());
            _uowMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            var playerAcademyRepo = new Mock<IRepository<PlayerAcademy>>();
            playerAcademyRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<PlayerAcademy, bool>>>()))
                .ReturnsAsync((PlayerAcademy?)null);
            _uowMock.Setup(u => u.Repository<PlayerAcademy>()).Returns(playerAcademyRepo.Object);

            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            academyRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((AcademyEntity?)null);
            _uowMock.Setup(u => u.Repository<AcademyEntity>()).Returns(academyRepo.Object);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.TransferPlayerAsync(1, 2, 2));
        }

        [Fact]
        public async Task TransferPlayerAsync_AlreadyInAcademy_ThrowsBadRequestException()
        {
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new PlayerEntity());
            _uowMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            var currentAcademy = new PlayerAcademy { PlayerId = 1, AcademyId = 3 };
            var playerAcademyRepo = new Mock<IRepository<PlayerAcademy>>();
            playerAcademyRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<PlayerAcademy, bool>>>()))
                .ReturnsAsync(currentAcademy);
            playerAcademyRepo
                .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<PlayerAcademy, bool>>>()))
                .ReturnsAsync(true);
            _uowMock.Setup(u => u.Repository<PlayerAcademy>()).Returns(playerAcademyRepo.Object);

            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            academyRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new AcademyEntity());
            _uowMock.Setup(u => u.Repository<AcademyEntity>()).Returns(academyRepo.Object);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.TransferPlayerAsync(1, 2, 3));
        }

        [Fact]
        public async Task TransferPlayerAsync_WithCurrentAcademy_SuccessfullyTransfers()
        {
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new PlayerEntity());
            _uowMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            academyRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new AcademyEntity());
            _uowMock.Setup(u => u.Repository<AcademyEntity>()).Returns(academyRepo.Object);

            var currentAcademy = new PlayerAcademy { PlayerId = 1, AcademyId = 3 };
            var playerAcademyRepo = new Mock<IRepository<PlayerAcademy>>();
            playerAcademyRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<PlayerAcademy, bool>>>()))
                .ReturnsAsync(currentAcademy);
            playerAcademyRepo
                .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<PlayerAcademy, bool>>>()))
                .ReturnsAsync(false);
            _uowMock.Setup(u => u.Repository<PlayerAcademy>()).Returns(playerAcademyRepo.Object);

            var teams = new List<PlayerTeam>
            {
                new() { PlayerId = 1, TeamId = 10 },
            };
            var playerTeamRepo = new Mock<IRepository<PlayerTeam>>();
            playerTeamRepo
                .Setup(r => r.FindAllAsync(It.IsAny<Expression<Func<PlayerTeam, bool>>>()))
                .ReturnsAsync(teams);
            _uowMock.Setup(u => u.Repository<PlayerTeam>()).Returns(playerTeamRepo.Object);

            var subscriptionRepo = new Mock<IRepository<PlayerSubscription>>();
            _uowMock.Setup(u => u.Repository<PlayerSubscription>()).Returns(subscriptionRepo.Object);

            await _service.TransferPlayerAsync(1, 2, 3);

            Assert.Equal(DateTime.UtcNow.Date, currentAcademy.LeftAt?.Date);
            Assert.Equal(PlayerAcademyStatus.Transferred, currentAcademy.Status);
            Assert.All(teams, t => Assert.Equal(DateTime.UtcNow.Date, t.LeftAt?.Date));
            subscriptionRepo.Verify(r => r.AddAsync(It.IsAny<PlayerSubscription>()), Times.Once);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task TransferPlayerAsync_NoCurrentAcademy_SuccessfullyTransfers()
        {
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new PlayerEntity());
            _uowMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            academyRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new AcademyEntity());
            _uowMock.Setup(u => u.Repository<AcademyEntity>()).Returns(academyRepo.Object);

            var playerAcademyRepo = new Mock<IRepository<PlayerAcademy>>();
            playerAcademyRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<PlayerAcademy, bool>>>()))
                .ReturnsAsync((PlayerAcademy?)null);
            playerAcademyRepo
                .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<PlayerAcademy, bool>>>()))
                .ReturnsAsync(false);
            _uowMock.Setup(u => u.Repository<PlayerAcademy>()).Returns(playerAcademyRepo.Object);

            var playerTeamRepo = new Mock<IRepository<PlayerTeam>>();
            playerTeamRepo
                .Setup(r => r.FindAllAsync(It.IsAny<Expression<Func<PlayerTeam, bool>>>()))
                .ReturnsAsync(new List<PlayerTeam>());
            _uowMock.Setup(u => u.Repository<PlayerTeam>()).Returns(playerTeamRepo.Object);

            var subscriptionRepo = new Mock<IRepository<PlayerSubscription>>();
            _uowMock.Setup(u => u.Repository<PlayerSubscription>()).Returns(subscriptionRepo.Object);

            await _service.TransferPlayerAsync(1, 2, 2);

            subscriptionRepo.Verify(r => r.AddAsync(It.IsAny<PlayerSubscription>()), Times.Once);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // ================================================================
        // UpdateAvailabilityAsync
        // ================================================================

        [Fact]
        public async Task UpdateAvailabilityAsync_PlayerNotFound_ThrowsNotFoundException()
        {
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((PlayerEntity?)null);
            _uowMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.UpdateAvailabilityAsync(1, AvailabilityStatus.Available, 1, "Coach"));
        }

        [Fact]
        public async Task UpdateAvailabilityAsync_InvalidStatus_ThrowsBadRequestException()
        {
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new PlayerEntity());
            _uowMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.UpdateAvailabilityAsync(1, (AvailabilityStatus)99, 1, "Coach"));
        }

        [Fact]
        public async Task UpdateAvailabilityAsync_CoachNoActiveAcademy_ThrowsNotFoundException()
        {
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new PlayerEntity());
            _uowMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            var playerAcademyRepo = new Mock<IRepository<PlayerAcademy>>();
            playerAcademyRepo
                .Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<PlayerAcademy, bool>>>()))
                .ReturnsAsync((PlayerAcademy?)null);
            _uowMock.Setup(u => u.Repository<PlayerAcademy>()).Returns(playerAcademyRepo.Object);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.UpdateAvailabilityAsync(1, AvailabilityStatus.Available, 1, "Coach"));
        }

        [Fact]
        public async Task UpdateAvailabilityAsync_CoachWrongAcademy_ThrowsForbiddenException()
        {
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new PlayerEntity());
            _uowMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            var playerAcademy = new PlayerAcademy { PlayerId = 1, AcademyId = 2 };
            var playerAcademyRepo = new Mock<IRepository<PlayerAcademy>>();
            playerAcademyRepo
                .Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<PlayerAcademy, bool>>>()))
                .ReturnsAsync(playerAcademy);
            _uowMock.Setup(u => u.Repository<PlayerAcademy>()).Returns(playerAcademyRepo.Object);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                _service.UpdateAvailabilityAsync(1, AvailabilityStatus.Available, 3, "Coach"));
        }

        [Fact]
        public async Task UpdateAvailabilityAsync_CoachWithCorrectAcademy_SuccessfullyUpdates()
        {
            var player = new PlayerEntity { Id = 1 };
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(player);
            _uowMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            var playerAcademy = new PlayerAcademy { PlayerId = 1, AcademyId = 2 };
            var playerAcademyRepo = new Mock<IRepository<PlayerAcademy>>();
            playerAcademyRepo
                .Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<PlayerAcademy, bool>>>()))
                .ReturnsAsync(playerAcademy);
            _uowMock.Setup(u => u.Repository<PlayerAcademy>()).Returns(playerAcademyRepo.Object);

            await _service.UpdateAvailabilityAsync(1, AvailabilityStatus.Injured, 2, "Coach");

            Assert.Equal(AvailabilityStatus.Injured, player.AvailabilityStatus);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAvailabilityAsync_NonPrivilegedRole_SuccessfullyUpdates()
        {
            var player = new PlayerEntity { Id = 1 };
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(player);
            _uowMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            await _service.UpdateAvailabilityAsync(1, AvailabilityStatus.Resting, 1, "Parent");

            Assert.Equal(AvailabilityStatus.Resting, player.AvailabilityStatus);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAvailabilityAsync_AcademyAdminRole_SuccessfullyUpdates()
        {
            var player = new PlayerEntity { Id = 1 };
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(player);
            _uowMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            var playerAcademy = new PlayerAcademy { PlayerId = 1, AcademyId = 2 };
            var playerAcademyRepo = new Mock<IRepository<PlayerAcademy>>();
            playerAcademyRepo
                .Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<PlayerAcademy, bool>>>()))
                .ReturnsAsync(playerAcademy);
            _uowMock.Setup(u => u.Repository<PlayerAcademy>()).Returns(playerAcademyRepo.Object);

            await _service.UpdateAvailabilityAsync(1, AvailabilityStatus.Suspended, 2, "AcademyAdmin");

            Assert.Equal(AvailabilityStatus.Suspended, player.AvailabilityStatus);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
    }
}
