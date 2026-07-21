using Koralytics.Application.DTOs.Academies;
using Koralytics.Application.DTOs.Notification;

namespace Koralytics.Application.Services.Academy.AcademyAnnouncementService
{
    public interface IAcademyAnnouncementService
    {
        /// <summary>
        /// Creates an announcement for the academy and triggers notification delivery.
        /// Validates TargetType and ensures TargetId references an entity belonging to the academy.
        /// TODO: Trigger NotificationService.SendAnnouncementAsync() when implemented.
        /// </summary>
        Task<AnnouncementResponseDto> SendAnnouncementAsync(int academyId, CreateAnnouncementDto dto,  int sentByUserId, bool isSystemAdmin = false, CancellationToken cancellationToken = default);

        /// <summary>Gets all announcements for a given academy (newest first).</summary>
        Task<IEnumerable<AnnouncementResponseDto>> GetAnnouncementsAsync(int academyId);

        /// <summary>
        /// Removes a player from the academy by setting PlayerAcademy.LeftAt = UtcNow.
        /// Business rules:
        ///   - Player's subscription must be Unpaid or Grace with expired grace period.
        ///   - The requesting coach must currently coach the player's active team.
        /// Logs the action to RoleAuditLog.
        /// </summary>
        Task RemovePlayerAsync(int academyId, int playerId, int coachUserId, string reason);
    }
}
