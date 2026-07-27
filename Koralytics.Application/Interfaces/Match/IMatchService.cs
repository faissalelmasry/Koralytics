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
        Task StartMatchAsync(int matchId);
        Task<MatchResponseDto> EndMatchAsync(int matchId);
        Task<FormGuideResponseDto> GetFormGuideAsync(int teamId, MatchFormat format);
        Task<MatchListResponseDto> GetMatchesByDateAsync(DateTime date, int page, int pageSize);
        Task<MatchListResponseDto> GetTeamMatchesByStatusAsync(int teamId, MatchStatus? status, int page, int pageSize);
        Task<CoachMatchesResponseDto> GetCoachMatchesAsync(int coachId, MatchStatus? status, Domain.Enums.MatchType? type, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize);
        Task<AcademyMatchesResponseDto> GetAcademyMatchesAsync(int academyId, int? teamId, int? ageGroupId, MatchStatus? status, Domain.Enums.MatchType? type, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize);
        Task CancelMatchAsync(int matchId);

    }
}
