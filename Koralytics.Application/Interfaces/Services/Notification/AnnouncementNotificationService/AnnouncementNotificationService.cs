using Koralytics.Application.DTOs.Notification;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Notification;
using Koralytics.Application.Mappings.Academies;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public async Task SendAnnouncementNotificationAsync(int academyId, int userId, CreateAnnouncementDto body)
        {
            if (body == null)
                throw new BadRequestException("Announcement payload cannot be null.");

            if (string.IsNullOrWhiteSpace(body.Title) || string.IsNullOrWhiteSpace(body.Body))
                throw new BadRequestException("Announcement title and content are required.");

            var academyExists = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                  .ExistsAsync(a => a.Id == academyId && !a.IsDeleted);

            if (!academyExists)
                throw new NotFoundException($"Academy with ID {academyId} was not found or is inactive.");

            var payload = new
            {
                Title = body.Title,
                Content = body.Body,
                SenderId = userId,
                AcademyId = academyId,
                SentAt = DateTime.UtcNow
            };

            const string clientMethod = "ReceiveAnnouncement";

            switch (body.TargetType)
            {
                case AnnouncementTargetType.All:
                    await _realTimeBridge.SendToGroupAsync($"Academy_{academyId}", clientMethod, payload);
                    break;

                case AnnouncementTargetType.Team:
                    if (body.TargetId <= 0)
                        throw new BadRequestException("A valid TargetId is required when TargetType is 'TEAM'.");

                    var teamExists = await _unitOfWork.Repository<Team>()
                        .ExistsAsync(t => t.Id == body.TargetId && t.AcademyId == academyId && !t.IsDeleted);

                    if (!teamExists)
                        throw new NotFoundException($"Team with ID {body.TargetId} does not exist inside Academy {academyId}.");

                    await _realTimeBridge.SendToGroupAsync($"Team_{body.TargetId}", clientMethod, payload);

                    var playerTeams = await _unitOfWork.Repository<PlayerTeam>()
                        .FindAllAsync(pt => pt.TeamId == body.TargetId);

                    var parentUserIds = playerTeams
                        .Where(pt => pt.Player?.ParentPlayers != null)
                        .SelectMany(pt => pt.Player.ParentPlayers)
                        .Select(pp => pp.Id)
                        .Distinct()
                        .ToList();

                    foreach (var parentId in parentUserIds)
                    {
                        await _realTimeBridge.SendToGroupAsync($"Parent_{parentId}", clientMethod, payload);
                    }
                    break;

                case AnnouncementTargetType.AgeGroup:
                    if (body.TargetId <= 0)
                        throw new BadRequestException("A valid TargetId is required when TargetType is 'AGEGROUP'.");

                    var ageGroupExists = await _unitOfWork.Repository<AgeGroup>()
                        .ExistsAsync(ag => ag.Id == body.TargetId && ag.AcademyId == academyId && !ag.IsDeleted);

                    if (!ageGroupExists)
                        throw new NotFoundException($"Age Group with ID {body.TargetId} does not exist inside Academy {academyId}.");

                    await _realTimeBridge.SendToGroupAsync($"AgeGroup_{body.TargetId}", clientMethod, payload);
                    break;

                case AnnouncementTargetType.Role:
                    if (string.IsNullOrWhiteSpace(body.Role))
                        throw new BadRequestException("TargetRole is required when TargetType is 'ROLE'.");

                    await _realTimeBridge.SendToGroupAsync($"Academy_{academyId}_Role_{body.Role}", clientMethod, payload);
                    break;

                default:
                    throw new BadRequestException($"Invalid notification target type: {body.TargetType}");
            }
        }
    }
}
