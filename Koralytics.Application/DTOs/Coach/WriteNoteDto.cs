namespace Koralytics.Application.DTOs.Coach
{
    public class WriteNoteDto
    {
        public int PlayerId { get; set; }

        /// <summary>
        /// The note content written by the coach.
        /// </summary>
        public string Note { get; set; } = string.Empty;

        /// <summary>
        /// Whether the note is visible to the player themselves.
        /// </summary>
        public bool IsPublic { get; set; } = false;

        /// <summary>
        /// Optional: link this note to a specific drill session.
        /// </summary>
        public int? SessionId { get; set; }

        /// <summary>
        /// Optional: link this note to a specific match.
        /// </summary>
        public int? MatchId { get; set; }
    }
}
