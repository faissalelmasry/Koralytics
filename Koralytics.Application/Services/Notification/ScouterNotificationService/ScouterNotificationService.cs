using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Notification;
using Koralytics.Domain.Entities.Scouter;
using Koralytics.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        /// Notifies all scouters following a specific player when an engagement event occurs 
        /// (e.g., player posts a highlight, wins MOTM, or has an overall rating improvement).
        /// </summary>
        public async Task NotifyScouterFollowersAsync(int playerId, string eventType)
        {
           
            var playerExists = await _unitOfWork.Repository<Koralytics.Domain.Entities.Player.Player>()
                .ExistsAsync(p => p.Id == playerId);

            if (!playerExists)
            {
                throw new NotFoundException($"Player with ID {playerId} does not exist.");
            }

           
            var scouterFollows = await _unitOfWork.Repository<ScouterFollow>()
                .FindAllAsync(s => s.PlayerId == playerId);

            foreach (var follow in scouterFollows)
            {
                await _realTimeBridge.SendToGroupAsync(
                    $"Scouter_{follow.ScouterUserId}",
                    "ReceiveScouterNotification",
                    new { PlayerId = playerId, EventType = eventType }
                );
            }
        }
    }
}