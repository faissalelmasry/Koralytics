using AutoMapper;
using Koralytics.Application.DTOs.Drill;
using Koralytics.Application.Interfaces;
using Koralytics.Domain.Entities.Coach;
using Koralytics.Domain.Entities.Drill;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DrillSessionEntity = Koralytics.Domain.Entities.Drill.DrillSession;

namespace Koralytics.Application.Services.Drill.DrillSession
{
    public class DrillSessionService : IDrillSessionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

//        public DrillSessionService(IUnitOfWork unitOfWork, IMapper mapper)
//        {
//            _unitOfWork = unitOfWork;
//            _mapper = mapper;
//        }

        public async Task<DrillSessionDto> CreateSessionAsync(CreateDrillSessionDto dto, int currentCoachId, int currentAcademyId)
        {
            var isAuthorizedCoach = await _unitOfWork.Repository<CoachTeam>()
                .ExistsAsync(ct =>
                    ct.CoachUserId == currentCoachId &&
                    ct.TeamId == dto.TeamId &&
                    ct.RemovedAt == null);

//            if (!isAuthorizedCoach)
//            {
//                throw new UnauthorizedAccessException("You do not have active permission to schedule a session for this team.");
//            }

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

//            return _mapper.Map<DrillSessionDto>(session);
//        }

        public async Task<DrillDto> AddDrillToSessionAsync(int sessionId, AddSessionDrillDto dto, int currentCoachId)
        {
            var session = await _unitOfWork.Repository<DrillSessionEntity>().GetByIdAsNoTrackingAsync(sessionId);

//            if (session == null)
//            {
//                throw new KeyNotFoundException($"Drill Session with ID {sessionId} was not found.");
//            }

//            if (session.CoachId != currentCoachId)
//            {
//                throw new UnauthorizedAccessException("You can only add drills to your own scheduled sessions.");
//            }

            var templateExists = await _unitOfWork.Repository<Koralytics.Domain.Entities.Drill.DrillTemplate>()
                .ExistsAsync(t => t.Id == dto.DrillTemplateId);

//            if (!templateExists)
//            {
//                throw new KeyNotFoundException($"Drill Template with ID {dto.DrillTemplateId} does not exist.");
//            }

            var drill = _mapper.Map<Koralytics.Domain.Entities.Drill.Drill>(dto);

//            drill.SessionId = sessionId;
//            drill.CreatedById = currentCoachId;

//            await _unitOfWork.Repository<Koralytics.Domain.Entities.Drill.Drill>().AddAsync(drill);
//            await _unitOfWork.SaveChangesAsync();

//            return _mapper.Map<DrillDto>(drill);
//        }

        public async Task<IEnumerable<DrillSessionDto>> GetCoachSessionsAsync(int currentCoachId, int currentAcademyId, SessionFilterDto filter)
        {
            var query = _unitOfWork.Repository<DrillSessionEntity>()
                .GetQueryable()
                .AsNoTracking()
                .Where(s => s.CoachId == currentCoachId && s.AcademyId == currentAcademyId);

            if (filter.TeamId.HasValue)
            {
                query = query.Where(s => s.TeamId == filter.TeamId.Value);
            }

            // --- MODIFIED FIX: Filter by the new Status property ---
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

            var sessions = await query
                .OrderByDescending(s => s.SessionDate)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return _mapper.Map<IEnumerable<DrillSessionDto>>(sessions);
        }

        public async Task<DrillSessionDetailsDto> GetSessionByIdAsync(int sessionId, int currentCoachId)
        {
            var session = await _unitOfWork.Repository<DrillSessionEntity>()
                .GetQueryable()
                .Include(s => s.SessionDrills)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.CoachId == currentCoachId);

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

            session.SessionDate = dto.SessionDate;

            session.Type = dto.Type;
            session.Status = dto.Status;

            session.Notes = dto.Notes;
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
    }
}