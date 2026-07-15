using AutoMapper;

using Koralytics.Application.DTOs.Academies;
using Koralytics.Application.Interfaces;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Coach;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koralytics.Application.Services.Academy.AcademyAnnouncementService
{
    public class AcademyAnnouncementService : IAcademyAnnouncementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<AcademyAnnouncementService> _logger;

        public AcademyAnnouncementService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<AcademyAnnouncementService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        // ──────────────────────────────────────────────────────────────────────
        // SendAnnouncementAsync
        // Validates TargetType → ensures TargetId references an entity in the academy.
        // TODO: Trigger NotificationService.SendAnnouncementAsync() once implemented.
        // ──────────────────────────────────────────────────────────────────────
        public async Task<AnnouncementResponseDto> SendAnnouncementAsync(int academyId, SendAnnouncementDto dto, int sentByUserId)
        {
            _logger.LogInformation(
                "User {UserId} sending announcement to academy {AcademyId} — TargetType={TargetType}",
                sentByUserId, academyId, dto.TargetType);

            // Academy must exist
            _ = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                .FindAsync(a => a.Id == academyId && !a.IsDeleted)
                ?? throw new NotFoundException($"Academy with Id {academyId} not found.");

            // Validate TargetId references an entity within this academy
            await ValidateTargetAsync(academyId, dto.TargetType, dto.TargetId);

            var announcement = new AcademyAnnouncement
            {
                AcademyId = academyId,
                SentByUserId = sentByUserId,
                Title = dto.Title,
                Body = dto.Body,
                TargetType = dto.TargetType,
                TargetId = dto.TargetId,
                CreatedById = sentByUserId
            };

            await _unitOfWork.Repository<AcademyAnnouncement>().AddAsync(announcement);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Announcement '{Title}' (Id={Id}) created for academy {AcademyId}.",
                announcement.Title, announcement.Id, academyId);

            // TODO: Call NotificationService.SendAnnouncementAsync(announcement) when the notification module is implemented.
            // NotificationService should fan-out push/email notifications to all recipients determined by TargetType .
             

            // Build response (fetch sender name)
            var sender = await _unitOfWork.Repository<Domain.Entities.Identity.User>()
                .FindAsNoTrackingAsync(u => u.Id == sentByUserId);

            return new AnnouncementResponseDto
            {
                Id = announcement.Id,
                AcademyId = academyId,
                Title = announcement.Title,
                Body = announcement.Body,
                TargetType = announcement.TargetType,
                TargetId = announcement.TargetId,
                SentByUserId = sentByUserId,
                SentByFullName = sender is not null
                    ? $"{sender.FirstName} {sender.LastName}"
                    : string.Empty,
                CreatedAt = announcement.CreatedAt
            };
        }

        // ──────────────────────────────────────────────────────────────────────
        // GetAnnouncementsAsync
        // ──────────────────────────────────────────────────────────────────────
        public async Task<IEnumerable<AnnouncementResponseDto>> GetAnnouncementsAsync(int academyId)
        {
            _ = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                .FindAsNoTrackingAsync(a => a.Id == academyId)
                ?? throw new NotFoundException($"Academy with Id {academyId} not found.");

            var announcements = await _unitOfWork.Repository<AcademyAnnouncement>()
                .GetQueryableAsNoTracking()
                .Include(a => a.Academy)
                .Where(a => a.AcademyId == academyId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            // Fetch sender names in a single query
            var senderIds = announcements.Select(a => a.SentByUserId).Distinct().ToList();
            var senders = await _unitOfWork.Repository<Domain.Entities.Identity.User>()
                .FindAllAsNoTrackingAsync(u => senderIds.Contains(u.Id));
            var senderMap = senders.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");

            return announcements.Select(a => new AnnouncementResponseDto
            {
                Id = a.Id,
                AcademyId = a.AcademyId,
                Title = a.Title,
                Body = a.Body,
                TargetType = a.TargetType,
                TargetId = a.TargetId,
                SentByUserId = a.SentByUserId,
                SentByFullName = senderMap.TryGetValue(a.SentByUserId, out var name) ? name : string.Empty,
                CreatedAt = a.CreatedAt
            }).ToList();
        }

        // ──────────────────────────────────────────────────────────────────────
        // RemovePlayerAsync
        // Business rules:
        //   1. Player must be in the academy (active PlayerAcademy record).
        //   2. Player's latest subscription must be Unpaid/Grace AND grace period expired.
        //   3. Requesting coach must currently coach the player's active team.
        //   4. Sets PlayerAcademy.LeftAt = now.
        //   5. Logs to RoleAuditLog.
        // ──────────────────────────────────────────────────────────────────────
        public async Task RemovePlayerAsync(int academyId, int playerId, int coachUserId, string reason)
        {
            _logger.LogInformation(
                "Coach {CoachId} requesting removal of player {PlayerId} from academy {AcademyId}",
                coachUserId, playerId, academyId);

            // Academy must exist
            _ = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                .FindAsync(a => a.Id == academyId)
                ?? throw new NotFoundException($"Academy with Id {academyId} not found.");

            // Player must be actively enrolled in this academy
            var playerAcademy = await _unitOfWork.Repository<PlayerAcademy>()
                .FindAsync(pa => pa.PlayerId == playerId && pa.AcademyId == academyId && pa.LeftAt == null);
                                 

            if (playerAcademy is null)
                throw new NotFoundException(
                    $"Player {playerId} is not actively enrolled in academy {academyId}.");

            // Validate subscription: must be Unpaid or Grace with expired grace period
            var latestSubscription = await _unitOfWork.Repository<PlayerSubscription>()
                .GetQueryableAsNoTracking()
                .Where(ps => ps.PlayerId == playerId && ps.AcademyId == academyId )
                .OrderByDescending(ps => ps.Id)
                .FirstOrDefaultAsync();

            if (latestSubscription is null)
                throw new BadRequestException(
                    "Cannot remove player: no subscription record found.");

            var now = DateTime.UtcNow;

            var canRemove = latestSubscription.Status == SubscriptionStatus.Unpaid ||
                            (latestSubscription.Status == SubscriptionStatus.Grace &&
                             latestSubscription.GraceUntil.HasValue &&
                             latestSubscription.GraceUntil < now);

            if (!canRemove)
                throw new BadRequestException(
                    "Player can only be removed if their subscription is Unpaid, " +
                    "or they are in Grace status with an expired grace period.");

            // Validate requesting coach is actively coaching this player's team
            var playerTeam = await _unitOfWork.Repository<PlayerTeam>()
                .FindAsync(pt => pt.PlayerId == playerId && pt.LeftAt == null );

            if (playerTeam is null)
                throw new BadRequestException(
                    "Cannot remove player: player is not assigned to any active team.");

            // Verify the coach coaches that specific team
            var isCoachOfTeam = await _unitOfWork.Repository<CoachTeam>()
                .ExistsAsync(ct => ct.CoachUserId == coachUserId && ct.TeamId == playerTeam.TeamId && ct.RemovedAt == null );

            if (!isCoachOfTeam)
                throw new ForbiddenException(
                    "Only a coach of the player's active team can remove the player.");

            // Soft-remove: set LeftAt
            playerAcademy.LeftAt = now;
            _unitOfWork.Repository<PlayerAcademy>().SoftDelete(playerAcademy);

            // Log to RoleAuditLog
            var auditLog = new RoleAuditLog
            {
                AcademyId = academyId,
                PerformedByUserId = coachUserId,
                AffectedUserId = playerId,
                Action = RoleAuditAction.Removed,
                Details = $"Player removed from academy. Reason: {reason}",
                CreatedById = coachUserId
            };

            await _unitOfWork.Repository<RoleAuditLog>().AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Player {PlayerId} removed from academy {AcademyId} by coach {CoachId}.",
                playerId, academyId, coachUserId);
        }

        // ──────────────────────────────────────────────────────────────────────
        // Private helper: validates TargetId references an entity in this academy
        // ──────────────────────────────────────────────────────────────────────
        private async Task ValidateTargetAsync( int academyId, AnnouncementTargetType targetType, int targetId)
        {
            switch (targetType)
            {
                case AnnouncementTargetType.All:
                    // No entity to validate
                    break;

                case AnnouncementTargetType.Team:
                    var teamExists = await _unitOfWork.Repository<Team>()
                        .ExistsAsync(t => t.Id == targetId && t.AcademyId == academyId );
                    if (!teamExists)
                        throw new NotFoundException(
                            $"Team {targetId} not found in academy {academyId}.");
                    break;

                case AnnouncementTargetType.AgeGroup:
                    var ageGroupExists = await _unitOfWork.Repository<AgeGroup>()
                        .ExistsAsync(ag => ag.Id == targetId && ag.AcademyId == academyId );
                    if (!ageGroupExists)
                        throw new NotFoundException(
                            $"AgeGroup {targetId} not found in academy {academyId}.");
                    break;

                case AnnouncementTargetType.Role:
                    // TODO: Validate that the Role (SystemAdmin, Coach, etc.) is a valid role for the academy
                    /*  1:Scouter
                        2:SystemAdmin
                        3:AcademyAdmin
                        4:Player
                        5:Parent
                        6:Coach
                    */
                    if (targetId <= 0 || targetId > 6)
                        throw new BadRequestException(
                            "TargetId must be a valid positive role id between 1 and 6.");
                    break;

                default:
                    throw new BadRequestException($"Unknown TargetType: {targetType}.");
            }
        }
    }
}
