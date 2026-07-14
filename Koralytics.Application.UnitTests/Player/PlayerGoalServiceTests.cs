using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using Koralytics.Application.DTOs.Player;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Services.Player.PlayerGoalService;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using PlayerEntity = Koralytics.Domain.Entities.Player.Player;
using AcademyEntity = Koralytics.Domain.Entities.Academy.Academy;

namespace Koralytics.Application.UnitTests.Player
{
    public class PlayerGoalServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<PlayerGoalService>> _loggerMock;
        private readonly PlayerGoalService _service;

        public PlayerGoalServiceTests()
        {
            _unitOfWorkMock = new();
            _mapperMock = new();
            _loggerMock = new();
            _service = new PlayerGoalService(
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        // ================================================================
        // CreatePlayerGoalAsync
        // ================================================================

        [Fact]
        public async Task CreatePlayerGoalAsync_PlayerNotFound_ThrowsNotFoundException()
        {
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo
                .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<PlayerEntity, bool>>>()))
                .ReturnsAsync(false);
            _unitOfWorkMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            var dto = new CreatePlayerGoalDto
            {
                AcademyId = 1,
                Category = "Shooting",
                TargetScore = 90,
                Deadline = DateTime.UtcNow.AddDays(30)
            };

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.CreatePlayerGoalAsync(1, dto));
        }

        [Fact]
        public async Task CreatePlayerGoalAsync_AcademyNotFound_ThrowsNotFoundException()
        {
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo
                .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<PlayerEntity, bool>>>()))
                .ReturnsAsync(true);
            _unitOfWorkMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            academyRepo
                .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<AcademyEntity, bool>>>()))
                .ReturnsAsync(false);
            _unitOfWorkMock.Setup(u => u.Repository<AcademyEntity>()).Returns(academyRepo.Object);

            var dto = new CreatePlayerGoalDto
            {
                AcademyId = 999,
                Category = "Shooting",
                TargetScore = 90,
                Deadline = DateTime.UtcNow.AddDays(30)
            };

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.CreatePlayerGoalAsync(1, dto));
        }

        [Fact]
        public async Task CreatePlayerGoalAsync_ValidRequest_ReturnsPlayerGoalDto()
        {
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo
                .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<PlayerEntity, bool>>>()))
                .ReturnsAsync(true);
            _unitOfWorkMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            academyRepo
                .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<AcademyEntity, bool>>>()))
                .ReturnsAsync(true);
            _unitOfWorkMock.Setup(u => u.Repository<AcademyEntity>()).Returns(academyRepo.Object);

            var goalRepo = new Mock<IRepository<PlayerGoal>>();
            _unitOfWorkMock.Setup(u => u.Repository<PlayerGoal>()).Returns(goalRepo.Object);

            var deadline = DateTime.UtcNow.AddDays(30);
            var dto = new CreatePlayerGoalDto
            {
                AcademyId = 2,
                Category = "Passing",
                TargetScore = 85,
                Deadline = deadline
            };

            var mappedGoal = new PlayerGoal
            {
                Id = 10,
                PlayerId = 1,
                AcademyId = 2,
                Category = "Passing",
                TargetScore = 85,
                Deadline = deadline,
                Achieved = false
            };

            var expectedDto = new PlayerGoalDto
            {
                Id = 10,
                PlayerId = 1,
                AcademyId = 2,
                Category = "Passing",
                TargetScore = 85,
                Deadline = deadline,
                Achieved = false
            };

            _mapperMock
                .Setup(m => m.Map<PlayerGoal>(dto))
                .Returns(mappedGoal);

            _mapperMock
                .Setup(m => m.Map<PlayerGoalDto>(It.IsAny<PlayerGoal>()))
                .Returns(expectedDto);

            var result = await _service.CreatePlayerGoalAsync(1, dto);

            Assert.NotNull(result);
            Assert.Equal(expectedDto.Id, result.Id);
            Assert.Equal(expectedDto.PlayerId, result.PlayerId);
            Assert.Equal(expectedDto.AcademyId, result.AcademyId);
            Assert.Equal(expectedDto.Category, result.Category);
            Assert.Equal(expectedDto.TargetScore, result.TargetScore);
            Assert.Equal(expectedDto.Deadline, result.Deadline);
            Assert.False(result.Achieved);

            goalRepo.Verify(r => r.AddAsync(It.IsAny<PlayerGoal>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreatePlayerGoalAsync_SetsAchievedToFalse()
        {
            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            playerRepo
                .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<PlayerEntity, bool>>>()))
                .ReturnsAsync(true);
            _unitOfWorkMock.Setup(u => u.Repository<PlayerEntity>()).Returns(playerRepo.Object);

            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            academyRepo
                .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<AcademyEntity, bool>>>()))
                .ReturnsAsync(true);
            _unitOfWorkMock.Setup(u => u.Repository<AcademyEntity>()).Returns(academyRepo.Object);

            var goalRepo = new Mock<IRepository<PlayerGoal>>();
            _unitOfWorkMock.Setup(u => u.Repository<PlayerGoal>()).Returns(goalRepo.Object);

            var dto = new CreatePlayerGoalDto
            {
                AcademyId = 1,
                Category = "Dribbling",
                TargetScore = 70,
                Deadline = DateTime.UtcNow.AddDays(14)
            };

            var mappedGoal = new PlayerGoal
            {
                Id = 1,
                PlayerId = 1,
                AcademyId = 1,
                Category = "Dribbling",
                TargetScore = 70,
                Deadline = dto.Deadline,
                Achieved = true
            };

            _mapperMock
                .Setup(m => m.Map<PlayerGoal>(dto))
                .Returns(mappedGoal);

            _mapperMock
                .Setup(m => m.Map<PlayerGoalDto>(It.IsAny<PlayerGoal>()))
                .Returns(new PlayerGoalDto { Id = 1, PlayerId = 1, Achieved = false });

            var result = await _service.CreatePlayerGoalAsync(1, dto);

            Assert.False(result.Achieved);
            goalRepo.Verify(r => r.AddAsync(It.Is<PlayerGoal>(g => !g.Achieved)), Times.Once);
        }

        // ================================================================
        // UpdatePlayerGoalAsync
        // ================================================================

        [Fact]
        public async Task UpdatePlayerGoalAsync_GoalNotFound_ThrowsNotFoundException()
        {
            var goals = new List<PlayerGoal>();
            var queryable = goals.BuildMock();
            var goalRepo = new Mock<IRepository<PlayerGoal>>();
            goalRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            _unitOfWorkMock.Setup(u => u.Repository<PlayerGoal>()).Returns(goalRepo.Object);

            var dto = new UpdatePlayerGoalDto { Achieved = true };

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.UpdatePlayerGoalAsync(1, dto));
        }

        [Fact]
        public async Task UpdatePlayerGoalAsync_AchievedTrue_UpdatesAndReturnsDto()
        {
            var goal = new PlayerGoal
            {
                Id = 5,
                PlayerId = 1,
                AcademyId = 2,
                Category = "Shooting",
                TargetScore = 90,
                Deadline = DateTime.UtcNow.AddDays(7),
                Achieved = false
            };

            var goals = new List<PlayerGoal> { goal };
            var queryable = goals.BuildMock();
            var goalRepo = new Mock<IRepository<PlayerGoal>>();
            goalRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            _unitOfWorkMock.Setup(u => u.Repository<PlayerGoal>()).Returns(goalRepo.Object);

            var expectedDto = new PlayerGoalDto
            {
                Id = 5,
                PlayerId = 1,
                AcademyId = 2,
                Category = "Shooting",
                TargetScore = 90,
                Deadline = goal.Deadline,
                Achieved = true
            };

            _mapperMock
                .Setup(m => m.Map<PlayerGoalDto>(goal))
                .Returns(expectedDto);

            var dto = new UpdatePlayerGoalDto { Achieved = true };

            var result = await _service.UpdatePlayerGoalAsync(5, dto);

            Assert.True(result.Achieved);
            Assert.True(goal.Achieved);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdatePlayerGoalAsync_AchievedFalse_UpdatesAndReturnsDto()
        {
            var goal = new PlayerGoal
            {
                Id = 3,
                PlayerId = 1,
                AcademyId = 2,
                Category = "Defending",
                TargetScore = 80,
                Deadline = DateTime.UtcNow.AddDays(10),
                Achieved = true
            };

            var goals = new List<PlayerGoal> { goal };
            var queryable = goals.BuildMock();
            var goalRepo = new Mock<IRepository<PlayerGoal>>();
            goalRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            _unitOfWorkMock.Setup(u => u.Repository<PlayerGoal>()).Returns(goalRepo.Object);

            var expectedDto = new PlayerGoalDto
            {
                Id = 3,
                PlayerId = 1,
                AcademyId = 2,
                Category = "Defending",
                TargetScore = 80,
                Deadline = goal.Deadline,
                Achieved = false
            };

            _mapperMock
                .Setup(m => m.Map<PlayerGoalDto>(goal))
                .Returns(expectedDto);

            var dto = new UpdatePlayerGoalDto { Achieved = false };

            var result = await _service.UpdatePlayerGoalAsync(3, dto);

            Assert.False(result.Achieved);
            Assert.False(goal.Achieved);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
    }
}
