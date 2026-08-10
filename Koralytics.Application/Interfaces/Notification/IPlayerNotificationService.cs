using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.Interfaces.Notification
{
    public interface IPlayerNotificationService
    {
        Task NotifyPlayerMilestoneAsync(int playerId, string message, CancellationToken cancellationToken = default);

        Task NotifyParentAsync(int playerId, string eventType, CancellationToken cancellationToken = default);

        Task NotifySubscriptionGraceAsync(int playerId, int academyId, CancellationToken cancellationToken = default);
        Task NotifyAcademySubscriptionPaidAsync(int playerId, int academyId, int currentUserId, string role, CancellationToken cancellationToken = default);
        Task NotifyParentsOfPlayersAsync(List<int> playerIds, string eventType, CancellationToken cancellationToken = default);
        Task NotifyMultiplePlayersAsync(List<int> playerIds, string message, CancellationToken cancellationToken = default);



    }
}
