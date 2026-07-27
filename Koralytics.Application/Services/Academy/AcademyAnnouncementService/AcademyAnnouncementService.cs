using AutoMapper;

using Koralytics.Application.DTOs.Academies;
using Koralytics.Application.DTOs.Notification;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Notification;
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
        private readonly IAnnouncementNotificationService _announcementNotificationService;

        public AcademyAnnouncementService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<AcademyAnnouncementService> logger,
            IAnnouncementNotificationService announcementNotificationService
            )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _announcementNotificationService = announcementNotificationService;
        }

        // ──────────────────────────────────────────────────────────────────────
        // SendAnnouncementAsync
        // Validates TargetType → ensures TargetId references an entity in the academy.
        // ──────────────────────────────────────────────────────────────────────
        public async Task<AnnouncementResponseDto> SendAnnouncementAsync(int academyId,CreateAnnouncementDto dto, int sentByUserId,bool isSystemAdmin = false,CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "User {UserId} sending announcement to academy {AcademyId} — TargetType={TargetType}",
                sentByUserId, academyId, dto.TargetType);

            // Academy must exist
            _ = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                .FindAsync(a => a.Id == academyId && !a.IsDeleted)
                ?? throw new NotFoundException($"Academy with Id {academyId} not found.");

            await ValidateTargetAsync(academyId, dto.TargetType, dto.TargetId);

            string mappedRole = string.Empty;
            if (dto.TargetType == AnnouncementTargetType.Role)
            {
                mappedRole = dto.TargetId switch
                {
                    4 => "Player",
                    5 => "Parent",
                    6 => "Coach",
                    _ => throw new BadRequestException($"Role ID {dto.TargetId} is not supported for live notifications.")
                };
            }
            dto.Role = mappedRole;

            // Send Notification via Notification Service with isSystemAdmin flag
            await _announcementNotificationService.SendAnnouncementNotificationAsync(
                academyId,
                sentByUserId,
                dto,
                isSystemAdmin,
                cancellationToken);

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
        public async Task<Koralytics.Application.DTOs.Common.PagedResponseDto<AnnouncementResponseDto>> GetAnnouncementsAsync(int academyId, Koralytics.Application.DTOs.Common.PaginationRequestDto request)
        {
            _ = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                .FindAsNoTrackingAsync(a => a.Id == academyId)
                ?? throw new NotFoundException($"Academy with Id {academyId} not found.");

            var announcementsQuery = _unitOfWork.Repository<AcademyAnnouncement>()
                .GetQueryableAsNoTracking()
                .Where(a => a.AcademyId == academyId)
                .OrderByDescending(a => a.CreatedAt);

            var totalCount = await announcementsQuery.CountAsync();
            var announcements = await announcementsQuery
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            // Fetch sender names in a single query
            var senderIds = announcements.Select(a => a.SentByUserId).Distinct().ToList();
            var senders = await _unitOfWork.Repository<Domain.Entities.Identity.User>()
                .FindAllAsNoTrackingAsync(u => senderIds.Contains(u.Id));
            var senderMap = senders.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");

            var items = announcements.Select(a => new AnnouncementResponseDto
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

            return new Koralytics.Application.DTOs.Common.PagedResponseDto<AnnouncementResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
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
                    if (targetId is not (4 or 5 or 6))
                        throw new BadRequestException(
                            $"Role ID {targetId} is not supported for academy announcements.");
                    break;

                default:
                    throw new BadRequestException($"Unknown TargetType: {targetType}.");
            }
        }
    }
}
