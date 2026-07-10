using Koralytics.Application.DTOs.Player;

namespace Koralytics.Application.Services.Player.PlayerProfileServices
{
    public interface IPlayerProfileService
    {
        Task<PlayerProfileDto> GetPlayerProfileAsync(int playerId);
        Task<DrillTimelineDto> GetDrillTimelineAsync(int playerId, int page = 1, int pageSize = 20);
        Task<MatchTimelineDto> GetMatchTimelineAsync(int playerId, int page = 1, int pageSize = 20);
        Task<AchievementTimelineDto> GetAchievementTimelineAsync(int playerId, int page = 1, int pageSize = 20);
        Task<PlayerVsAcademyAverageDto> GetPlayerVsAcademyAverageAsync(
            int playerId, int academyId);
        Task<ScouterViewsCountDto> GetScouterViewsCountAsync(
            int playerId, int year, int month);
    }
}
