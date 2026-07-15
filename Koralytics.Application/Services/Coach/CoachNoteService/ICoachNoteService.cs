using Koralytics.Application.DTOs.Coach;

namespace Koralytics.Application.Services.Coach.CoachNoteService
{
    public interface ICoachNoteService
    {
        /// <summary>
        /// Writes a note about a player. Validates the player belongs to one of
        /// the coach's active teams. Optionally linked to a session or match.
        /// </summary>
        Task<CoachNoteDto> WriteNoteAsync(int coachId, int academyId, WriteNoteDto dto);

        /// <summary>
        /// Returns all notes written by this coach about a specific player,
        /// ordered by creation date descending (newest first).
        /// </summary>
        Task<IEnumerable<CoachNoteDto>> GetPlayerNotesAsync(int coachId, int playerId);
    }
}
