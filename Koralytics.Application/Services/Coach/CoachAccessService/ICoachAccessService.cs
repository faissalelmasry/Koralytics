using Koralytics.Application.DTOs.Coach;

namespace Koralytics.Application.Services.Coach.CoachAccessService
{
    public interface ICoachAccessService
    {
        /// <summary>
        /// Grants temporary squad-view access to another user for a specified period.
        /// Validates that the grantee user exists before creating the record.
        /// </summary>
        Task<TempAccessDto> GrantTempAccessAsync(int coachId, GrantTempAccessDto dto);

        /// <summary>
        /// Revokes an existing temp-access grant.
        /// Only the owning coach may revoke their own grants.
        /// </summary>
        Task<TempAccessDto> RevokeTempAccessAsync(int coachId, int accessId);

        /// <summary>
        /// Returns all active (non-revoked, non-expired) access grants issued by this coach.
        /// </summary>
        Task<IEnumerable<TempAccessDto>> GetActiveGrantsAsync(int coachId);
    }
}
