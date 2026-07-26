using Koralytics.Application.DTOs.Drill;
namespace Koralytics.Application.Services.Drill.DrillSession
{
    public interface IDrillSessionService
    {
        Task<DrillSessionDto> CreateSessionAsync(CreateDrillSessionDto dto, int currentCoachId, int currentAcademyId);
        Task<DrillDto> AddDrillToSessionAsync(int sessionId, AddSessionDrillDto dto, int currentCoachId);
        Task<Koralytics.Application.Common.PagedResult<DrillSessionDto>> GetCoachSessionsAsync(int currentUserId, string currentUserRole, int currentAcademyId, SessionFilterDto filter);
        
        Task<DrillSessionDetailsDto> GetSessionByIdAsync(int sessionId, int currentUserId, string currentUserRole, int currentAcademyId);
        Task<DrillSessionDto> UpdateSessionAsync(int sessionId, UpdateDrillSessionDto dto, int currentCoachId);
        Task RemoveDrillFromSessionAsync(int sessionId, int drillId, int currentCoachId);
        Task DeleteSessionAsync(int sessionId, int currentCoachId);
        Task CompleteSessionAsync(int sessionId, int currentCoachId);

    }
}
