using Koralytics.Application.DTOs.Drill;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.Services.Drill.DrillResult
{
    public interface IDrillResultService
    {
        Task SubmitResultsAsync(int sessionId, int drillId, SubmitDrillResultsDto dto, int currentCoachId);
        Task MarkAttendanceAsync(int sessionId, UpdateSessionAttendanceDto dto, int currentCoachId);
        Task<PlayerProgressionDto> GetPlayerDrillProgressionAsync(int playerId, int categoryId, int currentAcademyId);
        Task<IEnumerable<DrillResultDto>> GetDrillResultsAsync(int sessionId, int drillId, int currentCoachId);
        Task<IEnumerable<PlayerAttendanceDto>> GetSessionAttendanceAsync(int sessionId, int currentCoachId);

    }
}
