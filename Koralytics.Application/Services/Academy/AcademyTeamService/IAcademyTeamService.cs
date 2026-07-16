using Koralytics.Application.DTOs.Academies;

namespace Koralytics.Application.Services.Academy.AcademyTeamService
{
    public interface IAcademyTeamService
    {
        /// <summary>
        /// Creates an AgeGroup for the given academy.
        /// Validates that the min/max age range does not overlap with any existing group in the same academy.
        /// </summary>
        Task<AgeGroupResponseDto> CreateAgeGroupAsync(int academyId, CreateAgeGroupDto dto, int performedByUserId);

        /// <summary>
        /// Creates a Team under the given academy.
        /// Validates that the AgeGroup and Location both belong to this academy.
        /// </summary>
        Task<TeamResponseDto> CreateTeamAsync(int academyId, CreateTeamDto dto, int performedByUserId);

        /// <summary>
        /// Assigns a coach to a team by creating a CoachTeam record.
        /// Validates that the coach belongs to the same academy as the team.
        /// Prevents duplicate active assignments.
        /// </summary>
        Task<CoachTeamAssignmentDto> AssignCoachToTeamAsync(int coachUserId, int teamId, int performedByUserId);

        /// <summary>
        /// Soft-removes a coach from a team by setting CoachTeam.RemovedAt = UtcNow.
        /// </summary>
        Task RemoveCoachFromTeamAsync(int coachUserId, int teamId, int performedByUserId);

        /// <summary>Gets all teams for a given academy.</summary>
        Task<IEnumerable<TeamResponseDto>> GetTeamsByAcademyAsync(int academyId);

        /// <summary>Gets all age groups for a given academy.</summary>
        Task<IEnumerable<AgeGroupResponseDto>> GetAgeGroupsByAcademyAsync(int academyId);

        /// <summary>
        /// Assigns a player to a team by creating a PlayerTeam record.
        /// Validates that the player belongs to the same academy as the team.
        /// Prevents duplicate active assignments.
        /// </summary>
        Task AssignPlayerToTeamAsync(int playerUserId, int teamId, int performedByUserId);
        
        /// <summary>
        /// Soft-removes a player from a team by setting PlayerTeam.LeftAt = UtcNow.
        /// </summary>
        Task RemovePlayerFromTeamAsync(int playerUserId, int teamId, int performedByUserId);
    }
}
