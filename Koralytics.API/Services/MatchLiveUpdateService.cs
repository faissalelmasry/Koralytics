using Koralytics.API.Hubs;
using Koralytics.Application.DTOs.Match;
using Koralytics.Application.Interfaces.Match;
using Microsoft.AspNetCore.SignalR;

namespace Koralytics.API.Services
{
    public class MatchLiveUpdateService : IMatchLiveUpdateService
    {
        private readonly IHubContext<MatchHub> _hubContext;

        public MatchLiveUpdateService(IHubContext<MatchHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task BroadcastMatchScoreUpdateAsync(LiveMatchScoreUpdateDto updateDto)
        {
            // Send to everyone (e.g. for the live matches list)
            await _hubContext.Clients.All.SendAsync("ReceiveMatchScoreUpdate", updateDto);
            
            // Also send to the specific match group (e.g. for the match detail page)
            await _hubContext.Clients.Group($"Match_{updateDto.MatchId}").SendAsync("ReceiveMatchScoreUpdate", updateDto);
        }

        public async Task BroadcastMatchEventAsync(LiveMatchEventUpdateDto eventDto)
        {
            // Send to the specific match group
            await _hubContext.Clients.Group($"Match_{eventDto.MatchId}").SendAsync("ReceiveMatchEventUpdate", eventDto);
        }
    }
}
