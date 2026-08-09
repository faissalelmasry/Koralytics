using AutoMapper;
using Koralytics.Application.DTOs.Drill;
using Koralytics.Application.Interfaces;
using Koralytics.Domain.Entities.Coach;
using Koralytics.Domain.Entities.Drill;
using Microsoft.EntityFrameworkCore;
using DrillSessionEntity = Koralytics.Domain.Entities.Drill.DrillSession;
using Koralytics.Application.Services.Player.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Koralytics.Application.Services.Drill.DrillSession
{
    public class DrillSessionService : IDrillSessionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly CardInvalidationList _invalidationList;

        public DrillSessionService(IUnitOfWork unitOfWork, IMapper mapper, CardInvalidationList invalidationList)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _invalidationList = invalidationList;
        }

        public async Task<DrillSessionDto> CreateSessionAsync(CreateDrillSessionDto dto, int currentCoachId, int currentAcademyId)
        {
            var isAuthorizedCoach = await _unitOfWork.Repository<CoachTeam>()
                .ExistsAsync(ct =>
                    ct.CoachUserId == currentCoachId &&
                    ct.TeamId == dto.TeamId &&
                    ct.RemovedAt == null);

            if (!isAuthorizedCoach)
            {
                throw new UnauthorizedAccessException("You do not have active permission to schedule a session for this team.");
            }

            var session = _mapper.Map<DrillSessionEntity>(dto);

            session.CoachId = currentCoachId;
            session.AcademyId = currentAcademyId;
            session.CreatedById = currentCoachId;

            foreach (var playerId in dto.PlayerIds)
            {
                session.SessionAttendances.Add(new SessionAttendance
                {
                    playerId = playerId,
                    IsPresent = false,
                    CreatedById = currentCoachId
                });
            }

            await _unitOfWork.Repository<DrillSessionEntity>().AddAsync(session);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<DrillSessionDto>(session);
        }

        public async Task<DrillDto> AddDrillToSessionAsync(int sessionId, AddSessionDrillDto dto, int currentCoachId)
        {
            // 🟢 OPTIMIZED: Anonymous projection. Only pulls the CoachId column instead of the whole row.
            var sessionData = await _unitOfWork.Repository<DrillSessionEntity>()
                .GetQueryableAsNoTracking()
                .Where(s => s.Id == sessionId)
                .Select(s => new { s.CoachId })
                .FirstOrDefaultAsync();

            if (sessionData == null)
            {
                throw new KeyNotFoundException($"Drill Session with ID {sessionId} was not found.");
            }

            if (sessionData.CoachId != currentCoachId)
            {
                throw new UnauthorizedAccessException("You can only add drills to your own scheduled sessions.");
            }

            // 🟢 OPTIMIZED: Anonymous projection. Only pulls Mode and Difficulty.
            var templateData = await _unitOfWork.Repository<Domain.Entities.Drill.DrillTemplate>()
                .GetQueryableAsNoTracking()
                .Where(t => t.Id == dto.DrillTemplateId)
                .Select(t => new { t.DrillMode, t.DifficultyLevel })
                .FirstOrDefaultAsync();

            if (templateData == null)
            {
                throw new KeyNotFoundException($"Drill Template with ID {dto.DrillTemplateId} does not exist.");
            }

            var drill = _mapper.Map<Domain.Entities.Drill.Drill>(dto);

            drill.SessionId = sessionId;
            drill.CreatedById = currentCoachId;

            if (drill.Mode == 0) drill.Mode = templateData.DrillMode;
            if (drill.DifficultyLevel == 0) drill.DifficultyLevel = templateData.DifficultyLevel;

            await _unitOfWork.Repository<Domain.Entities.Drill.Drill>().AddAsync(drill);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<DrillDto>(drill);
        }

        public async Task<Koralytics.Application.Common.PagedResult<DrillSessionDto>> GetCoachSessionsAsync(int currentUserId, string currentUserRole, int currentAcademyId, SessionFilterDto filter)
        {
            var query = _unitOfWork.Repository<DrillSessionEntity>()
                .GetQueryableAsNoTracking()
                .Include(s => s.DrillSessionCoach)
                .Include(s => s.DrillSessionTeam)
                .Where(s => s.AcademyId == currentAcademyId);

            bool isAdmin = string.Equals(currentUserRole, "AcademyAdmin", StringComparison.OrdinalIgnoreCase);

            if (!isAdmin)
            {
                query = query.Where(s => s.CoachId == currentUserId);
            }

            if (filter.TeamId.HasValue)
            {
                query = query.Where(s => s.TeamId == filter.TeamId.Value);
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(s => s.Status == filter.Status.Value);
            }

            if (filter.FromDate.HasValue)
            {
                query = query.Where(s => s.SessionDate >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                query = query.Where(s => s.SessionDate <= filter.ToDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                query = query.Where(s => s.Notes != null && s.Notes.Contains(filter.SearchTerm));
            }

            int totalCount = await query.CountAsync();

            var sessions = await query
                .OrderByDescending(s => s.SessionDate)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<DrillSessionDto>>(sessions).ToList();

            return new Koralytics.Application.Common.PagedResult<DrillSessionDto>
            {
                Items = dtos,
                Page = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<DrillSessionDetailsDto> GetSessionByIdAsync(int sessionId, int currentUserId, string currentUserRole, int currentAcademyId)
        {
            var query = _unitOfWork.Repository<DrillSessionEntity>()
                .GetQueryableAsNoTracking()
                .Include(s => s.DrillSessionTeam)
                .Include(s => s.DrillSessionCoach)
                .Include(s => s.SessionDrills)
                    .ThenInclude(d => d.DrillTemplate)
                        .ThenInclude(dt => dt.DrillCategory)
                .Where(s => s.Id == sessionId && s.AcademyId == currentAcademyId);

            bool isAdmin = string.Equals(currentUserRole, "AcademyAdmin", StringComparison.OrdinalIgnoreCase);

            if (!isAdmin)
            {
                query = query.Where(s => s.CoachId == currentUserId);
            }

            var session = await query.FirstOrDefaultAsync();

            if (session == null)
            {
                throw new KeyNotFoundException($"Session with ID {sessionId} was not found or you do not have permission to access it.");
            }

            return _mapper.Map<DrillSessionDetailsDto>(session);
        }

        public async Task<DrillSessionDto> UpdateSessionAsync(int sessionId, UpdateDrillSessionDto dto, int currentCoachId)
        {
            var session = await _unitOfWork.Repository<DrillSessionEntity>()
                .GetQueryable()
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.CoachId == currentCoachId);

            if (session == null)
            {
                throw new KeyNotFoundException($"Session with ID {sessionId} was not found or you do not have permission to modify it.");
            }

            // 🟢 OPTIMIZED: Standard DateTime empty check
            if (dto.SessionDate != default(DateTime))
            {
                session.SessionDate = dto.SessionDate;
            }

            if (dto.Type != 0)
            {
                session.Type = dto.Type;
            }

            if (dto.Location != null)
            {
                session.Location = dto.Location;
            }

            session.Status = dto.Status;

            if (dto.Notes != null)
            {
                session.Notes = dto.Notes;
            }

            session.UpdatedById = currentCoachId;

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<DrillSessionDto>(session);
        }

        public async Task RemoveDrillFromSessionAsync(int sessionId, int drillId, int currentCoachId)
        {
            var sessionExists = await _unitOfWork.Repository<DrillSessionEntity>()
                .ExistsAsync(s => s.Id == sessionId && s.CoachId == currentCoachId);

            if (!sessionExists)
            {
                throw new UnauthorizedAccessException("You do not have permission to alter this session.");
            }

            var rowsDeleted = await _unitOfWork.Repository<Koralytics.Domain.Entities.Drill.Drill>()
                .GetQueryable()
                .Where(d => d.Id == drillId && d.SessionId == sessionId)
                .ExecuteDeleteAsync();

            if (rowsDeleted == 0)
            {
                throw new KeyNotFoundException($"Drill with ID {drillId} is not attached to Session {sessionId}.");
            }
        }

        public async Task DeleteSessionAsync(int sessionId, int currentCoachId)
        {
            var rowsDeleted = await _unitOfWork.Repository<DrillSessionEntity>()
                .GetQueryable()
                .Where(s => s.Id == sessionId && s.CoachId == currentCoachId)
                .ExecuteDeleteAsync();

            if (rowsDeleted == 0)
            {
                throw new KeyNotFoundException($"Session with ID {sessionId} was not found or you do not have permission to delete it.");
            }
        }

        public async Task CompleteSessionAsync(int sessionId, int currentCoachId)
        {
            var session = await _unitOfWork.Repository<DrillSessionEntity>()
                .GetQueryable()
                .Include(s => s.SessionAttendances)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.CoachId == currentCoachId);

            if (session == null)
            {
                throw new KeyNotFoundException($"Drill Session with ID {sessionId} was not found or you do not have permission.");
            }

            if (session.Status == Domain.Enums.SessionStatus.Completed)
            {
                throw new InvalidOperationException("This session is already marked as completed.");
            }

            session.Status = Koralytics.Domain.Enums.SessionStatus.Completed;
            session.UpdatedById = currentCoachId;

            await _unitOfWork.SaveChangesAsync();

            var presentPlayerIds = session.SessionAttendances
                .Where(sa => sa.IsPresent)
                .Select(sa => sa.playerId)
                .ToList();

            foreach (var playerId in presentPlayerIds)
            {
                _invalidationList.Invalidate(playerId);
            }
        }
    }
}