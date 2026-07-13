using Koralytics.Application.DTOs.Match;
using Koralytics.Domain.Enums;

namespace Koralytics.Application.Interfaces.Match
{
    public interface IMatchService
    {
        Task<MatchResponseDto> CreateFriendlyMatchAsync(CreateFriendlyMatchDto dto);
        Task<MatchResponseDto> CreateTournamentMatchAsync(CreateTournamentMatchDto dto);
        Task<MatchResponseDto> CreateSessionMatchAsync(CreateSessionMatchDto dto);
        Task<MatchResponseDto> GetMatchAsync(int matchId);
        Task<MatchResponseDto> StartMatchAsync(int matchId);
        Task<MatchResponseDto> EndMatchAsync(int matchId);
        Task<FormGuideResponseDto> GetFormGuideAsync(int teamId, MatchFormat format);
        Task<MatchListResponseDto> GetMatchesByDateAsync(DateTime date, int page, int pageSize);
        Task<MatchListResponseDto> GetTeamMatchesByStatusAsync(int teamId, MatchStatus? status, int page, int pageSize);
    }
}
