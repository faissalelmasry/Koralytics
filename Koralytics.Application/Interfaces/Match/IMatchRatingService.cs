using Koralytics.Application.DTOs.Match;

namespace Koralytics.Application.Interfaces.Match
{
    public interface IMatchRatingService
    {
        Task SubmitLineupAsync(int matchId, int coachId, SubmitLineupDto dto);
        Task<List<LineupResponseDto>> GetLineupAsync(int matchId);
        Task SubmitMatchRatingsAsync(int matchId, int coachId, SubmitMatchRatingsDto dto);
        Task<MatchRatingsResponseDto> GetMatchRatingsAsync(int matchId);
    }
}
