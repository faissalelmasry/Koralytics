using AutoMapper;
using Koralytics.Application.DTOs.Drill;
using Koralytics.Application.Interfaces;
using Koralytics.Domain.Entities.Drill;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DrillSessionEntity = Koralytics.Domain.Entities.Drill.DrillSession;

namespace Koralytics.Application.Services.Drill.DrillResult
{
    public class DrillResultService : IDrillResultService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public DrillResultService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task SubmitResultsAsync(int sessionId, int drillId, SubmitDrillResultsDto dto, int currentCoachId)
        {
            var session = await _unitOfWork.Repository<DrillSessionEntity>().GetByIdAsNoTrackingAsync(sessionId);

            if (session == null)
            {
                throw new KeyNotFoundException($"Drill Session with ID {sessionId} was not found.");
            }

            if (session.CoachId != currentCoachId)
            {
                throw new UnauthorizedAccessException("You can only submit results for your own scheduled sessions.");
            }

            var drill = await _unitOfWork.Repository<Koralytics.Domain.Entities.Drill.Drill>().GetByIdAsNoTrackingAsync(drillId);

            if (drill == null || drill.SessionId != sessionId)
            {
                throw new InvalidOperationException($"Drill with ID {drillId} does not belong to Session {sessionId}.");
            }

            var attendanceSheet = await _unitOfWork.Repository<SessionAttendance>()
                .FindAllAsNoTrackingAsync(sa => sa.SessionId == sessionId);

            var resultRepo = _unitOfWork.Repository<Domain.Entities.Drill.DrillResult>();

