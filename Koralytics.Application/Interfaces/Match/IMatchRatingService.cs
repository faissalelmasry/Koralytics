using Koralytics.Application.DTOs.Match;

namespace Koralytics.Application.Interfaces.Match
{
    public interface IMatchRatingService
    {
        Task<List<LineupResponseDto>> SubmitLineupAsync(int matchId, int coachId, SubmitLineupDto dto);
        Task<List<LineupResponseDto>> GetLineupAsync(int matchId);
        Task<MatchResponseDto> SubmitMatchRatingsAsync(int matchId, int coachId, SubmitMatchRatingsDto dto);
    }
}
