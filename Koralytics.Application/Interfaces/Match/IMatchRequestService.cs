using Koralytics.Application.DTOs.Match;

namespace Koralytics.Application.Interfaces.Match
{
    public interface IMatchRequestService
    {
        Task<MatchRequestResponseDto> RequestFriendlyMatchAsync(int coachId, CreateMatchRequestDto dto);
        Task<MatchResponseDto> AcceptMatchRequestAsync(int requestId, int coachId);
        Task DeclineMatchRequestAsync(int requestId, int coachId);
        Task<List<MatchRequestResponseDto>> GetPendingRequestsAsync(int teamId);
        Task<List<MatchRequestResponseDto>> GetSentRequestsAsync(int teamId);
    }
}
