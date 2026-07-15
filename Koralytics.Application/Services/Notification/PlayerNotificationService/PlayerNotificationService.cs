using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Notification;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.Services.Notification.PlayerNotificationService
{
    public class PlayerNotificationService : IPlayerNotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRealTimeBridge _realTimeBridge;
        private const string ClientMethod = "ReceiveNotification"; 

        public PlayerNotificationService(IUnitOfWork unitOfWork, IRealTimeBridge realTimeBridge)
        {
            _unitOfWork = unitOfWork;
            _realTimeBridge = realTimeBridge;
        }

        public async Task NotifyPlayerMilestoneAsync(int playerId, string achievementType)
        {
            var playerExists = await _unitOfWork.Repository<Domain.Entities.Player.Player>()
                .ExistsAsync(p => p.Id == playerId);

            if (!playerExists)
            {
                throw new NotFoundException($"Player with ID {playerId} does not exist.");
            }

            await _realTimeBridge.SendToGroupAsync(
                $"Player_{playerId}",
                "ReceiveMilestoneNotification",
                new { PlayerId = playerId, AchievementType = achievementType }
            );
        }

        public async Task NotifyParentAsync(int playerId, string eventType)
        {
            var parentRelations = await _unitOfWork.Repository<Domain.Entities.Parents.ParentPlayer>()
                .FindAllAsync(p => p.PlayerId == playerId);

            foreach (var relation in parentRelations)
            {
                await _realTimeBridge.SendToGroupAsync(
                    $"Parent_{relation.ParentId}",
                    "ReceiveParentNotification",
                    new { PlayerId = playerId, EventType = eventType }
                );
            }
        }

        public async Task NotifySubscriptionGraceAsync(int playerId, int academyId)
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

            await NotifyParentAsync(playerId, "SubscriptionGrace");
            await SendPlayerSubscriptionGraceInternalAsync(playerId, academyId);
        }
        private async Task SendPlayerSubscriptionGraceInternalAsync(int playerId, int academyId)
        {
            await _realTimeBridge.SendToGroupAsync(
                $"Player_{playerId}",
                "ReceiveSubscriptionGraceNotification",
                new { PlayerId = playerId, AcademyId = academyId }
            );
        }
    }
}