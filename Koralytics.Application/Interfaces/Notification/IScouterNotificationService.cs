using Koralytics.Domain.Entities.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.Interfaces.Notification
{
    public interface IScouterNotificationService
    {
        Task NotifyScouterFollowersAsync(int playerId, string eventType, CancellationToken cancellationToken = default);

    }
}
