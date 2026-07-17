using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using Koralytics.Application.DTOs.Academies;
using Koralytics.Application.DTOs.SystemAdmin;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Services.Academy.AcademyService;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Coach;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Entities.SystemAdmin;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Xunit;
using AcademyEntity = Koralytics.Domain.Entities.Academy.Academy;
using PlayerEntity = Koralytics.Domain.Entities.Player.Player;

namespace Koralytics.Application.UnitTests.Academies
{
    public class AcademyServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<AcademyService>> _loggerMock;
        private readonly AcademyService _service;

        public AcademyServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<AcademyService>>();

            _service = new AcademyService(
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        // --- ApproveAcademyAsync ---

        [Fact]
        public async Task ApproveAcademyAsync_RequestNotFound_ThrowsNotFoundException()
        {
            var reqRepo = new Mock<IRepository<AcademyRequest>>();
            reqRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AcademyRequest, bool>>>()))
                .ReturnsAsync((AcademyRequest?)null);
            _unitOfWorkMock.Setup(u => u.Repository<AcademyRequest>()).Returns(reqRepo.Object);

            var dto = new CreateAcademyDto { AcademyRequestId = 1, Name = "Test Academy" };
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.ApproveAcademyAsync(dto, 100));
        }

        [Fact]
        public async Task ApproveAcademyAsync_NameAlreadyTaken_ThrowsConflictException()
        {
            var request = new AcademyRequest { Id = 1, RequestStatus = AcademyRequestStatus.Pending };
            var reqRepo = new Mock<IRepository<AcademyRequest>>();
            reqRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AcademyRequest, bool>>>()))
                .ReturnsAsync(request);
            _unitOfWorkMock.Setup(u => u.Repository<AcademyRequest>()).Returns(reqRepo.Object);

            var acadRepo = new Mock<IRepository<AcademyEntity>>();
            acadRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<AcademyEntity, bool>>>()))
                .ReturnsAsync(true);
            _unitOfWorkMock.Setup(u => u.Repository<AcademyEntity>()).Returns(acadRepo.Object);

            var dto = new CreateAcademyDto { AcademyRequestId = 1, Name = "Existing Academy" };
            await Assert.ThrowsAsync<ConflictException>(() =>
                _service.ApproveAcademyAsync(dto, 100));
        }

        // --- AddLocationAsync ---

        [Fact]
        public async Task AddLocationAsync_AcademyNotFound_ThrowsNotFoundException()
        {
            var acadRepo = new Mock<IRepository<AcademyEntity>>();
            acadRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AcademyEntity, bool>>>()))
                .ReturnsAsync((AcademyEntity?)null);
            _unitOfWorkMock.Setup(u => u.Repository<AcademyEntity>()).Returns(acadRepo.Object);

            var dto = new AddLocationDto { Name = "Main Branch" };
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.AddLocationAsync(1, dto, 100));
        }

        // --- GetAcademyAsync ---

        [Fact]
        public async Task GetAcademyAsync_Success_ReturnsDto()
        {
            var acadRepo = new Mock<IRepository<AcademyEntity>>();
            var academy = new AcademyEntity { Id = 1, Name = "Koralytics Academy" };
            acadRepo.Setup(r => r.GetQueryableAsNoTracking())
                .Returns(new List<AcademyEntity> { academy }.BuildMock());
            _unitOfWorkMock.Setup(u => u.Repository<AcademyEntity>()).Returns(acadRepo.Object);

            _mapperMock.Setup(m => m.Map<AcademyResponseDto>(It.IsAny<AcademyEntity>()))
                .Returns(new AcademyResponseDto { Id = 1, Name = "Koralytics Academy" });

            var result = await _service.GetAcademyAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Koralytics Academy", result.Name);
        }

        // --- SendPlayerJoinRequestAsync ---

        [Fact]
        public async Task SendPlayerJoinRequestAsync_PlayerAlreadyInAcademy_ThrowsConflictException()
        {
            var acadRepo = new Mock<IRepository<AcademyEntity>>();
            acadRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AcademyEntity, bool>>>()))
                .ReturnsAsync(new AcademyEntity { Id = 1 });
            _unitOfWorkMock.Setup(u => u.Repository<AcademyEntity>()).Returns(acadRepo.Object);

            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<PlayerEntity, bool>>>()))
                .ReturnsAsync(new PlayerEntity { Id = 1 });
            _unitOfWorkMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            var paRepo = new Mock<IRepository<PlayerAcademy>>();
            paRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<PlayerAcademy, bool>>>()))
                .ReturnsAsync(true);
            _unitOfWorkMock.Setup(u => u.Repository<PlayerAcademy>()).Returns(paRepo.Object);

            await Assert.ThrowsAsync<ConflictException>(() =>
                _service.SendPlayerJoinRequestAsync(1, 1, 100));
        }

        // --- RespondToPlayerJoinRequestAsync ---

        [Fact]
        public async Task RespondToPlayerJoinRequestAsync_RequestNotFound_ThrowsNotFoundException()
        {
            var reqRepo = new Mock<IRepository<AcademyPlayerJoinRequest>>();
            reqRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AcademyPlayerJoinRequest, bool>>>()))
                .ReturnsAsync((AcademyPlayerJoinRequest?)null);
            _unitOfWorkMock.Setup(u => u.Repository<AcademyPlayerJoinRequest>()).Returns(reqRepo.Object);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.RespondToPlayerJoinRequestAsync(1, JoinRequestStatus.Accepted, 1));
        }

        // --- CancelPlayerJoinRequestAsync ---

        [Fact]
        public async Task CancelPlayerJoinRequestAsync_NotPending_ThrowsBadRequestException()
        {
            var reqRepo = new Mock<IRepository<AcademyPlayerJoinRequest>>();
            reqRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AcademyPlayerJoinRequest, bool>>>()))
                .ReturnsAsync(new AcademyPlayerJoinRequest { Id = 1, Status = JoinRequestStatus.Accepted });
            _unitOfWorkMock.Setup(u => u.Repository<AcademyPlayerJoinRequest>()).Returns(reqRepo.Object);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.CancelPlayerJoinRequestAsync(1, 100));
        }
    }
}
