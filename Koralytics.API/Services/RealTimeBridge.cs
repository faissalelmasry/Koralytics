using Koralytics.API.Hubs;
using Koralytics.Application.DTOs.Notification;
using Koralytics.Application.Interfaces.Notification;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Koralytics.API.Services
{
    public class RealTimeBridge : IRealTimeBridge
    {
        // Cap on simultaneous SignalR sends / Redis writes during a fan-out.
        // Keeps a large academy announcement from opening thousands of concurrent
        // connections/commands at once.
        private const int MaxFanOutConcurrency = 20;

        // Global index of every user we've ever cached a notification for, so the
        // background cleanup job can sweep expired entries without needing an
        // external list of "all user ids".
        private const string KnownUsersIndexKey = "Notifications:KnownUsers";

        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IDatabase _redisDb;
        private readonly ILogger<RealTimeBridge> _logger;

        public RealTimeBridge(IHubContext<NotificationHub> hubContext, IServiceProvider serviceProvider, ILogger<RealTimeBridge> logger)
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var redis = serviceProvider.GetService(typeof(IConnectionMultiplexer)) as IConnectionMultiplexer;
            if (redis == null)
            {
                
                throw new InvalidOperationException(
                    "RealTimeBridge requires a registered IConnectionMultiplexer. " +
                    "Verify Redis is configured and registered in DI.");
            }

            try
            {
                _redisDb = redis.GetDatabase();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("RealTimeBridge could not resolve a Redis database from the configured connection.", ex);
            }
        }

        public async Task SendToAllAsync(string method, object payload, CancellationToken cancellationToken = default)
        {
            await _hubContext.Clients.All.SendAsync(method, payload, cancellationToken);
        }

        public async Task SendToGroupAsync(string groupName, string method, object payload, CancellationToken cancellationToken = default)
        {
            await _hubContext.Clients.Group(groupName).SendAsync(method, payload, cancellationToken);
        }

        public async Task SendAndCacheToUserAsync(int userId, string clientMethod, CachedNotification notification, CancellationToken cancellationToken = default)
        {
           
            var jsonPayload = JsonSerializer.Serialize(notification);

            string hashKey = $"User:{userId}:Notifications";
            string zsetKey = $"User:{userId}:Notifications:Expired";
            double score = new DateTimeOffset(notification.SentAt).ToUnixTimeSeconds();

            await _redisDb.HashSetAsync(hashKey, notification.Id, jsonPayload);
            await _redisDb.SortedSetAddAsync(zsetKey, notification.Id, score);
            await _redisDb.SetAddAsync(KnownUsersIndexKey, userId);

            await _hubContext.Clients.Group($"User_{userId}").SendAsync(clientMethod, notification, cancellationToken);
        }

        public async Task SendAndCacheToUsersAsync(IEnumerable<int> userIds, string clientMethod, CachedNotification notification, CancellationToken cancellationToken = default)
        {
            if (userIds == null) return;

            var distinctIds = userIds.Distinct().ToList();
            if (distinctIds.Count == 0) return;

            using var throttle = new SemaphoreSlim(MaxFanOutConcurrency);
            var failures = new List<(int UserId, Exception Error)>();
            var failuresLock = new object();

            var tasks = distinctIds.Select(async userId =>
            {
                await throttle.WaitAsync(cancellationToken);
                try
                {
                    await SendAndCacheToUserAsync(userId, clientMethod, notification, cancellationToken);
                }
                catch (Exception ex)
                {
                    lock (failuresLock)
                    {
                        failures.Add((userId, ex));
                    }
                }
                finally
                {
                    throttle.Release();
                }
            });

            await Task.WhenAll(tasks);

            foreach (var failure in failures)
            {
                _logger.LogWarning(failure.Error,
                    "Failed to deliver notification {ClientMethod} to user {UserId}",
                    clientMethod, failure.UserId);
            }
        }

        public async Task<List<CachedNotification>> GetNotificationsAsync(int userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
        {
            if (skip < 0) skip = 0;
            if (take <= 0) take = 50;

            string hashKey = $"User:{userId}:Notifications";
            string zsetKey = $"User:{userId}:Notifications:Expired";

            
            var notificationIds = await _redisDb.SortedSetRangeByRankAsync(zsetKey, skip, skip + take - 1, Order.Descending);

            if (notificationIds.Length == 0)
                return new List<CachedNotification>();

            var hashFields = notificationIds.Select(id => (RedisValue)id).ToArray();
            var notificationsJson = await _redisDb.HashGetAsync(hashKey, hashFields);

            var list = new List<CachedNotification>();
            foreach (var json in notificationsJson)
            {
                if (json.HasValue)
                {
                    var notif = JsonSerializer.Deserialize<CachedNotification>(json!);
                    if (notif != null)
                        list.Add(notif);
                }
            }
            return list;
        }

        public async Task MarkAsReadAsync(int userId, string notificationId, CancellationToken cancellationToken = default)
        {
            string hashKey = $"User:{userId}:Notifications";

            var json = await _redisDb.HashGetAsync(hashKey, notificationId);
            if (!json.HasValue) return;

            var notification = JsonSerializer.Deserialize<CachedNotification>(json!);
            if (notification != null)
            {
                notification.IsRead = true;
                var updatedJson = JsonSerializer.Serialize(notification);
                await _redisDb.HashSetAsync(hashKey, notificationId, updatedJson);
            }
        }

        public async Task DeleteExpiredNotificationsAsync(int userId, CancellationToken cancellationToken = default)
        {
            string hashKey = $"User:{userId}:Notifications";
            string zsetKey = $"User:{userId}:Notifications:Expired";

            double threshold = DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds();

            var expiredIds = await _redisDb.SortedSetRangeByScoreAsync(zsetKey, double.NegativeInfinity, threshold);

            if (expiredIds.Length > 0)
            {
                var fieldsToDelete = expiredIds.Select(id => (RedisValue)id).ToArray();

                await _redisDb.HashDeleteAsync(hashKey, fieldsToDelete);
                await _redisDb.SortedSetRemoveRangeByScoreAsync(zsetKey, double.NegativeInfinity, threshold);
            }

           
            if (!await _redisDb.KeyExistsAsync(hashKey))
            {
                await _redisDb.SetRemoveAsync(KnownUsersIndexKey, userId);
            }
        }

        public async Task CleanupAllExpiredNotificationsAsync(CancellationToken cancellationToken = default)
        {
            var knownUsers = await _redisDb.SetMembersAsync(KnownUsersIndexKey);

            foreach (var userIdValue in knownUsers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!int.TryParse(userIdValue.ToString(), out var userId))
                    continue;

                try
                {
                    await DeleteExpiredNotificationsAsync(userId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to purge expired notifications for user {UserId} during sweep", userId);
                }
            }
        }
    }
}