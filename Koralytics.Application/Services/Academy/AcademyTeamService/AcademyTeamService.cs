using AutoMapper;

using Koralytics.Application.DTOs.Academies;
using Koralytics.Application.Interfaces;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Coach;
using Koralytics.Domain.Exceptions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koralytics.Application.Services.Academy.AcademyTeamService
{
    public class AcademyTeamService : IAcademyTeamService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<AcademyTeamService> _logger;

        public AcademyTeamService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<AcademyTeamService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        // ──────────────────────────────────────────────────────────────────────
        // CreateAgeGroupAsync
        // Validates age range does not overlap any existing group in the academy.
        // ──────────────────────────────────────────────────────────────────────
        public async Task<AgeGroupResponseDto> CreateAgeGroupAsync(int academyId, CreateAgeGroupDto dto, int performedByUserId)
        {
            _logger.LogInformation(
                "User {UserId} creating age group for academy {AcademyId}", performedByUserId, academyId);

            // Academy must exist
            _ = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                .FindAsync(a => a.Id == academyId)
                ?? throw new NotFoundException($"Academy with Id {academyId} not found.");

            // Validate age range is coherent (also enforced in FluentValidation, but guarded here too)
            if (dto.MinAge >= dto.MaxAge)
                throw new BadRequestException("MinAge must be less than MaxAge.");

            // Overlap check: no existing group in this academy may share any age in [MinAge, MaxAge]
            var overlapping = await _unitOfWork.Repository<AgeGroup>()
                .ExistsAsync(ag => ag.AcademyId == academyId && !ag.IsDeleted && ag.MinAge < dto.MaxAge && ag.MaxAge > dto.MinAge);

            if (overlapping)
                throw new ConflictException(
                    $"The age range {dto.MinAge}-{dto.MaxAge} overlaps with an existing age group in this academy.");

            // Name uniqueness within academy
            var nameExists = await _unitOfWork.Repository<AgeGroup>()
                .ExistsAsync(ag => ag.AcademyId == academyId && ag.Name == dto.Name );

            if (nameExists)
                throw new ConflictException(
                    $"An age group named '{dto.Name}' already exists in this academy.");

            var ageGroup = _mapper.Map<AgeGroup>(dto);
            ageGroup.AcademyId = academyId;
            ageGroup.CreatedById = performedByUserId;

            await _unitOfWork.Repository<AgeGroup>().AddAsync(ageGroup);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "AgeGroup '{Name}' (Id={Id}) created for academy {AcademyId}.",
                ageGroup.Name, ageGroup.Id, academyId);

            return _mapper.Map<AgeGroupResponseDto>(ageGroup);
        }

        // ──────────────────────────────────────────────────────────────────────
        // CreateTeamAsync
        // Validates AgeGroup and Location both belong to this academy.
        // ──────────────────────────────────────────────────────────────────────
        public async Task<TeamResponseDto> CreateTeamAsync(int academyId, CreateTeamDto dto, int performedByUserId)
        {
            _logger.LogInformation(
                "User {UserId} creating team for academy {AcademyId}", performedByUserId, academyId);

            // Academy must exist
            _ = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                .FindAsync(a => a.Id == academyId)
                ?? throw new NotFoundException($"Academy with Id {academyId} not found.");

            // AgeGroup must belong to this academy
            var ageGroup = await _unitOfWork.Repository<AgeGroup>()
                .FindAsync(ag => ag.Id == dto.AgeGroupId && ag.AcademyId == academyId);

            if (ageGroup is null)
                throw new NotFoundException(
                    $"AgeGroup {dto.AgeGroupId} not found in academy {academyId}.");

            // Location must belong to this academy
            var location = await _unitOfWork.Repository<AcademyLocation>()
                .FindAsync(l => l.Id == dto.LocationId && l.AcademyId == academyId);

            if (location is null)
                throw new NotFoundException(
                    $"Location {dto.LocationId} not found in academy {academyId}.");

            // Team name uniqueness within the age group
            var nameExists = await _unitOfWork.Repository<Team>()
                .ExistsAsync(t => t.AcademyId == academyId && t.AgeGroupId == dto.AgeGroupId && t.Name == dto.Name );

            if (nameExists)
                throw new ConflictException(
                    $"A team named '{dto.Name}' already exists in this age group.");

            var team = _mapper.Map<Team>(dto);
            team.AcademyId = academyId;
            team.CreatedById = performedByUserId;

            await _unitOfWork.Repository<Team>().AddAsync(team);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Team '{Name}' (Id={Id}) created for academy {AcademyId}.",
                team.Name, team.Id, academyId);

            // Reload with navigation properties for mapping
            var created = await _unitOfWork.Repository<Team>()
                .GetQueryableAsNoTracking()
                .Include(t => t.AgeGroup)
                .Include(t => t.Location)
                .FirstOrDefaultAsync(t => t.Id == team.Id);

            return _mapper.Map<TeamResponseDto>(created!);
        }

        // ──────────────────────────────────────────────────────────────────────
        // AssignCoachToTeamAsync
        // Coach must belong to the same academy as the team (via CoachAcademy).
        // ──────────────────────────────────────────────────────────────────────
        public async Task<CoachTeamAssignmentDto> AssignCoachToTeamAsync(int coachUserId, int teamId, int performedByUserId)
        {
            _logger.LogInformation(
                "User {PerformedBy} assigning coach {CoachId} to team {TeamId}",
                performedByUserId, coachUserId, teamId);

            // Team must exist
            var team = await _unitOfWork.Repository<Team>()
                .GetQueryable()
                .Include(t => t.AgeGroup)
                .Include(t => t.Location)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team is null)
                throw new NotFoundException($"Team with Id {teamId} not found.");

            // Coach must be registered in the same academy (active CoachAcademy record)
            var coachAcademy = await _unitOfWork.Repository<CoachAcademy>()
                .FindAsync(ca => ca.CoachUserId == coachUserId && ca.AcademyId == team.AcademyId && ca.LeftAt == null);
                                 

            if (coachAcademy is null)
                throw new BadRequestException(
                    $"Coach {coachUserId} does not belong to the same academy as team {teamId}.");

            // Prevent duplicate active assignment
            var alreadyAssigned = await _unitOfWork.Repository<CoachTeam>()
                .ExistsAsync(ct => ct.CoachUserId == coachUserId && ct.TeamId == teamId && ct.RemovedAt == null );

            if (alreadyAssigned)
                throw new ConflictException(
                    $"Coach {coachUserId} is already actively assigned to team {teamId}.");

            //team.CoachId = coachUserId;

            var coachTeam = new CoachTeam
            {
                CoachUserId = coachUserId,
                TeamId = teamId,
                AssignedAt = DateTime.UtcNow,
                CreatedById = performedByUserId
            };

            await _unitOfWork.Repository<CoachTeam>().AddAsync(coachTeam);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Coach {CoachId} assigned to team {TeamId} successfully.", coachUserId, teamId);

            // Fetch coach name for response
            var coach = await _unitOfWork.Repository<Domain.Entities.Coach.Coach>()
                .GetQueryableAsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == coachUserId);

            return new CoachTeamAssignmentDto
            {
                CoachId = coachUserId,
                CoachFullName = coach is not null ? $"{coach.FirstName} {coach.LastName}" : string.Empty,
                TeamId = teamId,
                TeamName = team.Name,
                AssignedAt = coachTeam.AssignedAt
            };
        }

        // ──────────────────────────────────────────────────────────────────────
        // RemoveCoachFromTeamAsync
        // Sets CoachTeam.RemovedAt = UtcNow (soft-removal by domain convention).
        // ──────────────────────────────────────────────────────────────────────
        public async Task RemoveCoachFromTeamAsync(int coachUserId, int teamId, int performedByUserId)
        {
            _logger.LogInformation(
                "User {PerformedBy} removing coach {CoachId} from team {TeamId}",
                performedByUserId, coachUserId, teamId);

            var coachTeam = await _unitOfWork.Repository<CoachTeam>()
                .FindAsync(ct => ct.CoachUserId == coachUserId && ct.TeamId == teamId && ct.RemovedAt == null );

            if (coachTeam is null)
                throw new NotFoundException(
                    $"No active assignment found for coach {coachUserId} in team {teamId}.");

            coachTeam.RemovedAt = DateTime.UtcNow;

            _unitOfWork.Repository<CoachTeam>().SoftDelete(coachTeam);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Coach {CoachId} removed from team {TeamId}.", coachUserId, teamId);
        }

        // ──────────────────────────────────────────────────────────────────────
        // GetTeamsByAcademyAsync
        // ──────────────────────────────────────────────────────────────────────
        public async Task<IEnumerable<TeamResponseDto>> GetTeamsByAcademyAsync(int academyId)
        {
            _ = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                .FindAsNoTrackingAsync(a => a.Id == academyId )
                ?? throw new NotFoundException($"Academy with Id {academyId} not found.");

            var teams = await _unitOfWork.Repository<Team>()
                .GetQueryableAsNoTracking()
                .Include(t => t.AgeGroup)
                .Include(t => t.Location)
                //.Include(t=>t.Coach)
                .Where(t => t.AcademyId == academyId )
                .ToListAsync();

            return _mapper.Map<IEnumerable<TeamResponseDto>>(teams);
        }

        

        // ──────────────────────────────────────────────────────────────────────
        // GetAgeGroupsByAcademyAsync
        // ──────────────────────────────────────────────────────────────────────
        public async Task<IEnumerable<AgeGroupResponseDto>> GetAgeGroupsByAcademyAsync(int academyId)
        {
            _ = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                .FindAsNoTrackingAsync(a => a.Id == academyId && !a.IsDeleted)
                ?? throw new NotFoundException($"Academy with Id {academyId} not found.");

            var groups = await _unitOfWork.Repository<AgeGroup>()
                .FindAllAsNoTrackingAsync(ag => ag.AcademyId == academyId && !ag.IsDeleted);

            return _mapper.Map<IEnumerable<AgeGroupResponseDto>>(groups);
        }
    }
}
