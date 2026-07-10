using Koralytics.Application.DTOs.Drill;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.Services.Drill.DrillAnalytic
{
    public interface IDrillAnalyticsService
    {
        Task<IEnumerable<CategoryPerformanceDto>> GetSquadWeakCategoriesAsync(int teamId);
        Task<CoachBiasReportDto> DetectCoachBiasAsync(int coachUserId, int academyId);

    }
}
