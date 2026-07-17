using Koralytics.Application.DTOs.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.Interfaces.Notification
{
    
        public interface IRealTimeBridge
        {

        Task SendToAllAsync(string method, object payload, CancellationToken cancellationToken = default);

        Task SendToGroupAsync(string groupName, string method, object payload, CancellationToken cancellationToken = default);

        /// <summary>
        /// Persists the notification to Redis and pushes it live to a single user via SignalR.
        /// Caching happens before the live push so the persisted feed and the live toast never disagree.
        /// </summary>
        Task SendAndCacheToUserAsync(int userId, string clientMethod, CachedNotification notification, CancellationToken cancellationToken = default);

        /// <summary>
        /// Fans the same notification out to many users with bounded concurrency.
        /// A failure for one user is logged and does not abort delivery to the rest of the batch.
        /// </summary>
        Task SendAndCacheToUsersAsync(IEnumerable<int> userIds, string clientMethod, CachedNotification notification, CancellationToken cancellationToken = default);

        Task<List<CachedNotification>> GetNotificationsAsync(int userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);

        Task MarkAsReadAsync(int userId, string notificationId, CancellationToken cancellationToken = default);

        Task DeleteExpiredNotificationsAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sweeps expired notifications for every user currently tracked in the notification cache.
        /// Intended to be invoked by a background/scheduled job, not per HTTP request.
        /// </summary>
        Task CleanupAllExpiredNotificationsAsync(CancellationToken cancellationToken = default);
    }
    }

