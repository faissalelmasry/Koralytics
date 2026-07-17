using Koralytics.Application.DTOs.Notification;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Notification;
using Koralytics.Domain.Entities.Scouter;
using Koralytics.Domain.Exceptions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Koralytics.Application.Services.Notification.ScouterNotificationService
{
    public class ScouterNotificationService : IScouterNotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRealTimeBridge _realTimeBridge;

        public ScouterNotificationService(IUnitOfWork unitOfWork, IRealTimeBridge realTimeBridge)
        {
            _unitOfWork = unitOfWork;
            _realTimeBridge = realTimeBridge;
        }

        /// <summary>
        /// Notifies all scouters following a specific player when an engagement event occurs.
        /// Saves the notification to each scouter's Redis cache and broadcasts it.
        /// </summary>
        public async Task NotifyScouterFollowersAsync(int playerId, string eventType, CancellationToken cancellationToken = default)
        {
            var playerExists = await _unitOfWork.Repository<Domain.Entities.Player.Player>()
                .ExistsAsync(p => p.Id == playerId);

            if (!playerExists)
            {
                throw new NotFoundException($"Player with ID {playerId} does not exist.");
            }

            var scouterFollows = await _unitOfWork.Repository<ScouterFollow>()
                .FindAllAsync(s => s.PlayerId == playerId);

            var scouterUserIds = scouterFollows.Select(f => f.ScouterUserId).Distinct().ToList();
            if (scouterUserIds.Count == 0) return;

            var notification = new CachedNotification
            {
                Title = "Followed Player Activity",
                Content = $"The player you follow has triggered a new event: {eventType}",
                Type = "ScouterNotification",
                Payload = new { PlayerId = playerId, EventType = eventType }
            };

            
            await _realTimeBridge.SendAndCacheToUsersAsync(scouterUserIds, "ReceiveScouterNotification", notification, cancellationToken);
        }
    }
}