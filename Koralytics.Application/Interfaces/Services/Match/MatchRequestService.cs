using AutoMapper;
using Koralytics.Application.DTOs.Match;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Match;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Coach;
using Koralytics.Domain.Entities.Match;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AcademyEntity = Koralytics.Domain.Entities.Academy.Academy;

namespace Koralytics.Application.Services.Match
{
    public class MatchRequestService : IMatchRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<MatchRequestService> _logger;
        private readonly IMatchService _matchService;

        public MatchRequestService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<MatchRequestService> logger,
            IMatchService matchService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _matchService = matchService;
        }

        public async Task<MatchRequestResponseDto> RequestFriendlyMatchAsync(int coachId, CreateMatchRequestDto dto)
        {
            _logger.LogInformation("Coach {CoachId} requesting friendly match: Team {RequesterTeamId} vs Team {TargetTeamId}",
                coachId, dto.RequesterTeamId, dto.TargetTeamId);

            if (dto.RequesterTeamId == dto.TargetTeamId)
                throw new BadRequestException("Requester and target teams cannot be the same.");

            var requesterTeam = await _unitOfWork.Repository<Team>()
                .FindAsNoTrackingAsync(t => t.Id == dto.RequesterTeamId);

            if (requesterTeam is null)
                throw new NotFoundException($"Requester team with Id {dto.RequesterTeamId} not found");

            var targetTeam = await _unitOfWork.Repository<Team>()
                .FindAsNoTrackingAsync(t => t.Id == dto.TargetTeamId);

            if (targetTeam is null)
                throw new NotFoundException($"Target team with Id {dto.TargetTeamId} not found");

            var isCoachOfTeam = await _unitOfWork.Repository<CoachTeam>()
                .ExistsAsync(ct => ct.CoachUserId == coachId
                    && ct.TeamId == dto.RequesterTeamId
                    && ct.RemovedAt == null);

            var isAdminOfRequesterAcademy = await _unitOfWork.Repository<AcademyEntity>()
                .ExistsAsync(a => a.AdminUserId == coachId
                    && a.Id == requesterTeam.AcademyId);

            if (!isCoachOfTeam && !isAdminOfRequesterAcademy)
                throw new ForbiddenException("You must be a coach of the requester team or the academy admin.");

            var existingPending = await _unitOfWork.Repository<MatchRequest>()
                .ExistsAsync(r => r.RequesterTeamId == dto.RequesterTeamId
                    && r.TargetTeamId == dto.TargetTeamId
                    && r.Status == MatchRequestStatus.Pending);

            if (existingPending)
                throw new BadRequestException("A pending request already exists between these teams.");

            var request = new MatchRequest
            {
                RequesterTeamId = dto.RequesterTeamId,
                TargetTeamId = dto.TargetTeamId,
                RequesterCoachId = coachId,
                Format = dto.Format,
                ProposedDate = dto.ProposedDate,
                Location = dto.Location,
                Status = MatchRequestStatus.Pending
            };

            await _unitOfWork.Repository<MatchRequest>().AddAsync(request);
            await _unitOfWork.SaveChangesAsync();

            var created = await _unitOfWork.Repository<MatchRequest>()
                .GetQueryableAsNoTracking()
                .Include(r => r.RequesterTeam)
                .Include(r => r.TargetTeam)
                .Include(r => r.RequesterCoach)
                .FirstOrDefaultAsync(r => r.Id == request.Id);

            _logger.LogInformation("Match request {RequestId} created: Team {A} -> Team {B}",
                request.Id, dto.RequesterTeamId, dto.TargetTeamId);

            return _mapper.Map<MatchRequestResponseDto>(created!);
        }

        public async Task<MatchResponseDto> AcceptMatchRequestAsync(int requestId, int coachId)
        {
            _logger.LogInformation("Coach {CoachId} accepting match request {RequestId}", coachId, requestId);

            var request = await _unitOfWork.Repository<MatchRequest>()
                .GetQueryable()
                .Include(r => r.RequesterTeam)
                .Include(r => r.TargetTeam)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request is null)
                throw new NotFoundException($"Match request with Id {requestId} not found");

            if (request.Status != MatchRequestStatus.Pending)
                throw new BadRequestException("Only pending requests can be accepted.");

            var targetTeam = await _unitOfWork.Repository<Team>()
                .FindAsNoTrackingAsync(t => t.Id == request.TargetTeamId);

            if (targetTeam is null)
                throw new NotFoundException($"Target team with Id {request.TargetTeamId} not found");

            var isCoachOfTargetTeam = await _unitOfWork.Repository<CoachTeam>()
                .ExistsAsync(ct => ct.CoachUserId == coachId
                    && ct.TeamId == request.TargetTeamId
                    && ct.RemovedAt == null);

            var isAdminOfTargetAcademy = await _unitOfWork.Repository<AcademyEntity>()
                .ExistsAsync(a => a.AdminUserId == coachId
                    && a.Id == targetTeam.AcademyId);

            if (!isCoachOfTargetTeam && !isAdminOfTargetAcademy)
                throw new ForbiddenException("You must be a coach of the target team or the target academy admin.");

            var matchDto = new CreateFriendlyMatchDto
            {
                HomeTeamId = request.RequesterTeamId,
                AwayTeamId = request.TargetTeamId,
                Format = request.Format,
                MatchDate = request.ProposedDate,
                Location = request.Location
            };

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var match = await _matchService.CreateFriendlyMatchAsync(matchDto);

                request.Status = MatchRequestStatus.Accepted;
                request.ResolvedByCoachId = coachId;
                request.ResolvedAt = DateTime.UtcNow;
                request.MatchId = match.Id;

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Match request {RequestId} accepted, match {MatchId} created",
                    requestId, match.Id);

                return match;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeclineMatchRequestAsync(int requestId, int coachId)
        {
            _logger.LogInformation("Coach {CoachId} declining match request {RequestId}", coachId, requestId);

            var request = await _unitOfWork.Repository<MatchRequest>()
                .GetQueryable()
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request is null)
                throw new NotFoundException($"Match request with Id {requestId} not found");

            if (request.Status != MatchRequestStatus.Pending)
                throw new BadRequestException("Only pending requests can be declined.");

            var targetTeam = await _unitOfWork.Repository<Team>()
                .FindAsNoTrackingAsync(t => t.Id == request.TargetTeamId);

            if (targetTeam is null)
                throw new NotFoundException($"Target team with Id {request.TargetTeamId} not found");

            var isCoachOfTargetTeam = await _unitOfWork.Repository<CoachTeam>()
                .ExistsAsync(ct => ct.CoachUserId == coachId
                    && ct.TeamId == request.TargetTeamId
                    && ct.RemovedAt == null);

            var isAdminOfTargetAcademy = await _unitOfWork.Repository<AcademyEntity>()
                .ExistsAsync(a => a.AdminUserId == coachId
                    && a.Id == targetTeam.AcademyId);

            if (!isCoachOfTargetTeam && !isAdminOfTargetAcademy)
                throw new ForbiddenException("You must be a coach of the target team or the target academy admin.");

            request.Status = MatchRequestStatus.Declined;
            request.ResolvedByCoachId = coachId;
            request.ResolvedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Match request {RequestId} declined", requestId);
        }

        public async Task<List<MatchRequestResponseDto>> GetPendingRequestsAsync(int teamId)
        {
            _logger.LogInformation("Fetching pending match requests for team {TeamId}", teamId);

            var teamExists = await _unitOfWork.Repository<Team>()
                .ExistsAsync(t => t.Id == teamId);

            if (!teamExists)
                throw new NotFoundException($"Team with Id {teamId} not found");

            var requests = await _unitOfWork.Repository<MatchRequest>()
                .GetQueryableAsNoTracking()
                .Include(r => r.RequesterTeam)
                .Include(r => r.TargetTeam)
                .Include(r => r.RequesterCoach)
                .Include(r => r.ResolvedByCoach)
                .Where(r => r.TargetTeamId == teamId && r.Status == MatchRequestStatus.Pending)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return _mapper.Map<List<MatchRequestResponseDto>>(requests);
        }

        public async Task<List<MatchRequestResponseDto>> GetSentRequestsAsync(int teamId)
        {
            _logger.LogInformation("Fetching sent match requests from team {TeamId}", teamId);

            var teamExists = await _unitOfWork.Repository<Team>()
                .ExistsAsync(t => t.Id == teamId);

            if (!teamExists)
                throw new NotFoundException($"Team with Id {teamId} not found");

            var requests = await _unitOfWork.Repository<MatchRequest>()
                .GetQueryableAsNoTracking()
                .Include(r => r.RequesterTeam)
                .Include(r => r.TargetTeam)
                .Include(r => r.RequesterCoach)
                .Include(r => r.ResolvedByCoach)
                .Where(r => r.RequesterTeamId == teamId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return _mapper.Map<List<MatchRequestResponseDto>>(requests);
        }
    }
}
