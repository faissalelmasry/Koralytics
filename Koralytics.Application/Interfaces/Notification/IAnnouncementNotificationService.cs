using Koralytics.Application.DTOs.Notification;
using Koralytics.Domain.Entities.Academy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.Interfaces.Notification
{
    public interface IAnnouncementNotificationService
    {
        Task SendAnnouncementNotificationAsync(int academyId, int userId, CreateAnnouncementDto body, bool isSystemAdmin = false, CancellationToken cancellationToken = default);
    }
}
