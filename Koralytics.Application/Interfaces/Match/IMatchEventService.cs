using Koralytics.Application.DTOs.Match;

namespace Koralytics.Application.Interfaces.Match
{
    public interface IMatchEventService
    {
        Task<MatchEventResponseDto> LogMatchEventAsync(int matchId, LogMatchEventDto dto);
        Task<MatchEventResponseDto> LogSessionMatchEventAsync(int matchId, LogSessionMatchEventDto dto);
        Task<MatchTimelineResponseDto> GetMatchTimelineAsync(int matchId);
    }
}
