using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.Interfaces.Notification
{
    public interface IMatchNotificationService
    {
        Task NotifyMatchEventAsync(int matchId, string eventTitle, string eventMessage, string eventType, CancellationToken cancellationToken = default);
        Task NotifyAcademyAsync(int academyId, string message, CancellationToken cancellationToken = default);
    }
}

