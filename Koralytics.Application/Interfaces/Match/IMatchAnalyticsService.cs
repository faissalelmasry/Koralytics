using Koralytics.Application.DTOs.Match;

namespace Koralytics.Application.Interfaces.Match
{
    public interface IMatchAnalyticsService
    {
        Task<HeadToHeadResponseDto> GetHeadToHeadAsync(int teamAId, int teamBId);
        Task<PostMatchAnalysisResponseDto> GetPostMatchAnalysisAsync(int teamId);
        Task<PlayerReadinessDto> GetPlayerReadinessAsync(int playerId);
    }
}
