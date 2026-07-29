using Koralytics.Application.DTOs.Match;

namespace Koralytics.Application.Interfaces.Match
{
    public interface IMatchLiveUpdateService
    {
        Task BroadcastMatchScoreUpdateAsync(LiveMatchScoreUpdateDto updateDto);
        Task BroadcastMatchEventAsync(LiveMatchEventUpdateDto eventDto);
        Task BroadcastMatchEventDeletedAsync(LiveMatchEventDeletedDto deletedDto);
    }
}
