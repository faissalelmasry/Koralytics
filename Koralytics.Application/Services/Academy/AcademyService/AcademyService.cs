using AutoMapper;

using Koralytics.Application.DTOs.Academies;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Validators.Academies;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.SystemAdmin;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koralytics.Application.Services.Academy.AcademyService
{
    public class AcademyService : IAcademyService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<AcademyService> _logger;

        public AcademyService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<AcademyService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        // ──────────────────────────────────────────────────────────────────────
        // CreateAcademyAsync
        // Called after SuperAdmin approves an AcademyRequest.
        // ──────────────────────────────────────────────────────────────────────
        public async Task<AcademyResponseDto> CreateAcademyAsync(CreateAcademyDto dto, int performedByUserId)
        {
            _logger.LogInformation("User {UserId} creating academy from AcademyRequest {RequestId}",performedByUserId, dto.AcademyRequestId);

            // Validate AcademyRequest exists and is still Pending
            var AcademyRequest = await _unitOfWork.Repository<AcademyRequest>()
                .FindAsync(r => r.Id == dto.AcademyRequestId);

            if (AcademyRequest is null)
                throw new NotFoundException(
                    $"AcademyRequest with Id {dto.AcademyRequestId} not found.");

            if (AcademyRequest.RequestStatus == AcademyRequestStatus.Approved)
                throw new ConflictException(
                    "This academy request has already been approved and an academy was created.");

            if (AcademyRequest.RequestStatus == AcademyRequestStatus.Rejected)
                throw new BadRequestException(
                    "Cannot create an academy from a rejected request.");

            // Validate academy name is unique
            var nameExists = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                .ExistsAsync(a => a.Name == dto.Name);

            if (nameExists)
                throw new ConflictException(
                    $"An academy with the name '{dto.Name}' already exists.");

            
            //start transaction for these two operations: creating academy and updating request status
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {

                // Map and create the Academy record
                var academy = _mapper.Map<Domain.Entities.Academy.Academy>(dto);
                academy.Status = AcademyStatus.Active;
                academy.CreatedById = performedByUserId;
                academy.UpdatedById = performedByUserId;
                academy.UpdatedAt = DateTime.UtcNow;


                await _unitOfWork.Repository<Domain.Entities.Academy.Academy>().AddAsync(academy);
                await _unitOfWork.SaveChangesAsync();

                // Update the AcademyRequest: mark Approved, record reviewer
                AcademyRequest.RequestStatus = AcademyRequestStatus.Approved;
                AcademyRequest.ReviewedById = performedByUserId;
                AcademyRequest.ReviewedAt = DateTime.UtcNow;
                AcademyRequest.UpdatedById = performedByUserId;
                await _unitOfWork.SaveChangesAsync();

                // Update the RoleAuditLog for the Admin user to reflect their new role in the academy
                var roleAuditLog = new RoleAuditLog
                {
                    AffectedUserId = academy.AdminUserId,
                    AcademyId = academy.Id,
                    Action = RoleAuditAction.Assigned,
                    PerformedByUserId = performedByUserId,
                    Details = $"Academy Admin {academy.AdminUserId} assigned as admin to academy {academy.Name} by {performedByUserId}"
                };
                await _unitOfWork.Repository<RoleAuditLog>().AddAsync(roleAuditLog);

                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Academy '{Name}' (Id={Id}) created. Request {RequestId} marked Approved.",
                    academy.Name, academy.Id, dto.AcademyRequestId);

                // Reload with Admin navigation for mapping
                var created = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                    .GetQueryableAsNoTracking()
                    .Include(a => a.Admin)
                    .FirstOrDefaultAsync(a => a.Id == academy.Id);

                return _mapper.Map<AcademyResponseDto>(created!);

            }
            catch (Exception ex) 
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to create academy for AcademyRequest {RequestId}.",dto.AcademyRequestId);
                throw;

            }


        }

        // ──────────────────────────────────────────────────────────────────────
        // UpdateAcademyAsync
        // ──────────────────────────────────────────────────────────────────────
        public async Task<AcademyResponseDto> UpdateAcademyAsync(int academyId, UpdateAcademyDto dto, int performedByUserId)
        {
            _logger.LogInformation(
                "User {UserId} updating academy {AcademyId}", performedByUserId, academyId);

            var academy = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                .FindAsync(a => a.Id == academyId);

            if (academy is null)
                throw new NotFoundException($"Academy with Id {academyId} not found.");

            // If name is changing, ensure it stays unique
            if (dto.Name is not null && dto.Name != academy.Name)
            {
                var nameExists = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                    .ExistsAsync(a => a.Name == dto.Name && a.Id != academyId);

                if (nameExists)
                    throw new ConflictException(
                        $"An academy with the name '{dto.Name}' already exists.");

                academy.Name = dto.Name;
            }

            if (dto.LogoUrl is not null)
                academy.LogoUrl = dto.LogoUrl;

            if (dto.PrimaryColor is not null)
                academy.PrimaryColor = dto.PrimaryColor;

            if (dto.SecondaryColor is not null)
                academy.SecondaryColor = dto.SecondaryColor;

            academy.UpdatedById = performedByUserId;
            academy.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Academy {AcademyId} updated successfully.", academyId);

            var updated = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                .GetQueryableAsNoTracking()
                .Include(a => a.Admin)
                .FirstOrDefaultAsync(a => a.Id == academyId);

            return _mapper.Map<AcademyResponseDto>(updated!);
        }

        // ──────────────────────────────────────────────────────────────────────
        // AddLocationAsync
        // Automatically sets IsMain = true when it is the first location.
        // ──────────────────────────────────────────────────────────────────────
        public async Task<AcademyLocationResponseDto> AddLocationAsync(int academyId, AddLocationDto dto, int performedByUserId)
        {
            _logger.LogInformation(
                "User {UserId} adding location to academy {AcademyId}", performedByUserId, academyId);

            var academy = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                .FindAsync(a => a.Id == academyId);

            if (academy is null)
                throw new NotFoundException($"Academy with Id {academyId} not found.");

            // Check for duplicate location name within the same academy
            var duplicateName = await _unitOfWork.Repository<AcademyLocation>()
                .ExistsAsync(l => l.AcademyId == academyId && l.Name == dto.Name);

            if (duplicateName)
                throw new ConflictException(
                    $"A location named '{dto.Name}' already exists for this academy.");

            // Determine if this is the first location (auto-set as main)
            var hasExistingLocations = await _unitOfWork.Repository<AcademyLocation>()
                .ExistsAsync(l => l.AcademyId == academyId);

            var location = _mapper.Map<AcademyLocation>(dto);
            location.AcademyId = academyId;
            location.IsMain = !hasExistingLocations; // first location → IsMain = true
            location.CreatedById = performedByUserId;

            await _unitOfWork.Repository<AcademyLocation>().AddAsync(location);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Location '{Name}' (Id={Id}) added to academy {AcademyId}. IsMain={IsMain}",
                location.Name, location.Id, academyId, location.IsMain);

            return _mapper.Map<AcademyLocationResponseDto>(location);
        }

        // ──────────────────────────────────────────────────────────────────────
        // GetAcademyAsync
        // ──────────────────────────────────────────────────────────────────────
        public async Task<AcademyResponseDto> GetAcademyAsync(int academyId)
        {
            var academy = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                .GetQueryableAsNoTracking()
                .Include(a => a.Admin)
                .FirstOrDefaultAsync(a => a.Id == academyId);

            if (academy is null)
                throw new NotFoundException($"Academy with Id {academyId} not found.");

            return _mapper.Map<AcademyResponseDto>(academy);
        }

        // ──────────────────────────────────────────────────────────────────────
        // GetAllAcademiesAsync
        // ──────────────────────────────────────────────────────────────────────
        public async Task<IEnumerable<AcademyResponseDto>> GetAllAcademiesAsync()
        {
            var academies = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                .GetQueryableAsNoTracking()
                .Include(a => a.Admin)
                .ToListAsync();
            if (academies is null)
                throw new NotFoundException($"No academies found.");
            return _mapper.Map<IEnumerable<AcademyResponseDto>>(academies);
        }

        // ──────────────────────────────────────────────────────────────────────
        // GetLocationsAsync
        // ──────────────────────────────────────────────────────────────────────
        public async Task<IEnumerable<AcademyLocationResponseDto>> GetLocationsAsync(int academyId)
        {
            _ = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                .FindAsNoTrackingAsync(a => a.Id == academyId)
                ?? throw new NotFoundException($"Academy with Id {academyId} not found.");

            var locations = await _unitOfWork.Repository<AcademyLocation>()
                .FindAllAsNoTrackingAsync(l => l.AcademyId == academyId);

            return _mapper.Map<IEnumerable<AcademyLocationResponseDto>>(locations);
        }

        // ──────────────────────────────────────────────────────────────────────
        // SetMainLocationAsync
        // ──────────────────────────────────────────────────────────────────────
        public async Task SetMainLocationAsync(int academyId, int locationId, int performedByUserId)
        {
            _logger.LogInformation(
                "User {UserId} setting location {LocationId} as main for academy {AcademyId}",
                performedByUserId, locationId, academyId);

            var target = await _unitOfWork.Repository<AcademyLocation>()
                .FindAsync(l => l.Id == locationId && l.AcademyId == academyId);

            if (target is null)
                throw new NotFoundException(
                    $"Location {locationId} not found in academy {academyId}.");

            if (target.IsMain)
                throw new BadRequestException("This location is already the main location.");

            // Clear existing main flag
            var currentMain = await _unitOfWork.Repository<AcademyLocation>()
                .FindAsync(l => l.AcademyId == academyId && l.IsMain);

            if (currentMain is not null)
                currentMain.IsMain = false;

            target.IsMain = true;
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Location {LocationId} is now the main location for academy {AcademyId}.",
                locationId, academyId);
        }
    }
}
