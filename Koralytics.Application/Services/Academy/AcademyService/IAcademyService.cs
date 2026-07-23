using Koralytics.Application.DTOs.Academies;
using Koralytics.Application.DTOs.Academy;
using Koralytics.Domain.Enums;
namespace Koralytics.Application.Services.Academy.AcademyService
{
    public interface IAcademyService
    {
        /// <summary>
        /// Creates an Academy record after SuperAdmin approves an AcademyRequest.
        /// Updates the AcademyRequest status to Approved.
        /// </summary>
        Task<AcademyResponseDto> ApproveAcademyAsync(CreateAcademyDto dto, int performedByUserId);

        /// <summary>
        /// Retrieves academy creation requests submitted by the current user.
        /// </summary>
        Task<IEnumerable<Koralytics.Application.DTOs.SystemAdmin.AcademyRequestResponseDto>> GetMyAcademyRequestsAsync(int userId);

        /// <summary>
        /// Updates mutable academy properties: Name, LogoUrl, PrimaryColor, SecondaryColor.
        /// </summary>
        Task<AcademyResponseDto> UpdateAcademyAsync(int academyId, UpdateAcademyDto dto, int performedByUserId);

        /// <summary>
        /// Adds a new physical location to the academy.
        /// Automatically sets IsMain = true when it is the first location added.
        /// </summary>
        Task<AcademyLocationResponseDto> AddLocationAsync(int academyId, AddLocationDto dto, int performedByUserId);

        /// <summary>Gets a single academy by id.</summary>
        Task<AcademyResponseDto> GetAcademyAsync(int academyId);

        /// <summary>Gets all academies with pagination and search.</summary>
        Task<AcademyListResponseDto> GetAllAcademiesAsync(AcademyListRequestDto request);

        Task<Koralytics.Application.DTOs.Common.PagedResponseDto<AcademyMemberResponseDto>> GetAcademyMembersAsync(int academyId, Koralytics.Application.DTOs.Common.PaginationRequestDto request);
        Task<Koralytics.Application.DTOs.Common.PagedResponseDto<AcademyAdminResponseDto>> GetAcademyAdminsAsync(int academyId, Koralytics.Application.DTOs.Common.PaginationRequestDto request);

        /// <summary>Gets all locations for an academy.</summary>
        Task<IEnumerable<AcademyLocationResponseDto>> GetLocationsAsync(int academyId);

        /// <summary>
        /// Promotes a location to main (sets it as IsMain=true, clears IsMain on all others).
        /// </summary>
        Task SetMainLocationAsync(int academyId, int locationId, int performedByUserId);

        // ─── Academy Requests ──────────────────────────────────────────────────
        Task<Koralytics.Application.DTOs.SystemAdmin.AcademyRequestResponseDto> RequestAcademyAsync(Koralytics.Application.DTOs.SystemAdmin.CreateAcademyRequestDto dto, int requestedByUserId);
        Task<IEnumerable<Koralytics.Application.DTOs.SystemAdmin.AcademyRequestResponseDto>> GetPendingRequestsAsync();
        Task RejectAcademyRequestAsync(int requestId, Koralytics.Application.DTOs.SystemAdmin.RejectAcademyRequestDto dto, int performedByUserId);

        // ─── Member Join Requests ──────────────────────────────────────────────
        Task AssignAdminToAcademyAsync(int academyId, int adminUserId, int performedByUserId);
        Task RemoveAdminFromAcademyAsync(int academyId, int adminUserId, int performedByUserId);
            
        Task<IEnumerable<PlayerSearchResponseDto>> SearchAvailablePlayersAsync(string? name, int academyId);
        Task<IEnumerable<CoachSearchResponseDto>> SearchCoachesAsync(string? name, int academyId);

        Task SendPlayerJoinRequestAsync(int academyId, int playerId, int adminUserId);
        Task SendCoachJoinRequestAsync(int academyId, int coachId, int adminUserId);

        Task RespondToPlayerJoinRequestAsync(int requestId, JoinRequestStatus status, int adminUserId);
        Task RespondToCoachJoinRequestAsync(int requestId, JoinRequestStatus status, int adminUserId);
        
        Task CancelPlayerJoinRequestAsync(int requestId, int adminUserId);
        Task CancelCoachJoinRequestAsync(int requestId, int adminUserId);

        Task<IEnumerable<AcademyPlayerJoinRequestResponseDto>> GetPendingPlayerRequestsForAcademyAsync(int academyId);
        Task<IEnumerable<AcademyCoachJoinRequestResponseDto>> GetPendingCoachRequestsForAcademyAsync(int academyId);

        Task<IEnumerable<AcademyPlayerJoinRequestResponseDto>> GetPendingPlayerRequestsForUserAsync(int playerId);
        Task<IEnumerable<AcademyCoachJoinRequestResponseDto>> GetPendingCoachRequestsForUserAsync(int coachId);

        // ─── Member Removal ────────────────────────────────────────────────────
        Task RemoveCoachFromAcademyAsync(int academyId, int coachUserId, int performedByUserId);
        Task RemovePlayerFromAcademyAsync(int academyId, int playerUserId, int performedByUserId);
        
        Task UpdatePlayerSubscriptionAsync(int academyId, int playerId, Koralytics.Application.DTOs.Academies.UpdatePlayerSubscriptionDto dto, int performedByUserId);
    }
}
