using Koralytics.Application.DTOs.Notification;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Notification;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Coach;
using Koralytics.Domain.Entities.Parents;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Koralytics.Application.Services.Notification.AnnouncementNotificationService
{
    public class AnnouncementNotificationService : IAnnouncementNotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRealTimeBridge _realTimeBridge;

        public AnnouncementNotificationService(IUnitOfWork unitOfWork, IRealTimeBridge realTimeBridge)
        {
            _unitOfWork = unitOfWork;
            _realTimeBridge = realTimeBridge;
        }

        public async Task SendAnnouncementNotificationAsync(int academyId,int userId, CreateAnnouncementDto body, bool isSystemAdmin = false, CancellationToken cancellationToken = default)
        {
            if (body == null)
                throw new BadRequestException("Announcement payload cannot be null.");

            if (string.IsNullOrWhiteSpace(body.Title) || string.IsNullOrWhiteSpace(body.Body))
                throw new BadRequestException("Announcement title and content are required.");

            var academyExists = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                  .ExistsAsync(a => a.Id == academyId && !a.IsDeleted);

            if (!academyExists)
                throw new NotFoundException($"Academy with ID {academyId} was not found or is inactive.");

            var callerIsAcademyStaff = isSystemAdmin || await _unitOfWork.Repository<CoachAcademy>()
                .ExistsAsync(ca => ca.AcademyId == academyId && ca.CoachUserId == userId && ca.LeftAt == null);

            if (!callerIsAcademyStaff)
                throw new ForbiddenException($"User {userId} is not authorized to send announcements for Academy {academyId}.");
            
            var payloadData = new { SenderId = userId, AcademyId = academyId };

            var notification = new CachedNotification
            {
                Title = body.Title,
                Content = body.Body,
                Type = "AcademyAnnouncement",
                Payload = payloadData
            };

            const string clientMethod = "ReceiveAnnouncement";
            var targetPlayerIds = new List<int>();
            var targetNonPlayerUserIds = new List<int>();
            var notifyParentsOfPlayers = false;

            switch (body.TargetType)
            {
                case AnnouncementTargetType.All:
                    targetPlayerIds = await _unitOfWork.Repository<PlayerAcademy>()
                        .GetQueryableAsNoTracking()
                        .Where(pa => pa.AcademyId == academyId && pa.Status == PlayerAcademyStatus.Active)
                        .Select(pa => pa.PlayerId)
                        .ToListAsync();
                    break;

                case AnnouncementTargetType.Team:
                    if (body.TargetId <= 0)
                        throw new BadRequestException("A valid TargetId is required when TargetType is 'TEAM'.");

                    var teamExists = await _unitOfWork.Repository<Team>()
                        .ExistsAsync(t => t.Id == body.TargetId && t.AcademyId == academyId && !t.IsDeleted);

                    if (!teamExists)
                        throw new NotFoundException($"Team with ID {body.TargetId} does not exist inside Academy {academyId}.");

                    targetPlayerIds = await _unitOfWork.Repository<PlayerTeam>()
                        .GetQueryableAsNoTracking()
                        .Where(pt => pt.TeamId == body.TargetId && pt.Team.AcademyId == academyId)
                        .Select(pt => pt.PlayerId)
                        .Distinct()
                        .ToListAsync();

                   
                    notifyParentsOfPlayers = true;
                    break;

                case AnnouncementTargetType.AgeGroup:
                    if (body.TargetId <= 0)
                        throw new BadRequestException("A valid TargetId is required when TargetType is 'AGEGROUP'.");

                    var ageGroupExists = await _unitOfWork.Repository<AgeGroup>()
                        .ExistsAsync(ag => ag.Id == body.TargetId && ag.AcademyId == academyId && !ag.IsDeleted);

                    if (!ageGroupExists)
                        throw new NotFoundException($"Age Group with ID {body.TargetId} does not exist inside Academy {academyId}.");

                    targetPlayerIds = await _unitOfWork.Repository<PlayerTeam>()
                        .GetQueryableAsNoTracking()
                        .Where(pt => pt.Team.AgeGroupId == body.TargetId && pt.Team.AcademyId == academyId)
                        .Select(pt => pt.PlayerId)
                        .Distinct()
                        .ToListAsync();
                    break;

                case AnnouncementTargetType.Role:
                    if (string.IsNullOrWhiteSpace(body.Role))
                        throw new BadRequestException("TargetRole is required when TargetType is 'ROLE'.");

                    if (body.Role.Equals("Coach", StringComparison.OrdinalIgnoreCase))
                    {
                        targetNonPlayerUserIds = await _unitOfWork.Repository<CoachAcademy>()
                            .GetQueryableAsNoTracking()
                            .Where(ca => ca.AcademyId == academyId && ca.LeftAt == null)
                            .Select(ca => ca.CoachUserId)
                            .ToListAsync();
                    }
                    else if (body.Role.Equals("Player", StringComparison.OrdinalIgnoreCase))
                    {
                        targetPlayerIds = await _unitOfWork.Repository<PlayerAcademy>()
                            .GetQueryableAsNoTracking()
                            .Where(pa => pa.AcademyId == academyId && pa.Status == PlayerAcademyStatus.Active)
                            .Select(pa => pa.PlayerId)
                            .ToListAsync();
                    }
                    else
                    {
                        throw new BadRequestException($"Unsupported announcement target role: '{body.Role}'.");
                    }
                    break;

                default:
                    throw new BadRequestException($"Invalid notification target type: {body.TargetType}");
            }

            var allTargetUserIds = new List<int>(targetNonPlayerUserIds);
            allTargetUserIds.AddRange(targetPlayerIds);

            if (notifyParentsOfPlayers && targetPlayerIds.Count > 0)
            {
                var parentIds = await _unitOfWork.Repository<ParentPlayer>()
                    .GetQueryableAsNoTracking()
                    .Where(pp => targetPlayerIds.Contains(pp.PlayerId))
                    .Select(pp => pp.ParentId)
                    .Distinct()
                    .ToListAsync();

                allTargetUserIds.AddRange(parentIds);
            }

            var distinctTargetUserIds = allTargetUserIds.Distinct().ToList();

            if (distinctTargetUserIds.Count > 0)
            {
                
                await _realTimeBridge.SendAndCacheToUsersAsync(distinctTargetUserIds, clientMethod, notification, cancellationToken);
            }
        }
    }
}