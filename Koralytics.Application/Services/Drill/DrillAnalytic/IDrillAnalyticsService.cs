using Koralytics.Application.DTOs.Drill;
namespace Koralytics.Application.Services.Drill.DrillAnalytic
{
    public interface IDrillAnalyticsService
    {
        Task<IEnumerable<CategoryPerformanceDto>> GetSquadWeakCategoriesAsync(int teamId);
        Task<CoachBiasReportDto> DetectCoachBiasAsync(int targetCoachId, int academyId, int currentUserId, string currentUserRole);

    }
}
