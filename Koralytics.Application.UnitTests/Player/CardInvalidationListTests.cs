using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Services.Player.Helpers;
using Koralytics.Domain.Entities.Player;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using MockQueryable;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace Koralytics.Application.UnitTests.Player
{
    public class CardInvalidationListTests
    {
        private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
        private readonly Mock<IServiceScope> _scopeMock;
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<CardInvalidationList>> _loggerMock;

        public CardInvalidationListTests()
        {
            _scopeFactoryMock = new Mock<IServiceScopeFactory>();
            _scopeMock = new Mock<IServiceScope>();
            _serviceProviderMock = new Mock<IServiceProvider>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<CardInvalidationList>>();

            _scopeFactoryMock.Setup(s => s.CreateScope()).Returns(_scopeMock.Object);
            _scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
            _serviceProviderMock.Setup(sp => sp.GetService(typeof(IUnitOfWork))).Returns(_unitOfWorkMock.Object);
        }

        [Fact]
        public void InvalidateAndTryConsume_WithoutRedis_WorksInMemory()
        {
            var invalidationList = new CardInvalidationList(_scopeFactoryMock.Object, _loggerMock.Object);

            invalidationList.Invalidate(42);

            Assert.True(invalidationList.TryConsume(42));
            Assert.False(invalidationList.TryConsume(42));
        }

        [Fact]
        public void InvalidateAndTryConsume_WithRedis_CallsRedisSetAndPublish()
        {
            var redisMock = new Mock<IConnectionMultiplexer>();
            var dbMock = new Mock<IDatabase>();
            var subMock = new Mock<ISubscriber>();

            redisMock.Setup(r => r.IsConnected).Returns(true);
            redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);
            redisMock.Setup(r => r.GetSubscriber(It.IsAny<object>())).Returns(subMock.Object);

            var invalidationList = new CardInvalidationList(_scopeFactoryMock.Object, _loggerMock.Object, redisMock.Object);

            invalidationList.Invalidate(100);

            dbMock.Verify(d => d.SetAdd(It.IsAny<RedisKey>(), 100, CommandFlags.None), Times.Once);
            subMock.Verify(s => s.Publish(It.IsAny<RedisChannel>(), 100, CommandFlags.None), Times.Once);

            dbMock.Setup(d => d.SetRemove(It.IsAny<RedisKey>(), 100, CommandFlags.None)).Returns(true);

            Assert.True(invalidationList.TryConsume(100));
        }

        [Fact]
        public async Task StartAsync_RestoresPendingPlayersFromDbAndRedis()
        {
            var pendingCards = new List<PlayerCard>
            {
                new PlayerCard { PlayerId = 1, NeedsRecalculation = true },
                new PlayerCard { PlayerId = 2, NeedsRecalculation = true }
            }.BuildMock();

            var repositoryMock = new Mock<IRepository<PlayerCard>>();
            repositoryMock.Setup(r => r.GetQueryableAsNoTracking()).Returns(pendingCards);
            _unitOfWorkMock.Setup(u => u.Repository<PlayerCard>()).Returns(repositoryMock.Object);

            var redisMock = new Mock<IConnectionMultiplexer>();
            var dbMock = new Mock<IDatabase>();
            var subMock = new Mock<ISubscriber>();

            redisMock.Setup(r => r.IsConnected).Returns(true);
            redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);
            redisMock.Setup(r => r.GetSubscriber(It.IsAny<object>())).Returns(subMock.Object);

            dbMock.Setup(d => d.SetMembersAsync(It.IsAny<RedisKey>(), CommandFlags.None))
                .ReturnsAsync(new RedisValue[] { "3" });

            var invalidationList = new CardInvalidationList(_scopeFactoryMock.Object, _loggerMock.Object, redisMock.Object);

            await invalidationList.StartAsync(CancellationToken.None);

            Assert.True(invalidationList.TryConsume(1));
            Assert.True(invalidationList.TryConsume(2));
            Assert.True(invalidationList.TryConsume(3));
        }
    }
}
