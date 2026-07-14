namespace Koralytics.Application.DTOs.Player
{
    public class PlayerGoalDto
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public int AcademyId { get; set; }
        public string Category { get; set; } = string.Empty;
        public decimal TargetScore { get; set; }
        public DateTime Deadline { get; set; }
        public bool Achieved { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreatePlayerGoalDto
    {
        public int PlayerId { get; set; }
        public int AcademyId { get; set; }
        public string Category { get; set; } = string.Empty;
        public decimal TargetScore { get; set; }
        public DateTime Deadline { get; set; }
    }

    public class UpdatePlayerGoalDto
    {
        public bool Achieved { get; set; }
    }
}
