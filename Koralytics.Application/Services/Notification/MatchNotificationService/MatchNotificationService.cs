using Koralytics.Application.DTOs.Notification;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Notification;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Parents;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Exceptions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Koralytics.Application.Services.Notification
{
    public class MatchNotificationService : IMatchNotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRealTimeBridge _realTimeBridge;

        public MatchNotificationService(IUnitOfWork unitOfWork, IRealTimeBridge realTimeBridge)
        {
            _unitOfWork = unitOfWork;
            _realTimeBridge = realTimeBridge;
        }

        public async Task NotifyMatchEventAsync(int matchId, string eventTitle, string eventMessage, string eventType, CancellationToken cancellationToken = default)
        {
            // 1. Retrieve the Match
            var match = await _unitOfWork.Repository<Domain.Entities.Match.Match>()
                .GetByIdAsync(matchId);

            if (match == null)
            {
                throw new NotFoundException($"Match with ID {matchId} not found.");
            }

            var targetUserIds = new HashSet<int>();
            var teamIds = new List<int> { match.HomeTeamId, match.AwayTeamId };

            // 2. Retrieve Academy Admins by fetching the Teams first
            foreach (var teamId in teamIds)
            {
                var team = await _unitOfWork.Repository<Team>().GetByIdAsync(teamId);

                if (team != null && team.AcademyId > 0)
                {
                    var academy = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>().GetByIdAsync(team.AcademyId);

                    if (academy != null && academy.AdminUserId > 0)
                    {
                        targetUserIds.Add(academy.AdminUserId);
                    }
                }
            }

            // 3. Retrieve all Players in the match
            // Note: Use FindAllAsync or GetAllAsync so it returns a collection, preventing CS1579
            var teamPlayers = await _unitOfWork.Repository<PlayerTeam>()
                .FindAllAsync(tp => teamIds.Contains(tp.TeamId));

            var playerIds = teamPlayers.Select(tp => tp.PlayerId).Distinct().ToList();

            foreach (var playerId in playerIds)
            {
                targetUserIds.Add(playerId);
            }

            // 4. Retrieve Parents of those Players
            if (playerIds.Any())
            {
                // Note: Use FindAllAsync to return a collection
                var playerParents = await _unitOfWork.Repository<ParentPlayer>()
                    .FindAllAsync(pp => playerIds.Contains(pp.PlayerId));

                // Note: Changed pp.ParentUserId to pp.ParentId to prevent CS0411
                var parentUserIds = playerParents.Select(pp => pp.ParentId).Distinct().ToList();

                foreach (var parentId in parentUserIds)
                {
                    targetUserIds.Add(parentId);
                }
            }

            // 5. Send Notification using the RealTimeBridge
            if (targetUserIds.Any())
            {
                var notification = new CachedNotification
                {
                    Title = eventTitle,
                    Content = eventMessage,
                    Type = eventType,
                    Payload = new { MatchId = matchId }
                };

                await _realTimeBridge.SendAndCacheToUsersAsync(
                    targetUserIds.ToList(),
                    "ReceiveMatchEventNotification",
                    notification,
                    cancellationToken);
            }
        }
        public async Task NotifyAcademyAsync(int academyId, string message, CancellationToken cancellationToken = default)
        {
            
            var academyExists = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                .ExistsAsync(a => a.Id == academyId);

            if (!academyExists)
            {
                throw new NotFoundException($"Academy with ID {academyId} does not exist.");
            }

          
            var notification = new CachedNotification
            {
                Title = "Academy Update", 
                Content = message,        
                Type = "AcademyNotification",
                Payload = new { AcademyId = academyId, Message = message }
            };

           
            string groupName = $"Academy_{academyId}_Admins";

            await _realTimeBridge.SendToGroupAsync(groupName, "ReceiveAcademyNotification", notification, cancellationToken);
        }
    }
}