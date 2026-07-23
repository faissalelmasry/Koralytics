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
        Task<Koralytics.Application.DTOs.Common.PagedResponseDto<AnnouncementResponseDto>> GetAnnouncementsAsync(int academyId, Koralytics.Application.DTOs.Common.PaginationRequestDto request);

    }
}
