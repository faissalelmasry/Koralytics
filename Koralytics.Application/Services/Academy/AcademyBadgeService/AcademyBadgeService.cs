using AutoMapper;
using Koralytics.Application.DTOs.Academies;
using Koralytics.Application.Interfaces;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Koralytics.Application.Services.Academy.AcademyBadgeService
{
    public class AcademyBadgeService : IAcademyBadgeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<AcademyBadgeService> _logger;

        public AcademyBadgeService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<AcademyBadgeService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<AcademyBadgeResponseDto> CreateBadgeAsync(int academyId, CreateAcademyBadgeDto dto, int performedByUserId)
        {
            _logger.LogInformation("User {UserId} creating badge for academy {AcademyId}", performedByUserId, academyId);

            var academy = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>().FindAsync(a => a.Id == academyId);
            if (academy is null)
                throw new NotFoundException($"Academy {academyId} not found.");

            var badgeExists = await _unitOfWork.Repository<AcademyBadge>()
                .ExistsAsync(b => b.AcademyId == academyId && b.BadgeType == dto.BadgeType);

            if (badgeExists)
                throw new ConflictException($"A badge of type '{dto.BadgeType}' already exists in this academy.");

            var badge = _mapper.Map<AcademyBadge>(dto);
            badge.AcademyId = academyId;
            badge.CreatedById = performedByUserId;

            await _unitOfWork.Repository<AcademyBadge>().AddAsync(badge);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Badge type '{Type}' (Id={Id}) created for academy {AcademyId}", badge.BadgeType, badge.Id, academyId);

            return _mapper.Map<AcademyBadgeResponseDto>(badge);
        }

        public async Task<IEnumerable<AcademyBadgeResponseDto>> GetBadgesByAcademyAsync(int academyId)
        {
            var academyExists = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>().ExistsAsync(a => a.Id == academyId);
            if (!academyExists)
                throw new NotFoundException($"Academy {academyId} not found.");

            var badges = await _unitOfWork.Repository<AcademyBadge>()
                .FindAllAsNoTrackingAsync(b => b.AcademyId == academyId);

            return _mapper.Map<IEnumerable<AcademyBadgeResponseDto>>(badges);
        }

        public async Task DeleteBadgeAsync(int academyId, int badgeId, int performedByUserId)
        {
            _logger.LogInformation("User {UserId} deleting badge {BadgeId} in academy {AcademyId}", performedByUserId, badgeId, academyId);

            var badge = await _unitOfWork.Repository<AcademyBadge>().FindAsync(b => b.Id == badgeId && b.AcademyId == academyId);
            if (badge is null)
                throw new NotFoundException($"Badge {badgeId} not found in academy {academyId}.");

            _unitOfWork.Repository<AcademyBadge>().SoftDelete(badge);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Badge {BadgeId} deleted successfully.", badgeId);
        }
    }
}
