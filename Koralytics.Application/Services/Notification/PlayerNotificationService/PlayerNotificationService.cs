using Koralytics.Application.DTOs.Notification;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Notification;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Exceptions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Koralytics.Application.Services.Notification.PlayerNotificationService
{
    public class PlayerNotificationService : IPlayerNotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRealTimeBridge _realTimeBridge;

        public PlayerNotificationService(IUnitOfWork unitOfWork, IRealTimeBridge realTimeBridge)
        {
            _unitOfWork = unitOfWork;
            _realTimeBridge = realTimeBridge;
        }

        /// <summary>
        /// Notifies a player of an earned achievement, storing it directly in their notification cache.
        /// </summary>
        public async Task NotifyPlayerMilestoneAsync(int playerId, string message, CancellationToken cancellationToken = default)
        {
            var playerExists = await _unitOfWork.Repository<Domain.Entities.Player.Player>()
                .ExistsAsync(p => p.Id == playerId);

            if (!playerExists)
            {
                throw new NotFoundException($"Player with ID {playerId} does not exist.");
            }

            var notification = new CachedNotification
            {
                Title = "New Update", 
                Content = message,    
                Type = "PlayerNotification", 
                Payload = new { PlayerId = playerId, Message = message }
            };

          
            await _realTimeBridge.SendAndCacheToUserAsync(playerId, "ReceiveMilestoneNotification", notification, cancellationToken);
        }

        /// <summary>
        /// Notifies parents associated with a player regarding crucial updates.
        /// </summary>
        public async Task NotifyParentAsync(int playerId, string eventType, CancellationToken cancellationToken = default)
        {
            var parentRelations = await _unitOfWork.Repository<Domain.Entities.Parents.ParentPlayer>()
                .FindAllAsync(p => p.PlayerId == playerId);

            var parentIds = parentRelations.Select(r => r.ParentId).Distinct().ToList();
            if (parentIds.Count == 0) return;

            var notification = new CachedNotification
            {
                Title = "Parent Alert ",
                Content = $"There is an update regarding your child: {eventType}",
                Type = "ParentNotification",
                Payload = new { PlayerId = playerId, EventType = eventType }
            };

            
            await _realTimeBridge.SendAndCacheToUsersAsync(parentIds, "ReceiveParentNotification", notification, cancellationToken);
        }

        /// <summary>
        /// Sends grace period warnings to both parents and the player.
        /// </summary>
        public async Task NotifySubscriptionGraceAsync(int playerId, int academyId, CancellationToken cancellationToken = default)
        {
            var academyExists = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                .ExistsAsync(a => a.Id == academyId && !a.IsDeleted);

            if (!academyExists)
            {
                throw new NotFoundException($"Academy with ID {academyId} does not exist or is inactive.");
            }

            var playerExists = await _unitOfWork.Repository<Domain.Entities.Player.Player>()
                .ExistsAsync(p => p.Id == playerId);

            if (!playerExists)
            {
                throw new NotFoundException($"Player with ID {playerId} does not exist.");
            }

           
            var playerBelongsToAcademy = await _unitOfWork.Repository<PlayerAcademy>()
                .ExistsAsync(pa => pa.PlayerId == playerId && pa.AcademyId == academyId);

            if (!playerBelongsToAcademy)
            {
                throw new BadRequestException($"Player {playerId} is not enrolled in Academy {academyId}.");
            }

            
            await NotifyParentAsync(playerId, "SubscriptionGrace", cancellationToken);

           
            await SendPlayerSubscriptionGraceInternalAsync(playerId, academyId, cancellationToken);
        }

        private async Task SendPlayerSubscriptionGraceInternalAsync(int playerId, int academyId, CancellationToken cancellationToken)
        {
            var notification = new CachedNotification
            {
                Title = "Subscription Grace Period ",
                Content = "Your subscription is currently in its grace period. Please renew soon to keep full access.",
                Type = "SubscriptionGrace",
                Payload = new { PlayerId = playerId, AcademyId = academyId }
            };

            await _realTimeBridge.SendAndCacheToUserAsync(playerId, "ReceiveSubscriptionGraceNotification", notification, cancellationToken);
        }
        public async Task NotifyAcademySubscriptionPaidAsync(int playerId, int academyId, CancellationToken cancellationToken = default)
        { 
            var academy = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                .GetByIdAsync(academyId);

            if (academy == null || academy.IsDeleted)
            {
                throw new NotFoundException($"Academy with ID {academyId} does not exist or is inactive.");
            }

            // 2. Validate Player
            var player = await _unitOfWork.Repository<Domain.Entities.Player.Player>()
                .GetByIdAsync(playerId);

            if (player == null)
            {
                throw new NotFoundException($"Player with ID {playerId} does not exist.");
            }

            // 3. Validate Player belongs to Academy
            var playerBelongsToAcademy = await _unitOfWork.Repository<PlayerAcademy>()
                .ExistsAsync(pa => pa.PlayerId == playerId && pa.AcademyId == academyId);

            if (!playerBelongsToAcademy)
            {
                throw new BadRequestException($"Player {playerId} is not enrolled in Academy {academyId}.");
            }

          
            await SendAcademySubscriptionPaidInternalAsync(player, academy, cancellationToken);
        }

        private async Task SendAcademySubscriptionPaidInternalAsync(Domain.Entities.Player.Player player, Domain.Entities.Academy.Academy academy, CancellationToken cancellationToken)
        {
            var notification = new CachedNotification
            {
                Title = "New Subscription Payment",
                Content = $"A subscription payment has been successfully processed for player {player.FirstName} {player.LastName}.",
                Type = "SubscriptionPaid",
                Payload = new { PlayerId = player.Id, AcademyId = academy.Id }
            };
            await _realTimeBridge.SendAndCacheToUserAsync(academy.AdminUserId, "ReceiveSubscriptionPaidNotification", notification, cancellationToken);
        }
    }
}