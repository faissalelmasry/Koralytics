using Koralytics.Application.DTOs.Match;

namespace Koralytics.Application.Interfaces.Match
{
    public interface IMatchRequestService
    {
        Task<MatchRequestResponseDto> RequestFriendlyMatchAsync(int coachId, CreateMatchRequestDto dto);
        Task<MatchResponseDto> AcceptMatchRequestAsync(int requestId, int coachId);
        Task DeclineMatchRequestAsync(int requestId, int coachId);
        Task<MatchRequestListResponseDto> GetPendingRequestsAsync(
            int teamId,
            int page = 1,
            int pageSize = 20,
            string? status = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null);
        Task<MatchRequestListResponseDto> GetSentRequestsAsync(
            int teamId,
            int page = 1,
            int pageSize = 20,
            string? status = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null);
    }
}
