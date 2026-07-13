using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Services.Player.PlayerCardService;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable.Moq;
using PlayerEntity = Koralytics.Domain.Entities.Player.Player;
using MockQueryable;
using Koralytics.Application.DTOs.Player;
using Koralytics.Domain.Enums;
using Koralytics.Application.Services.Player.Helpers;

namespace Koralytics.Application.UnitTests.Player
{
    public class PlayerCardServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<PlayerCardService>> _loggerMock;
        private readonly Mock<ICardInvalidationList> _invalidationListMock;

        private readonly PlayerCardService _service;

        public PlayerCardServiceTests()
        {
            _unitOfWorkMock = new();
            _mapperMock = new();
            _loggerMock = new();
            
            var scopeFactoryMock = new Mock<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>();
            var listLoggerMock = new Mock<ILogger<CardInvalidationList>>();
            _invalidationListMock = new Mock<CardInvalidationList>(scopeFactoryMock.Object, listLoggerMock.Object);
            _invalidationListMock = new Mock<ICardInvalidationList>();

            _service = new PlayerCardService(
                _unitOfWorkMock.Object,
                _loggerMock.Object,
                _mapperMock.Object,
                _invalidationListMock.Object);
        }
        [Fact]
        public async Task GetDrillToMatchTransferRateAsync_PlayerNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var playerRepo = new Mock<IRepository<PlayerEntity>>();

            playerRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<PlayerEntity, bool>>>()))
                .ReturnsAsync((PlayerEntity?)null);

            _unitOfWorkMock
                .Setup(u => u.Repository<PlayerEntity>())
                .Returns(playerRepo.Object);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.GetDrillToMatchTransferRateAsync(1));
        }
        [Fact]
        public async Task GetDrillToMatchTransferRateAsync_PlayerCardNotFound_ReturnsNull()
        {
            // Arrange
            var player = new PlayerEntity
            {
                Id = 1
            };

            var playerCards = new List<PlayerCard>();

            var playerCardQueryable = playerCards.BuildMock();

            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            var playerCardRepo = new Mock<IRepository<PlayerCard>>();

            playerRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<PlayerEntity, bool>>>()))
                .ReturnsAsync(player);

            playerCardRepo
                .Setup(r => r.GetQueryableAsNoTracking())
                .Returns(playerCardQueryable);

            _unitOfWorkMock
                .Setup(u => u.Repository<PlayerEntity>())
                .Returns(playerRepo.Object);

            _unitOfWorkMock
                .Setup(u => u.Repository<PlayerCard>())
                .Returns(playerCardRepo.Object);

            // Act
            var result = await _service.GetDrillToMatchTransferRateAsync(1);

            // Assert
            Assert.Null(result);
        }
        [Fact]
        public async Task GetDrillToMatchTransferRateAsync_PlayerCardExists_ReturnsTransferRateDto()
        {
            // Arrange
            var player = new PlayerEntity
            {
                Id = 1,
                FirstName = "Mohamed",
                LastName = "Salah"
            };

            var playerCard = new PlayerCard
            {
                PlayerId = 1,
                Player = player,
                OverallTrainingAvg = 90,
                OverallTournamentAvg = 80,
                TransferClassification = TransferClassification.Developing
            };

            var dto = new TransferRateDto
            {
                PlayerId = 1,
                PlayerName = "Mohamed Salah",
                OverallTrainingAvg = 90,
                OverallTournamentAvg = 80,
                TransferGap = 10,
                Classification = "Developing"
            };

            var playerCards = new List<PlayerCard>
    {
        playerCard
    };

            var queryable = playerCards.BuildMock();

            var playerRepo = new Mock<IRepository<PlayerEntity>>();
            var playerCardRepo = new Mock<IRepository<PlayerCard>>();

            playerRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<PlayerEntity, bool>>>()))
                .ReturnsAsync(player);

            playerCardRepo
                .Setup(r => r.GetQueryableAsNoTracking())
                .Returns(queryable);

            _mapperMock
                .Setup(m => m.Map<TransferRateDto>(playerCard))
                .Returns(dto);

            _unitOfWorkMock
                .Setup(u => u.Repository<PlayerEntity>())
                .Returns(playerRepo.Object);

            _unitOfWorkMock
                .Setup(u => u.Repository<PlayerCard>())
                .Returns(playerCardRepo.Object);

            // Act
            var result = await _service.GetDrillToMatchTransferRateAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.PlayerId);
            Assert.Equal("Mohamed Salah", result.PlayerName);
            Assert.Equal(10, result.TransferGap);
            Assert.Equal("Developing", result.Classification);
        }
    }
}
