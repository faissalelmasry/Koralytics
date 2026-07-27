using Microsoft.AspNetCore.SignalR;

namespace Koralytics.API.Hubs
{
    public class MatchHub : Hub
    {
        public async Task JoinLiveMatch(string matchId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Match_{matchId}");
        }

        public async Task LeaveLiveMatch(string matchId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Match_{matchId}");
        }
    }
}
