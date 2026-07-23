using Koralytics.Application.DTOs.Player;

namespace Koralytics.Application.Services.Player.PlayerProfileServices
{
    public interface IPlayerProfileService
    {
        Task<PlayerProfileDto> GetPlayerProfileAsync(int playerId);
        Task<DrillTimelineDto> GetDrillTimelineAsync(int playerId, int page = 1, int pageSize = 20,
            DateTime? dateFrom = null, DateTime? dateTo = null);
        Task<MatchTimelineDto> GetMatchTimelineAsync(int playerId, int page = 1, int pageSize = 20,
            string? matchType = null, DateTime? dateFrom = null, DateTime? dateTo = null);
        Task<AchievementTimelineDto> GetAchievementTimelineAsync(int playerId, int page = 1, int pageSize = 20);
        Task<PlayerVsAcademyAverageDto> GetPlayerVsAcademyAverageAsync(
            int playerId, int academyId);
        Task<ScouterViewsCountDto> GetScouterViewsCountAsync(
            int playerId, int year, int month);
        Task<TeamScheduledEventsResponseDto> GetTeamScheduledEventsAsync(int playerId, int page = 1, int pageSize = 20,
            string? eventType = null, DateTime? dateFrom = null, DateTime? dateTo = null);
    }
}