            foreach (var incomingScore in dto.Results)
            {
                var playerAttendance = attendanceSheet.FirstOrDefault(sa => sa.playerId == incomingScore.PlayerId);

                if (playerAttendance == null || !playerAttendance.IsPresent)
                {
                    throw new InvalidOperationException($"Player {incomingScore.PlayerId} is not present for this session.");
                }

                decimal finalScoreCalculated = 0;
                if (drill.Mode == Koralytics.Domain.Enums.DrillMode.Manual)
                {
                    finalScoreCalculated = incomingScore.ManualScore ?? 0;
                }
                else if (drill.Mode == Koralytics.Domain.Enums.DrillMode.SuccessOrMissed)
                {
                    int totalAttempts = incomingScore.DoneCount + incomingScore.MissedCount;
                    finalScoreCalculated = totalAttempts > 0 ? ((decimal)incomingScore.DoneCount / totalAttempts) * 10 : 0;
                }

                var existingResult = await resultRepo.FindAsync(r => r.DrillId == drillId && r.PlayerId == incomingScore.PlayerId);

                if (existingResult != null)
                {
                    existingResult.ManualScore = drill.Mode == Koralytics.Domain.Enums.DrillMode.Manual ? incomingScore.ManualScore : null;
                    existingResult.DoneCount = drill.Mode == Koralytics.Domain.Enums.DrillMode.SuccessOrMissed ? incomingScore.DoneCount : 0;
                    existingResult.MissedCount = drill.Mode == Koralytics.Domain.Enums.DrillMode.SuccessOrMissed ? incomingScore.MissedCount : 0;
                    existingResult.FinalScore = Math.Round(finalScoreCalculated, 2);
                    existingResult.CoachNotes = incomingScore.CoachNotes;
                    existingResult.UpdatedById = currentCoachId;
                    existingResult.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var newResult = new Domain.Entities.Drill.DrillResult
                    {
                        DrillId = drillId,
                        PlayerId = incomingScore.PlayerId,
                        ManualScore = drill.Mode == Koralytics.Domain.Enums.DrillMode.Manual ? incomingScore.ManualScore : null,
                        DoneCount = drill.Mode == Koralytics.Domain.Enums.DrillMode.SuccessOrMissed ? incomingScore.DoneCount : 0,
                        MissedCount = drill.Mode == Koralytics.Domain.Enums.DrillMode.SuccessOrMissed ? incomingScore.MissedCount : 0,
                        FinalScore = Math.Round(finalScoreCalculated, 2),
                        CoachNotes = incomingScore.CoachNotes,
                        CreatedById = currentCoachId
                    };
                    await resultRepo.AddAsync(newResult);
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task MarkAttendanceAsync(int sessionId, UpdateSessionAttendanceDto dto, int currentCoachId)
        {
            var session = await _unitOfWork.Repository<DrillSessionEntity>().GetByIdAsNoTrackingAsync(sessionId);

            if (session == null)
            {
                throw new KeyNotFoundException($"Drill Session with ID {sessionId} was not found.");
            }

            if (session.CoachId != currentCoachId)
            {
                throw new UnauthorizedAccessException("You can only modify attendance for your own scheduled sessions.");
            }

            var existingAttendance = await _unitOfWork.Repository<SessionAttendance>()
                .FindAllAsync(sa => sa.SessionId == sessionId);

            foreach (var incomingData in dto.Attendances)
            {
                var recordToUpdate = existingAttendance.FirstOrDefault(sa => sa.playerId == incomingData.PlayerId);

                if (recordToUpdate != null)
                {
                    recordToUpdate.IsPresent = incomingData.IsPresent;
                    recordToUpdate.UpdatedById = currentCoachId;
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<PlayerProgressionDto> GetPlayerDrillProgressionAsync(int playerId, int categoryId, int currentAcademyId)
        {
            var playerExists = await _unitOfWork.Repository<Koralytics.Domain.Entities.Player.Player>()
                .ExistsAsync(p => p.Id == playerId && p.PlayerAcademies.Any(pa => pa.AcademyId == currentAcademyId));

            if (!playerExists)
            {
                throw new UnauthorizedAccessException($"Player with ID {playerId} does not exist or does not belong to your academy.");
            }

            var rawResults = await _unitOfWork.Repository<Domain.Entities.Drill.DrillResult>()
                .GetQueryable()
                .AsNoTracking()
                .Include(dr => dr.Drill)
                    .ThenInclude(d => d.DrillSession)
                .Include(dr => dr.Drill)
                    .ThenInclude(d => d.DrillTemplate)
                        .ThenInclude(t => t.DrillCategory) 
                .Where(dr => dr.PlayerId == playerId && dr.Drill!.DrillTemplate!.CategoryId == categoryId)
                .ToListAsync();

            if (!rawResults.Any())
            {
                return new PlayerProgressionDto
                {
                    PlayerId = playerId,
                    CategoryName = "Unknown",
                    ProgressionChart = new List<ProgressionDataPointDto>()
                };
            }

            string categoryName = rawResults.First().Drill?.DrillTemplate?.DrillCategory?.Name ?? "Unknown";

            var response = new PlayerProgressionDto
            {
                PlayerId = playerId,
                CategoryName = categoryName,
                ProgressionChart = rawResults
                    .Where(r => r.Drill?.DrillSession != null && r.Drill?.DrillTemplate != null)
                    .Select(r => new ProgressionDataPointDto
                    {
                        SessionDate = r.Drill!.DrillSession!.SessionDate,
                        FinalScore = r.FinalScore,
                        DrillName = r.Drill!.DrillTemplate!.Name
                    })
                    .OrderBy(p => p.SessionDate)
                    .ToList()
            };

            return response;
        }

        public async Task<IEnumerable<DrillResultDto>> GetDrillResultsAsync(int sessionId, int drillId, int currentCoachId)
        {
            var sessionExists = await _unitOfWork.Repository<DrillSessionEntity>()
                .ExistsAsync(s => s.Id == sessionId && s.CoachId == currentCoachId);

            if (!sessionExists)
            {
                throw new UnauthorizedAccessException("You do not have permission to view these results.");
            }

            var results = await _unitOfWork.Repository<Domain.Entities.Drill.DrillResult>()
                .FindAllAsNoTrackingAsync(r => r.DrillId == drillId);

            return _mapper.Map<IEnumerable<DrillResultDto>>(results);
        }

        public async Task<IEnumerable<PlayerAttendanceDto>> GetSessionAttendanceAsync(int sessionId, int currentCoachId)
        {
            var sessionExists = await _unitOfWork.Repository<DrillSessionEntity>()
                .ExistsAsync(s => s.Id == sessionId && s.CoachId == currentCoachId);

            if (!sessionExists)
            {
                throw new UnauthorizedAccessException("You do not have permission to view this attendance sheet.");
            }

            var attendance = await _unitOfWork.Repository<SessionAttendance>()
                .FindAllAsNoTrackingAsync(sa => sa.SessionId == sessionId);

            return _mapper.Map<IEnumerable<PlayerAttendanceDto>>(attendance);
        }
    }
}