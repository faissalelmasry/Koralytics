using Koralytics.Application.Common;
using Koralytics.Application.DTOs.Coach;
using Koralytics.Application.Interfaces;
using Koralytics.Domain.Entities.Coach;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using CoachNoteEntity = Koralytics.Domain.Entities.Coach.CoachNote;
using PlayerEntity = Koralytics.Domain.Entities.Player.Player;
using CoachTeamEntity = Koralytics.Domain.Entities.Coach.CoachTeam;

namespace Koralytics.Application.Services.Coach.CoachNoteService
{
    public class CoachNoteService : ICoachNoteService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CoachNoteService> _logger;

        public CoachNoteService(IUnitOfWork unitOfWork, ILogger<CoachNoteService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // WriteNoteAsync
        // ─────────────────────────────────────────────────────────────────────
        public async Task<CoachNoteDto> WriteNoteAsync(int coachId, int academyId, WriteNoteDto dto)
        {
            _logger.LogInformation(
                "Coach {CoachId} writing note for player {PlayerId}", coachId, dto.PlayerId);

            if (string.IsNullOrWhiteSpace(dto.Note))
                throw new BadRequestException("Note content cannot be empty.");

            // 1. Validate player exists
            var player = await _unitOfWork.Repository<PlayerEntity>()
                .FindAsync(p => p.Id == dto.PlayerId)
                ?? throw new NotFoundException($"Player with Id {dto.PlayerId} not found.");

            // 2. Validate the player belongs to at least one of the coach's active teams
            //    Single query: JOIN CoachTeam → PlayerTeam to avoid two round-trips.
            var playerBelongsToCoach = await _unitOfWork.Repository<CoachTeamEntity>()
                .GetQueryableAsNoTracking()
                .Where(ct => ct.CoachUserId == coachId && ct.RemovedAt == null)
                .Join(
                    _unitOfWork.Repository<PlayerTeam>().GetQueryableAsNoTracking(),
                    ct => ct.TeamId,
                    pt => pt.TeamId,
                    (ct, pt) => pt)
                .AnyAsync(pt => pt.PlayerId == dto.PlayerId && pt.LeftAt == null);

            if (!playerBelongsToCoach)
                throw new ForbiddenException(
                    $"Player {dto.PlayerId} does not belong to any of coach {coachId}'s active teams.");

            // 3. Create and persist the note
            var note = new CoachNoteEntity
            {
                CoachUserId = coachId,
                PlayerId = dto.PlayerId,
                AcademyId = academyId,
                Note = dto.Note.Trim(),
                IsPublic = dto.IsPublic,
                SessionId = dto.SessionId,
                MatchId = dto.MatchId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<CoachNoteEntity>().AddAsync(note);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Note {NoteId} written by coach {CoachId} for player {PlayerId}",
                note.Id, coachId, dto.PlayerId);

            return MapToDto(note, player);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GetPlayerNotesAsync
        // ─────────────────────────────────────────────────────────────────────
        public async Task<PagedResult<CoachNoteDto>> GetPlayerNotesAsync(
            int coachId, int playerId, int page = 1, int pageSize = 20)
        {
            _logger.LogInformation(
                "Coach {CoachId} fetching notes for player {PlayerId} (page {Page}, size {PageSize})",
                coachId, playerId, page, pageSize);

            // Validate player exists
            var playerExists = await _unitOfWork.Repository<PlayerEntity>()
                .ExistsAsync(p => p.Id == playerId);

            if (!playerExists)
                throw new NotFoundException($"Player with Id {playerId} not found.");

            var query = _unitOfWork.Repository<CoachNoteEntity>()
                .GetQueryableAsNoTracking()
                .Where(n => n.CoachUserId == coachId && n.PlayerId == playerId)
                .Include(n => n.Player)
                .OrderByDescending(n => n.CreatedAt);

            var totalCount = await query.CountAsync();

            var notes = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<CoachNoteDto>
            {
                Items     = notes.Select(n => MapToDto(n, n.Player)).ToList(),
                Page      = page,
                PageSize  = pageSize,
                TotalCount = totalCount
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private helper
        // ─────────────────────────────────────────────────────────────────────
        private static CoachNoteDto MapToDto(CoachNoteEntity note, PlayerEntity player) =>
            new()
            {
                Id = note.Id,
                PlayerId = note.PlayerId,
                PlayerFullName = $"{player.FirstName} {player.LastName}",
                Note = note.Note,
                IsPublic = note.IsPublic,
                SessionId = note.SessionId,
                MatchId = note.MatchId,
                CreatedAt = note.CreatedAt
            };
    }
}
