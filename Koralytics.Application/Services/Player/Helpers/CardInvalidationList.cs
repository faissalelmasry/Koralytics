using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Application.Interfaces;
using Koralytics.Domain.Entities.Player;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Koralytics.Application.Services.Player.Helpers
{
    public interface ICardInvalidationList
    {
        void Invalidate(int playerId);
        bool TryConsume(int playerId);
    }

    public class CardInvalidationList : ICardInvalidationList, IHostedService
    {
        private const string RedisSetKey = "CardInvalidationList:Pending";
        private const string RedisChannel = "CardInvalidationList:Events";

        private readonly ConcurrentDictionary<int, bool> _invalidated = new();
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CardInvalidationList> _logger;
        private readonly IDatabase? _redisDb;
        private readonly ISubscriber? _redisSubscriber;

        public CardInvalidationList(
            IServiceScopeFactory scopeFactory,
            ILogger<CardInvalidationList> logger,
            IConnectionMultiplexer? redis = null)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            if (redis != null && redis.IsConnected)
            {
                try
                {
                    _redisDb = redis.GetDatabase();
                    _redisSubscriber = redis.GetSubscriber();

                    _redisSubscriber.Subscribe(RedisChannel, (channel, value) =>
                    {
                        if (int.TryParse(value, out int playerId))
                        {
                            _invalidated.TryAdd(playerId, true);
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to initialize Redis for CardInvalidationList. Falling back to local memory and DB.");
                    _redisDb = null;
                    _redisSubscriber = null;
                }
            }
        }

        // Called by services — instant memory + Redis set + Pub/Sub
        public void Invalidate(int playerId)
        {
            _invalidated.TryAdd(playerId, true);

            if (_redisDb != null)
            {
                try
                {
                    _redisDb.SetAdd(RedisSetKey, playerId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Redis SetAdd failed for invalidating player {PlayerId}", playerId);
                }
            }

            if (_redisSubscriber != null)
            {
                try
                {
                    _redisSubscriber.Publish(RedisChannel, playerId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Redis Publish failed for invalidating player {PlayerId}", playerId);
                }
            }
        }

        public bool TryConsume(int playerId)
        {
            bool localRemoved = _invalidated.TryRemove(playerId, out _);
            bool redisRemoved = false;

            if (_redisDb != null)
            {
                try
                {
                    redisRemoved = _redisDb.SetRemove(RedisSetKey, playerId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Redis SetRemove failed for consuming player invalidation {PlayerId}", playerId);
                }
            }

            return localRemoved || redisRemoved;
        }

        // On startup — restore pending from DB and Redis
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider
                .GetRequiredService<IUnitOfWork>();

            var pendingIds = await unitOfWork.Repository<PlayerCard>()
                .GetQueryableAsNoTracking()
                .Where(pc => pc.NeedsRecalculation)
                .Select(pc => pc.PlayerId)
                .ToListAsync(cancellationToken);

            foreach (var playerId in pendingIds)
            {
                _invalidated.TryAdd(playerId, true);
            }

            int redisRestoredCount = 0;
            if (_redisDb != null)
            {
                try
                {
                    var redisMembers = await _redisDb.SetMembersAsync(RedisSetKey);
                    foreach (var member in redisMembers)
                    {
                        if (int.TryParse(member, out int playerId))
                        {
                            if (_invalidated.TryAdd(playerId, true))
                            {
                                redisRestoredCount++;
                            }
                        }
                    }

                    if (pendingIds.Count > 0)
                    {
                        var redisValues = pendingIds.Select(id => (RedisValue)id).ToArray();
                        await _redisDb.SetAddAsync(RedisSetKey, redisValues);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to synchronize Redis pending invalidations on startup.");
                }
            }

            _logger.LogInformation(
                "CardInvalidationList restored {DbCount} pending players from DB and {RedisCount} additional from Redis",
                pendingIds.Count,
                redisRestoredCount);
        }

        // On shutdown — persist pending from local memory + Redis to DB in one batch
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            var pendingIdsSet = new HashSet<int>(_invalidated.Keys);

            if (_redisDb != null)
            {
                try
                {
                    var redisMembers = await _redisDb.SetMembersAsync(RedisSetKey);
                    foreach (var member in redisMembers)
                    {
                        if (int.TryParse(member, out int playerId))
                        {
                            pendingIdsSet.Add(playerId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read Redis pending invalidations on shutdown.");
                }
            }

            if (pendingIdsSet.Count == 0) return;

            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider
                .GetRequiredService<IUnitOfWork>();

            var pendingIds = pendingIdsSet.ToList();

            var cards = await unitOfWork.Repository<PlayerCard>()
                .GetQueryable()
                .Where(pc => pendingIds.Contains(pc.PlayerId))
                .ToListAsync();

            foreach (var card in cards)
                card.NeedsRecalculation = true;

            await unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "CardInvalidationList persisted {Count} pending players to DB on shutdown",
                pendingIds.Count);
        }
    }
}
