using Koralytics.API.Hubs;
using Koralytics.Application.Interfaces.Notification;
using Microsoft.AspNetCore.SignalR;

namespace Koralytics.API.Services
{
    public class RealTimeBridge : IRealTimeBridge
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public RealTimeBridge(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }
        public async Task SendToAllAsync(string method, object payload)
        {
            await _hubContext.Clients.All.SendAsync(method, payload);
        }

        public async Task SendToGroupAsync(string groupName, string method, object payload)
        {
            await _hubContext.Clients.Group(groupName).SendAsync(method, payload);
        }
    }
}
