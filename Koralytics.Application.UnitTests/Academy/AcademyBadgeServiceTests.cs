using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using Koralytics.Application.DTOs.Academies;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Services.Academy.AcademyBadgeService;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using AcademyEntity = Koralytics.Domain.Entities.Academy.Academy;

namespace Koralytics.Application.UnitTests.Academies
{
    public class AcademyBadgeServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<AcademyBadgeService>> _loggerMock;
        private readonly AcademyBadgeService _service;

        public AcademyBadgeServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<AcademyBadgeService>>();

            _service = new AcademyBadgeService(
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task CreateBadgeAsync_AcademyNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            academyRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AcademyEntity, bool>>>()))
                .ReturnsAsync((AcademyEntity?)null);

            _unitOfWorkMock.Setup(u => u.Repository<AcademyEntity>()).Returns(academyRepo.Object);

            var dto = new CreateAcademyBadgeDto { BadgeType = AcademyBadgeType.Verified };

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.CreateBadgeAsync(1, dto, 100));
        }

        [Fact]
        public async Task CreateBadgeAsync_BadgeAlreadyExists_ThrowsConflictException()
        {
            // Arrange
            var academy = new AcademyEntity { Id = 1 };
            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            academyRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AcademyEntity, bool>>>()))
                .ReturnsAsync(academy);

            var badgeRepo = new Mock<IRepository<AcademyBadge>>();
            badgeRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<AcademyBadge, bool>>>()))
                .ReturnsAsync(true);

            _unitOfWorkMock.Setup(u => u.Repository<AcademyEntity>()).Returns(academyRepo.Object);
            _unitOfWorkMock.Setup(u => u.Repository<AcademyBadge>()).Returns(badgeRepo.Object);

            var dto = new CreateAcademyBadgeDto { BadgeType = AcademyBadgeType.Verified };

            // Act & Assert
            await Assert.ThrowsAsync<ConflictException>(() =>
                _service.CreateBadgeAsync(1, dto, 100));
        }

        [Fact]
        public async Task CreateBadgeAsync_Success_ReturnsResponseDto()
        {
            // Arrange
            var academy = new AcademyEntity { Id = 1 };
            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            academyRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AcademyEntity, bool>>>()))
                .ReturnsAsync(academy);

            var badgeRepo = new Mock<IRepository<AcademyBadge>>();
            badgeRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<AcademyBadge, bool>>>()))
                .ReturnsAsync(false);
            
            badgeRepo.Setup(r => r.AddAsync(It.IsAny<AcademyBadge>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock.Setup(u => u.Repository<AcademyEntity>()).Returns(academyRepo.Object);
            _unitOfWorkMock.Setup(u => u.Repository<AcademyBadge>()).Returns(badgeRepo.Object);

            var dto = new CreateAcademyBadgeDto { BadgeType = AcademyBadgeType.Verified };
            var badgeEntity = new AcademyBadge { Id = 10, BadgeType = AcademyBadgeType.Verified, AcademyId = 1 };
            
            _mapperMock.Setup(m => m.Map<AcademyBadge>(dto)).Returns(badgeEntity);
            _mapperMock.Setup(m => m.Map<AcademyBadgeResponseDto>(It.IsAny<AcademyBadge>()))
                .Returns(new AcademyBadgeResponseDto { Id = 10, BadgeType = AcademyBadgeType.Verified });

            // Act
            var result = await _service.CreateBadgeAsync(1, dto, 100);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.Id);
            Assert.Equal(AcademyBadgeType.Verified, result.BadgeType);
            
            badgeRepo.Verify(r => r.AddAsync(It.IsAny<AcademyBadge>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetBadgesByAcademyAsync_AcademyNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            academyRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<AcademyEntity, bool>>>()))
                .ReturnsAsync(false);

            _unitOfWorkMock.Setup(u => u.Repository<AcademyEntity>()).Returns(academyRepo.Object);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.GetBadgesByAcademyAsync(1));
        }

        [Fact]
        public async Task GetBadgesByAcademyAsync_Success_ReturnsList()
        {
            // Arrange
            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            academyRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<AcademyEntity, bool>>>()))
                .ReturnsAsync(true);

            var badges = new List<AcademyBadge>
            {
                new AcademyBadge { Id = 1, BadgeType = AcademyBadgeType.Verified, AcademyId = 1 }
            };
            
            var badgeRepo = new Mock<IRepository<AcademyBadge>>();
            badgeRepo.Setup(r => r.FindAllAsNoTrackingAsync(It.IsAny<Expression<Func<AcademyBadge, bool>>>()))
                .ReturnsAsync(badges);

            _unitOfWorkMock.Setup(u => u.Repository<AcademyEntity>()).Returns(academyRepo.Object);
            _unitOfWorkMock.Setup(u => u.Repository<AcademyBadge>()).Returns(badgeRepo.Object);

            var dtos = new List<AcademyBadgeResponseDto>
            {
                new AcademyBadgeResponseDto { Id = 1, BadgeType = AcademyBadgeType.Verified }
            };
            _mapperMock.Setup(m => m.Map<IEnumerable<AcademyBadgeResponseDto>>(badges))
                .Returns(dtos);

            // Act
            var result = await _service.GetBadgesByAcademyAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result.First().Id);
        }

        [Fact]
        public async Task DeleteBadgeAsync_BadgeNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var badgeRepo = new Mock<IRepository<AcademyBadge>>();
            badgeRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AcademyBadge, bool>>>()))
                .ReturnsAsync((AcademyBadge?)null);

            _unitOfWorkMock.Setup(u => u.Repository<AcademyBadge>()).Returns(badgeRepo.Object);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.DeleteBadgeAsync(1, 100));
        }

        [Fact]
        public async Task DeleteBadgeAsync_Success_SoftDeletesBadge()
        {
            // Arrange
            var badge = new AcademyBadge { Id = 1, BadgeType = AcademyBadgeType.Verified, AcademyId = 1 };
            
            var badgeRepo = new Mock<IRepository<AcademyBadge>>();
            badgeRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AcademyBadge, bool>>>()))
                .ReturnsAsync(badge);

            _unitOfWorkMock.Setup(u => u.Repository<AcademyBadge>()).Returns(badgeRepo.Object);

            // Act
            await _service.DeleteBadgeAsync(1, 100);

            // Assert
            badgeRepo.Verify(r => r.SoftDelete(badge), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
    }
}
