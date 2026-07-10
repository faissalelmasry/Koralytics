using Koralytics.Application.DTOs.Drill;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.Services.Drill.DrillSession
{
    public interface IDrillSessionService
    {
        Task<DrillSessionDto> CreateSessionAsync(CreateDrillSessionDto dto, int currentCoachId, int currentAcademyId);
        Task<DrillDto> AddDrillToSessionAsync(int sessionId, AddSessionDrillDto dto, int currentCoachId);
        Task<IEnumerable<DrillSessionDto>> GetCoachSessionsAsync(int currentCoachId, int currentAcademyId, SessionFilterDto filter);
        Task<DrillSessionDetailsDto> GetSessionByIdAsync(int sessionId, int currentCoachId);
        Task<DrillSessionDto> UpdateSessionAsync(int sessionId, UpdateDrillSessionDto dto, int currentCoachId);
        Task RemoveDrillFromSessionAsync(int sessionId, int drillId, int currentCoachId);
        Task DeleteSessionAsync(int sessionId, int currentCoachId);
    }
}
