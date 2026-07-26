using AutoMapper;

using Koralytics.Application.DTOs.Academies;
using Koralytics.Application.DTOs.Academy;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Validators.Academies;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Coach;
using Koralytics.Domain.Entities.Player;
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
        // ApproveAcademyAsync
        // Called after SuperAdmin approves an AcademyRequest.
        // ──────────────────────────────────────────────────────────────────────
        public async Task<AcademyResponseDto> ApproveAcademyAsync(CreateAcademyDto dto, int performedByUserId)
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

                // Update the AcademyAdmin entity to link to this new Academy
                var adminUser = await _unitOfWork.Repository<AcademyAdmin>().FindAsync(a => a.Id == academy.AdminUserId);
                if (adminUser != null)
                {
                    adminUser.AcademyId = academy.Id;
                }

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
                .Include(a => a.AcademyLocations)
                .FirstOrDefaultAsync(a => a.Id == academyId);

            if (academy is null)
                throw new NotFoundException($"Academy with Id {academyId} not found.");

            return _mapper.Map<AcademyResponseDto>(academy);
        }

        public async Task<IEnumerable<Koralytics.Application.DTOs.SystemAdmin.AcademyRequestResponseDto>> GetMyAcademyRequestsAsync(int userId)
        {
            var requests = await _unitOfWork.Repository<Domain.Entities.SystemAdmin.AcademyRequest>()
                .GetQueryableAsNoTracking()
                .Include(r => r.RequestedBy)
                .Where(r => r.RequestedById == userId)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<Koralytics.Application.DTOs.SystemAdmin.AcademyRequestResponseDto>>(requests);
        }

        // ──────────────────────────────────────────────────────────────────────
        // GetAllAcademiesAsync
        // ──────────────────────────────────────────────────────────────────────
        public async Task<AcademyListResponseDto> GetAllAcademiesAsync(AcademyListRequestDto request)
        {
            var query = _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                .GetQueryableAsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.SearchQuery))
            {
                var lowerQuery = request.SearchQuery.ToLower();
                query = query.Where(a => a.Name.ToLower().Contains(lowerQuery));
            }

            var totalCount = await query.CountAsync();

            var academies = await query
                .Include(a => a.Admin)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new AcademyListResponseDto
            {
                Academies = _mapper.Map<List<AcademyResponseDto>>(academies),
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
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

            // Clear existing main flag(s)
            var currentMains = await _unitOfWork.Repository<AcademyLocation>()
                .FindAllAsync(l => l.AcademyId == academyId && l.IsMain);

            foreach (var loc in currentMains)
            {
                loc.IsMain = false;
            }

            target.IsMain = true;
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Location {LocationId} is now the main location for academy {AcademyId}.",
                locationId, academyId);
        }

        // ──────────────────────────────────────────────────────────────────────
        // RequestAcademyAsync
        // ──────────────────────────────────────────────────────────────────────
        public async Task<Koralytics.Application.DTOs.SystemAdmin.AcademyRequestResponseDto> RequestAcademyAsync(Koralytics.Application.DTOs.SystemAdmin.CreateAcademyRequestDto dto, int requestedByUserId)
        {
            _logger.LogInformation("User {UserId} requesting new academy {AcademyName}", requestedByUserId, dto.AcademyName);

            // Check if user already has an active or pending request
            var existingRequest = await _unitOfWork.Repository<AcademyRequest>()
                .ExistsAsync(r => r.RequestedById == requestedByUserId && 
                             (r.RequestStatus == AcademyRequestStatus.Pending || r.RequestStatus == AcademyRequestStatus.Approved));
                             
            if (existingRequest)
                throw new ConflictException("You already have a pending or approved academy request.");

            var request = _mapper.Map<AcademyRequest>(dto);
            request.RequestedById = requestedByUserId;
            request.RequestStatus = AcademyRequestStatus.Pending;
            request.RequestedAt = DateTime.UtcNow;

            await _unitOfWork.Repository<AcademyRequest>().AddAsync(request);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("AcademyRequest {RequestId} created successfully.", request.Id);

            var created = await _unitOfWork.Repository<AcademyRequest>()
                .GetQueryableAsNoTracking()
                .Include(r => r.RequestedBy)
                .FirstOrDefaultAsync(r => r.Id == request.Id);

            return _mapper.Map<Koralytics.Application.DTOs.SystemAdmin.AcademyRequestResponseDto>(created);
        }

        // ──────────────────────────────────────────────────────────────────────
        // GetPendingRequestsAsync
        // ──────────────────────────────────────────────────────────────────────
        public async Task<IEnumerable<Koralytics.Application.DTOs.SystemAdmin.AcademyRequestResponseDto>> GetPendingRequestsAsync()
        {
            var requests = await _unitOfWork.Repository<AcademyRequest>()
                .GetQueryableAsNoTracking()
                .Include(r => r.RequestedBy)
                .Where(r => r.RequestStatus == AcademyRequestStatus.Pending)
                .ToListAsync();

            return _mapper.Map<IEnumerable<Koralytics.Application.DTOs.SystemAdmin.AcademyRequestResponseDto>>(requests);
        }

        // ──────────────────────────────────────────────────────────────────────
        // RejectAcademyRequestAsync
        // ──────────────────────────────────────────────────────────────────────
        public async Task RejectAcademyRequestAsync(int requestId, Koralytics.Application.DTOs.SystemAdmin.RejectAcademyRequestDto dto, int performedByUserId)
        {
            _logger.LogInformation("User {UserId} rejecting AcademyRequest {RequestId}", performedByUserId, requestId);

            var request = await _unitOfWork.Repository<AcademyRequest>().FindAsync(r => r.Id == requestId);
            if (request is null)
                throw new NotFoundException($"AcademyRequest {requestId} not found.");

            if (request.RequestStatus != AcademyRequestStatus.Pending)
                throw new BadRequestException("Only pending requests can be rejected.");

            request.RequestStatus = AcademyRequestStatus.Rejected;
            request.RejectedReason = dto.Reason;
            request.ReviewedById = performedByUserId;
            request.ReviewedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("AcademyRequest {RequestId} rejected successfully.", requestId);
        }

        // ──────────────────────────────────────────────────────────────────────
        // Admin Management
        // ──────────────────────────────────────────────────────────────────────
        public async Task AssignAdminToAcademyAsync(int academyId, int adminUserId, int performedByUserId)
        {
            _logger.LogInformation("User {UserId} assigning admin {AdminId} to academy {AcademyId}", performedByUserId, adminUserId, academyId);

            var academy = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>().FindAsync(a => a.Id == academyId);
            if (academy is null)
                throw new NotFoundException($"Academy {academyId} not found.");

            var admin = await _unitOfWork.Repository<AcademyAdmin>().FindAsync(a => a.Id == adminUserId);
            if (admin is null)
                throw new NotFoundException($"AcademyAdmin with User Id {adminUserId} not found.");

            if (admin.AcademyId == academyId)
                throw new ConflictException("Admin is already assigned to this academy.");

            admin.AcademyId = academyId;
            admin.UpdatedById = performedByUserId;
            admin.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task RemoveAdminFromAcademyAsync(int academyId, int adminUserId, int performedByUserId)
        {
            _logger.LogInformation("User {UserId} removing admin {AdminId} from academy {AcademyId}", performedByUserId, adminUserId, academyId);

            var admin = await _unitOfWork.Repository<AcademyAdmin>().FindAsync(a => a.Id == adminUserId);
            if (admin is null)
                throw new NotFoundException($"AcademyAdmin with User Id {adminUserId} not found.");

            if (admin.AcademyId != academyId)
                throw new BadRequestException("Admin is not assigned to this academy.");

            admin.AcademyId = null;
            admin.UpdatedById = performedByUserId;
            admin.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
        }

        // ──────────────────────────────────────────────────────────────────────
        // RegisterCoachToAcademyAsync
        // ──────────────────────────────────────────────────────────────────────
        public async Task RegisterCoachToAcademyAsync(int academyId, int coachUserId, int performedByUserId)
        {
            _logger.LogInformation("User {UserId} registering coach {CoachId} to academy {AcademyId}", performedByUserId, coachUserId, academyId);

            var academy = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>().FindAsync(a => a.Id == academyId);
            if (academy is null)
                throw new NotFoundException($"Academy {academyId} not found.");

            var coachExists = await _unitOfWork.Repository<Domain.Entities.Coach.Coach>().ExistsAsync(c => c.Id == coachUserId);
            if (!coachExists)
                throw new NotFoundException($"Coach with User Id {coachUserId} not found.");

            var alreadyRegistered = await _unitOfWork.Repository<Domain.Entities.Coach.CoachAcademy>()
                .ExistsAsync(ca => ca.AcademyId == academyId && ca.CoachUserId == coachUserId && ca.LeftAt == null);

            if (alreadyRegistered)
                throw new ConflictException("Coach is already registered to this academy.");

            var coachAcademy = new Domain.Entities.Coach.CoachAcademy
            {
                AcademyId = academyId,
                CoachUserId = coachUserId,
                JoinedAt = DateTime.UtcNow,
                CreatedById = performedByUserId
            };

            await _unitOfWork.Repository<Domain.Entities.Coach.CoachAcademy>().AddAsync(coachAcademy);
            await _unitOfWork.SaveChangesAsync();
        }

        // ──────────────────────────────────────────────────────────────────────
        // RemoveCoachFromAcademyAsync
        // ──────────────────────────────────────────────────────────────────────
        public async Task RemoveCoachFromAcademyAsync(int academyId, int coachUserId, int performedByUserId)
        {
            _logger.LogInformation("User {UserId} removing coach {CoachId} from academy {AcademyId}", performedByUserId, coachUserId, academyId);

            var coachAcademy = await _unitOfWork.Repository<Domain.Entities.Coach.CoachAcademy>()
                .FindAsync(ca => ca.AcademyId == academyId && ca.CoachUserId == coachUserId && ca.LeftAt == null);

            if (coachAcademy is null)
                throw new NotFoundException($"Active assignment for coach {coachUserId} in academy {academyId} not found.");

            // Cascade: remove the coach from all active team assignments within this academy
            var teamIds = await _unitOfWork.Repository<Team>()
                .GetQueryableAsNoTracking()
                .Where(t => t.AcademyId == academyId)
                .Select(t => t.Id)
                .ToListAsync();

            var coachTeamsInAcademy = await _unitOfWork.Repository<CoachTeam>()
                .FindAllAsync(ct => ct.CoachUserId == coachUserId && teamIds.Contains(ct.TeamId) && ct.RemovedAt == null);

            foreach (var ct in coachTeamsInAcademy)
            {
                ct.RemovedAt = DateTime.UtcNow;
                _unitOfWork.Repository<CoachTeam>().SoftDelete(ct);
            }

            coachAcademy.LeftAt = DateTime.UtcNow;
            _unitOfWork.Repository<Domain.Entities.Coach.CoachAcademy>().SoftDelete(coachAcademy);
            await _unitOfWork.SaveChangesAsync();
        }

        // ──────────────────────────────────────────────────────────────────────
        // RegisterPlayerToAcademyAsync
        // ──────────────────────────────────────────────────────────────────────
        public async Task RegisterPlayerToAcademyAsync(int academyId, int playerUserId, int performedByUserId)
        {
            _logger.LogInformation("User {UserId} registering player {PlayerId} to academy {AcademyId}", performedByUserId, playerUserId, academyId);

            var academy = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>().FindAsync(a => a.Id == academyId);
            if (academy is null)
                throw new NotFoundException($"Academy {academyId} not found.");

            var playerExists = await _unitOfWork.Repository<Domain.Entities.Player.Player>().ExistsAsync(p => p.Id == playerUserId);
            if (!playerExists)
                throw new NotFoundException($"Player with User Id {playerUserId} not found.");

            var alreadyRegistered = await _unitOfWork.Repository<Domain.Entities.Player.PlayerAcademy>()
                .ExistsAsync(pa => pa.PlayerId == playerUserId && pa.LeftAt == null);

            if (alreadyRegistered)
                throw new ConflictException("Player is already registered to an academy.");

            var playerAcademy = new Domain.Entities.Player.PlayerAcademy
            {
                AcademyId = academyId,
                PlayerId = playerUserId,
                JoinedAt = DateTime.UtcNow,
                Status = Domain.Enums.PlayerAcademyStatus.Active,
                CreatedById = performedByUserId
            };
            

            await _unitOfWork.Repository<Domain.Entities.Player.PlayerAcademy>().AddAsync(playerAcademy);

            // Create an unpaid subscription for the player at this academy
            var subscription = new PlayerSubscription
            {
                PlayerId = playerUserId,
                AcademyId = academyId,
                PaidByUserId = playerUserId,
                Status = SubscriptionStatus.Unpaid,
                CreatedById = performedByUserId
            };

            await _unitOfWork.Repository<PlayerSubscription>().AddAsync(subscription);
            await _unitOfWork.SaveChangesAsync();
        }

        // ──────────────────────────────────────────────────────────────────────
        // RemovePlayerFromAcademyAsync
        // ──────────────────────────────────────────────────────────────────────
        public async Task RemovePlayerFromAcademyAsync(int academyId, int playerUserId, int performedByUserId)
        {
            _logger.LogInformation("User {UserId} removing player {PlayerId} from academy {AcademyId}", performedByUserId, playerUserId, academyId);

            var playerAcademy = await _unitOfWork.Repository<Domain.Entities.Player.PlayerAcademy>()
                .FindAsync(pa => pa.AcademyId == academyId && pa.PlayerId == playerUserId && pa.LeftAt == null);

            if (playerAcademy is null)
                throw new NotFoundException($"Active assignment for player {playerUserId} in academy {academyId} not found.");

            // Cascade: remove the player from all active team assignments within this academy
            var teamIds = await _unitOfWork.Repository<Team>()
                .GetQueryableAsNoTracking()
                .Where(t => t.AcademyId == academyId)
                .Select(t => t.Id)
                .ToListAsync();

            var playerTeamsInAcademy = await _unitOfWork.Repository<Domain.Entities.Player.PlayerTeam>()
                .FindAllAsync(pt => pt.PlayerId == playerUserId && teamIds.Contains(pt.TeamId) && pt.LeftAt == null);

            foreach (var pt in playerTeamsInAcademy)
            {
                pt.LeftAt = DateTime.UtcNow;
                _unitOfWork.Repository<Domain.Entities.Player.PlayerTeam>().SoftDelete(pt);
            }

            playerAcademy.LeftAt = DateTime.UtcNow;
            _unitOfWork.Repository<Domain.Entities.Player.PlayerAcademy>().SoftDelete(playerAcademy);
            await _unitOfWork.SaveChangesAsync();
        }
        // ─── Member Join Requests ──────────────────────────────────────────────

        public async Task<IEnumerable<PlayerSearchResponseDto>> SearchAvailablePlayersAsync(string? name, int academyId)
        {
            var query = _unitOfWork.Repository<Domain.Entities.Player.Player>()
                .GetQueryableAsNoTracking()
                .Where(p => !p.PlayerAcademies.Any(pa => pa.LeftAt == null));

            if (!string.IsNullOrWhiteSpace(name))
            {
                var lowerName = name.ToLower();
                query = query.Where(p => (p.FirstName + " " + p.LastName).ToLower().Contains(lowerName));
            }

            var players = await query.ToListAsync();
            return players.Select(p => new PlayerSearchResponseDto
            {
                PlayerId = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                ImageUrl = p.ProfileImageUrl
            });
        }

        public async Task<IEnumerable<CoachSearchResponseDto>> SearchCoachesAsync(string? name, int academyId)
        {
            // Coach who is not in THIS academy, and doesn't have a pending request to THIS academy.
            var query = _unitOfWork.Repository<Domain.Entities.Coach.Coach>()
                .GetQueryableAsNoTracking()
                .Where(c => !c.CoachAcademies.Any(ca => ca.AcademyId == academyId && ca.LeftAt == null));

            if (!string.IsNullOrWhiteSpace(name))
            {
                var lowerName = name.ToLower();
                query = query.Where(c => (c.FirstName + " " + c.LastName).ToLower().Contains(lowerName));
            }

            var coaches = await query.ToListAsync();
            return coaches.Select(c => new CoachSearchResponseDto
            {
                CoachId = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                ImageUrl = c.ProfileImageUrl
            });
        }

        public async Task SendPlayerJoinRequestAsync(int academyId, int playerId, int adminUserId)
        {
            _logger.LogInformation("Admin {AdminId} sending join request to player {PlayerId} for academy {AcademyId}", adminUserId, playerId, academyId);

            var academy = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>().FindAsync(a => a.Id == academyId);
            if (academy is null) throw new NotFoundException($"Academy {academyId} not found.");

            var player = await _unitOfWork.Repository<Domain.Entities.Player.Player>().FindAsync(p => p.Id == playerId);
            if (player is null) throw new NotFoundException($"Player {playerId} not found.");

            // Check if player is already in an academy
            var alreadyInAcademy = await _unitOfWork.Repository<Domain.Entities.Player.PlayerAcademy>()
                .ExistsAsync(pa => pa.PlayerId == playerId && pa.LeftAt == null);
            if (alreadyInAcademy) throw new ConflictException("Player is already registered to an academy.");

            // Check if pending request exists
            var existingRequest = await _unitOfWork.Repository<AcademyPlayerJoinRequest>()
                .ExistsAsync(r => r.AcademyId == academyId && r.PlayerId == playerId && r.Status == JoinRequestStatus.Pending);
            if (existingRequest) throw new ConflictException("A pending join request already exists for this player.");

            var request = new AcademyPlayerJoinRequest
            {
                AcademyId = academyId,
                PlayerId = playerId,
                Status = JoinRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                CreatedById = adminUserId
            };

            await _unitOfWork.Repository<AcademyPlayerJoinRequest>().AddAsync(request);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task SendCoachJoinRequestAsync(int academyId, int coachId, int adminUserId)
        {
            _logger.LogInformation("Admin {AdminId} sending join request to coach {CoachId} for academy {AcademyId}", adminUserId, coachId, academyId);

            var academy = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>().FindAsync(a => a.Id == academyId);
            if (academy is null) throw new NotFoundException($"Academy {academyId} not found.");

            var coach = await _unitOfWork.Repository<Domain.Entities.Coach.Coach>().FindAsync(c => c.Id == coachId);
            if (coach is null) throw new NotFoundException($"Coach {coachId} not found.");

            // Check if coach is already in THIS academy
            var alreadyInAcademy = await _unitOfWork.Repository<Domain.Entities.Coach.CoachAcademy>()
                .ExistsAsync(ca => ca.AcademyId == academyId && ca.CoachUserId == coachId && ca.LeftAt == null);
            if (alreadyInAcademy) throw new ConflictException("Coach is already registered to this academy.");

            // Check if pending request exists
            var existingRequest = await _unitOfWork.Repository<AcademyCoachJoinRequest>()
                .ExistsAsync(r => r.AcademyId == academyId && r.CoachId == coachId && r.Status == JoinRequestStatus.Pending);
            if (existingRequest) throw new ConflictException("A pending join request already exists for this coach to this academy.");

            var request = new AcademyCoachJoinRequest
            {
                AcademyId = academyId,
                CoachId =   coachId,
                Status = JoinRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                CreatedById = adminUserId
            };

            await _unitOfWork.Repository<AcademyCoachJoinRequest>().AddAsync(request);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task RespondToPlayerJoinRequestAsync(int requestId, JoinRequestStatus status, int playerId)
        {
            _logger.LogInformation("Player {PlayerId} responding with {Status} to request {RequestId}", playerId, status, requestId);

            var request = await _unitOfWork.Repository<AcademyPlayerJoinRequest>().FindAsync(r => r.Id == requestId);
            if (request is null) throw new NotFoundException($"Join request {requestId} not found.");

            if (request.PlayerId != playerId) throw new UnauthorizedAccessException("You can only respond to your own join requests.");
            if (request.Status != JoinRequestStatus.Pending) throw new BadRequestException("This request has already been processed.");

            if (status != JoinRequestStatus.Accepted && status != JoinRequestStatus.Rejected)
                throw new BadRequestException("Invalid status for response.");

            request.Status = status;
            request.RespondedAt = DateTime.UtcNow;
            request.UpdatedById = playerId;

            if (status == JoinRequestStatus.Accepted)
            {
                // Register player
                await RegisterPlayerToAcademyAsync(request.AcademyId, playerId, playerId);
                
                // Cancel other pending requests since player can only be in one academy
                var otherPendingRequests = await _unitOfWork.Repository<AcademyPlayerJoinRequest>()
                    .FindAllAsync(r => r.PlayerId == playerId && r.Status == JoinRequestStatus.Pending && r.Id != requestId);
                
                foreach (var otherReq in otherPendingRequests)
                {
                    otherReq.Status = JoinRequestStatus.Cancelled;
                    otherReq.UpdatedById = playerId;
                    otherReq.UpdatedAt = DateTime.UtcNow;
                }
                
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task RespondToCoachJoinRequestAsync(int requestId, JoinRequestStatus status, int coachId)
        {
            _logger.LogInformation("Coach {CoachId} responding with {Status} to request {RequestId}", coachId, status, requestId);

            var request = await _unitOfWork.Repository<AcademyCoachJoinRequest>().FindAsync(r => r.Id == requestId);
            if (request is null) throw new NotFoundException($"Join request {requestId} not found.");

            if (request.CoachId != coachId) throw new UnauthorizedAccessException("You can only respond to your own join requests.");
            if (request.Status != JoinRequestStatus.Pending) throw new BadRequestException("This request has already been processed.");

            if (status != JoinRequestStatus.Accepted && status != JoinRequestStatus.Rejected)
                throw new BadRequestException("Invalid status for response.");

            request.Status = status;
            request.RespondedAt = DateTime.UtcNow;
            request.UpdatedById = coachId;

            if (status == JoinRequestStatus.Accepted)
            {
                // Register coach
                await RegisterCoachToAcademyAsync(request.AcademyId, coachId, coachId);
            }
            else
            {
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task CancelPlayerJoinRequestAsync(int requestId, int adminUserId)
        {
            _logger.LogInformation("Admin {AdminId} cancelling player join request {RequestId}", adminUserId, requestId);

            var request = await _unitOfWork.Repository<AcademyPlayerJoinRequest>().FindAsync(r => r.Id == requestId);
            if (request is null) throw new NotFoundException($"Join request {requestId} not found.");

            if (request.Status != JoinRequestStatus.Pending) throw new BadRequestException("Only pending requests can be cancelled.");

            request.Status = JoinRequestStatus.Cancelled;
            request.UpdatedById = adminUserId;
            request.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task CancelCoachJoinRequestAsync(int requestId, int adminUserId)
        {
            _logger.LogInformation("Admin {AdminId} cancelling coach join request {RequestId}", adminUserId, requestId);

            var request = await _unitOfWork.Repository<AcademyCoachJoinRequest>().FindAsync(r => r.Id == requestId);
            if (request is null) throw new NotFoundException($"Join request {requestId} not found.");

            if (request.Status != JoinRequestStatus.Pending) throw new BadRequestException("Only pending requests can be cancelled.");

            request.Status = JoinRequestStatus.Cancelled;
            request.UpdatedById = adminUserId;
            request.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<AcademyPlayerJoinRequestResponseDto>> GetPendingPlayerRequestsForAcademyAsync(int academyId)
        {
            var requests = await _unitOfWork.Repository<AcademyPlayerJoinRequest>()
                .GetQueryableAsNoTracking()
                .Include(r => r.Player)
                .Include(r => r.Academy)
                .Where(r => r.AcademyId == academyId && r.Status == JoinRequestStatus.Pending)
                .ToListAsync();

            return _mapper.Map<IEnumerable<AcademyPlayerJoinRequestResponseDto>>(requests);
        }

        public async Task<IEnumerable<AcademyCoachJoinRequestResponseDto>> GetPendingCoachRequestsForAcademyAsync(int academyId)
        {
            var requests = await _unitOfWork.Repository<AcademyCoachJoinRequest>()
                .GetQueryableAsNoTracking()
                .Include(r => r.Coach)
                .Include(r => r.Academy)
                .Where(r => r.AcademyId == academyId && r.Status == JoinRequestStatus.Pending)
                .ToListAsync();

            return _mapper.Map<IEnumerable<AcademyCoachJoinRequestResponseDto>>(requests);
        }

        public async Task<IEnumerable<AcademyPlayerJoinRequestResponseDto>> GetPendingPlayerRequestsForUserAsync(int playerId)
        {
            var requests = await _unitOfWork.Repository<AcademyPlayerJoinRequest>()
                .GetQueryableAsNoTracking()
                .Include(r => r.Player)
                .Include(r => r.Academy)
                .Where(r => r.PlayerId == playerId && r.Status == JoinRequestStatus.Pending)
                .ToListAsync();

            return _mapper.Map<IEnumerable<AcademyPlayerJoinRequestResponseDto>>(requests);
        }

        public async Task<IEnumerable<AcademyCoachJoinRequestResponseDto>> GetPendingCoachRequestsForUserAsync(int coachId)
        {
            var requests = await _unitOfWork.Repository<AcademyCoachJoinRequest>()
                .GetQueryableAsNoTracking()
                .Include(r => r.Coach)
                .Include(r => r.Academy)
                .Where(r => r.CoachId == coachId && r.Status == JoinRequestStatus.Pending)
                .ToListAsync();

            return _mapper.Map<IEnumerable<AcademyCoachJoinRequestResponseDto>>(requests);
        }

        public async Task<Koralytics.Application.DTOs.Common.PagedResponseDto<AcademyMemberResponseDto>> GetAcademyMembersAsync(int academyId, Koralytics.Application.DTOs.Common.PaginationRequestDto request)
        {
            var playersQuery = _unitOfWork.Repository<PlayerAcademy>()
                .GetQueryableAsNoTracking()
                .Where(pa => pa.AcademyId == academyId && pa.LeftAt == null)
                .Select(pa => new AcademyMemberResponseDto
                {
                    UserId = pa.PlayerId,
                    FullName = (pa.Player.FirstName + " " + pa.Player.LastName).Trim(),
                    Email = pa.Player.Email ?? string.Empty,
                    Role = "Player",
                    Position = pa.Player.PlayerPositions.Where(pp => pp.IsPrimary).Select(pp => pp.Position).FirstOrDefault() 
                               ?? pa.Player.PlayerPositions.Select(pp => pp.Position).FirstOrDefault(),
                    SquadStatus = pa.Player.AvailabilityStatus == Domain.Enums.AvailabilityStatus.Available ? "Available" :
                                  pa.Player.AvailabilityStatus == Domain.Enums.AvailabilityStatus.Injured ? "Injured" :
                                  pa.Player.AvailabilityStatus == Domain.Enums.AvailabilityStatus.Resting ? "Resting" :
                                  pa.Player.AvailabilityStatus == Domain.Enums.AvailabilityStatus.Suspended ? "Suspended" : "Available",
                    JoinedAt = pa.JoinedAt
                });

            var coachesQuery = _unitOfWork.Repository<CoachAcademy>()
                .GetQueryableAsNoTracking()
                .Where(ca => ca.AcademyId == academyId && ca.LeftAt == null)
                .Select(ca => new AcademyMemberResponseDto
                {
                    UserId = ca.CoachUserId,
                    FullName = (ca.Coach.FirstName + " " + ca.Coach.LastName).Trim(),
                    Email = ca.Coach.Email ?? string.Empty,
                    Role = "Coach",
                    Position = null,
                    SquadStatus = null,
                    JoinedAt = ca.JoinedAt
                });

            var combinedQuery = playersQuery.Concat(coachesQuery).OrderByDescending(m => m.JoinedAt);

            var totalCount = await combinedQuery.CountAsync();
            var items = await combinedQuery
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new Koralytics.Application.DTOs.Common.PagedResponseDto<AcademyMemberResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        public async Task<Koralytics.Application.DTOs.Common.PagedResponseDto<AcademyAdminResponseDto>> GetAcademyAdminsAsync(int academyId, Koralytics.Application.DTOs.Common.PaginationRequestDto request)
        {
            var academy = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                .GetQueryableAsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == academyId);

            if (academy == null) throw new NotFoundException("Academy not found.");

            var ownerId = academy.AdminUserId;

            var adminsQuery = _unitOfWork.Repository<AcademyAdmin>()
                .GetQueryableAsNoTracking()
                .Where(a => a.AcademyId == academyId || a.Id == ownerId)
                .Select(a => new AcademyAdminResponseDto
                {
                    UserId = a.Id,
                    FullName = (a.FirstName + " " + a.LastName).Trim(),
                    Email = a.Email ?? string.Empty,
                    IsOwner = a.Id == ownerId
                });

            var totalCount = await adminsQuery.CountAsync();
            var items = await adminsQuery
                .OrderByDescending(a => a.IsOwner)
                .ThenBy(a => a.FullName)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new Koralytics.Application.DTOs.Common.PagedResponseDto<AcademyAdminResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        public async Task UpdatePlayerSubscriptionAsync(int academyId, int playerId, Koralytics.Application.DTOs.Academies.UpdatePlayerSubscriptionDto dto, int performedByUserId)
        {
            var isPlayerInAcademy = await _unitOfWork.Repository<PlayerAcademy>()
                .GetQueryable()
                .AnyAsync(pa => pa.AcademyId == academyId && pa.PlayerId == playerId && pa.LeftAt == null);

            if (!isPlayerInAcademy) throw new NotFoundException("Player is not currently active in this academy.");

            var subscription = await _unitOfWork.Repository<PlayerSubscription>()
                .GetQueryable()
                .FirstOrDefaultAsync(s => s.AcademyId == academyId && s.PlayerId == playerId);

            if (subscription == null)
            {
                subscription = new PlayerSubscription
                {
                    AcademyId = academyId,
                    PlayerId = playerId,
                    CreatedById = performedByUserId,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<PlayerSubscription>().AddAsync(subscription);
            }
            
            subscription.Status = dto.Status;
            subscription.GraceUntil = dto.GraceUntil;

            if (dto.Status == SubscriptionStatus.Paid)
            {
                subscription.PaidAt = DateTime.UtcNow;
                subscription.PaidByUserId = performedByUserId; // Admin recorded the payment
            }
            else
            {
                // Unpaid or Grace, clear PaidAt if it was set
                subscription.PaidAt = null;
                // If it was newly created and not paid, we still need a valid PaidByUserId due to non-nullable FK.
                // We will assign the admin who updated it.
                subscription.PaidByUserId = performedByUserId; 
            }

            await _unitOfWork.SaveChangesAsync();
        }
    }
}
