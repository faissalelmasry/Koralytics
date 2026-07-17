using Koralytics.Application.DTOs.Coach;
using Koralytics.Application.Interfaces;
using Koralytics.Domain.Entities.Coach;
using Koralytics.Domain.Entities.Identity;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koralytics.Application.Services.Coach.CoachAccessService
{
    public class CoachAccessService : ICoachAccessService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CoachAccessService> _logger;

        public CoachAccessService(IUnitOfWork unitOfWork, ILogger<CoachAccessService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GrantTempAccessAsync
        // ─────────────────────────────────────────────────────────────────────
        public async Task<TempAccessDto> GrantTempAccessAsync(int coachId, GrantTempAccessDto dto)
        {
            _logger.LogInformation(
                "Coach {CoachId} granting temp access to user {GrantedTo}", coachId, dto.GrantedToUserId);

            if (dto.ExpiresAt <= DateTime.UtcNow)
                throw new BadRequestException("ExpiresAt must be a future date.");

            // Validate grantee exists
            var grantee = await _unitOfWork.Repository<User>()
                .FindAsync(u => u.Id == dto.GrantedToUserId)
                ?? throw new NotFoundException($"User with Id {dto.GrantedToUserId} not found.");

            // Prevent granting access to yourself
            if (dto.GrantedToUserId == coachId)
                throw new BadRequestException("A coach cannot grant access to themselves.");

            var access = new CoachTempAccess
            {
                CoachUserId = coachId,
                GrantedToUserId = dto.GrantedToUserId,
                AccessLevel = dto.AccessLevel,
                ExpiresAt = dto.ExpiresAt,
                Status = TempAccessStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<CoachTempAccess>().AddAsync(access);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Temp access {AccessId} granted by coach {CoachId} to user {GrantedTo}, expires {ExpiresAt}",
                access.Id, coachId, dto.GrantedToUserId, dto.ExpiresAt);

            return MapToDto(access, grantee);
        }

        // ─────────────────────────────────────────────────────────────────────
        // RevokeTempAccessAsync
        // ─────────────────────────────────────────────────────────────────────
        public async Task<TempAccessDto> RevokeTempAccessAsync(int coachId, int accessId)
        {
            _logger.LogInformation(
                "Coach {CoachId} revoking access grant {AccessId}", coachId, accessId);

            var access = await _unitOfWork.Repository<CoachTempAccess>()
                .GetQueryable()
                .Include(a => a.GrantedToUser)
                .FirstOrDefaultAsync(a => a.Id == accessId)
                ?? throw new NotFoundException($"Access grant {accessId} not found.");

            // Only the owning coach may revoke
            if (access.CoachUserId != coachId)
                throw new ForbiddenException(
                    $"Coach {coachId} does not own access grant {accessId}.");

            if (access.Status == TempAccessStatus.Revoked)
                throw new BadRequestException($"Access grant {accessId} is already revoked.");

            access.Status = TempAccessStatus.Revoked;
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Access grant {AccessId} revoked by coach {CoachId}", accessId, coachId);

            return MapToDto(access, access.GrantedToUser);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GetActiveGrantsAsync
        // ─────────────────────────────────────────────────────────────────────
        public async Task<IEnumerable<TempAccessDto>> GetActiveGrantsAsync(int coachId)
        {
            _logger.LogInformation("Fetching active access grants for coach {CoachId}", coachId);

            var now = DateTime.UtcNow;

            var grants = await _unitOfWork.Repository<CoachTempAccess>()
                .GetQueryableAsNoTracking()
                .Include(a => a.GrantedToUser)
                .Where(a =>
                    a.CoachUserId == coachId &&
                    a.Status == TempAccessStatus.Active &&
                    a.ExpiresAt > now)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return grants.Select(a => MapToDto(a, a.GrantedToUser));
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private helper
        // ─────────────────────────────────────────────────────────────────────
        private static TempAccessDto MapToDto(CoachTempAccess access, User grantee) =>
            new()
            {
                Id = access.Id,
                CoachUserId = access.CoachUserId,
                GrantedToUserId = access.GrantedToUserId,
                GrantedToFullName = $"{grantee.FirstName} {grantee.LastName}",
                AccessLevel = access.AccessLevel,
                Status = access.Status,
                ExpiresAt = access.ExpiresAt,
                CreatedAt = access.CreatedAt
            };
    }
}
