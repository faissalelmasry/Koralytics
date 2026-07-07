//using AutoMapper;
//using Koralytics.Application.DTOs.Drill;
//using Koralytics.Application.Interfaces;
//using Koralytics.Domain.Entities.Coach;
//using Koralytics.Domain.Entities.Drill;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using DrillSessionEntity = Koralytics.Domain.Entities.Drill.DrillSession;

//namespace Koralytics.Application.Services.Drills.DrillSession
//{
//    public class DrillSessionService : IDrillSessionService
//    {
//        private readonly IUnitOfWork _unitOfWork;
//        private readonly IMapper _mapper;

//        public DrillSessionService(IUnitOfWork unitOfWork, IMapper mapper)
//        {
//            _unitOfWork = unitOfWork;
//            _mapper = mapper;
//        }

//        public async Task<DrillSessionDto> CreateSessionAsync(CreateDrillSessionDto dto, int currentCoachId, int currentAcademyId)
//        {
//            // 1. SECURE VALIDATION: Does this coach ACTUALLY and CURRENTLY manage this team?
//            var isAuthorizedCoach = await _unitOfWork.Repository<CoachTeam>()
//                .ExistsAsync(ct =>
//                    ct.CoachUserId == currentCoachId &&
//                    ct.TeamId == dto.TeamId &&
//                    ct.RemovedAt == null);

//            if (!isAuthorizedCoach)
//            {
//                throw new UnauthorizedAccessException("You do not have active permission to schedule a session for this team.");
//            }

//            // 2. MAP THE SESSION
//            var session = _mapper.Map<DrillSessionEntity>(dto);

//            // Overwrite with secure JWT values
//            session.CoachId = currentCoachId;
//            session.AcademyId = currentAcademyId;
//            session.CreatedById = currentCoachId;

//            // 3. GENERATE ATTENDANCE SHEET IN MEMORY
//            foreach (var playerId in dto.PlayerIds)
//            {
//                session.SessionAttendances.Add(new SessionAttendance
//                {
//                    playerId = playerId,
//                    IsPresent = false,
//                    CreatedById = currentCoachId
//                });
//            }

//            // 4. BLAZING FAST TRANSACTION
//            await _unitOfWork.Repository<DrillSessionEntity>().AddAsync(session);
//            await _unitOfWork.SaveChangesAsync();

//            return _mapper.Map<DrillSessionDto>(session);
//        }

//        public async Task<DrillDto> AddDrillToSessionAsync(int sessionId, AddSessionDrillDto dto, int currentCoachId)
//        {
//            // 1. SECURE VALIDATION: Verify the session exists and the coach owns it
//            var session = await _unitOfWork.Repository<DrillSessionEntity>().GetByIdAsNoTrackingAsync(sessionId);

//            if (session == null)
//            {
//                throw new KeyNotFoundException($"Drill Session with ID {sessionId} was not found.");
//            }

//            if (session.CoachId != currentCoachId)
//            {
//                throw new UnauthorizedAccessException("You can only add drills to your own scheduled sessions.");
//            }

//            // 2. TEMPLATE VALIDATION: Ensure the template exists
//            var templateExists = await _unitOfWork.Repository<Koralytics.Domain.Entities.Drill.DrillTemplate>()
//                .ExistsAsync(t => t.Id == dto.DrillTemplateId);

//            if (!templateExists)
//            {
//                throw new KeyNotFoundException($"Drill Template with ID {dto.DrillTemplateId} does not exist.");
//            }

//            // 3. MAP AND SAVE THE DRILL
//            var drill = _mapper.Map<Koralytics.Domain.Entities.Drill.Drill>(dto);

//            drill.SessionId = sessionId;
//            drill.CreatedById = currentCoachId;

//            await _unitOfWork.Repository<Koralytics.Domain.Entities.Drill.Drill>().AddAsync(drill);
//            await _unitOfWork.SaveChangesAsync();

//            return _mapper.Map<DrillDto>(drill);
//        }

//        public async Task<IEnumerable<DrillSessionDto>> GetCoachSessionsAsync(int currentCoachId, int currentAcademyId)
//        {
//            var sessions = await _unitOfWork.Repository<DrillSessionEntity>()
//                .FindAllAsNoTrackingAsync(s =>
//                    s.CoachId == currentCoachId &&
//                    s.AcademyId == currentAcademyId);

//            return _mapper.Map<IEnumerable<DrillSessionDto>>(sessions);
//        }
//    }
//}