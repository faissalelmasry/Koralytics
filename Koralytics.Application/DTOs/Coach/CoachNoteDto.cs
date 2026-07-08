namespace Koralytics.Application.DTOs.Coach
{
    public class CoachNoteDto
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public string PlayerFullName { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
        public int? SessionId { get; set; }
        public int? MatchId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
