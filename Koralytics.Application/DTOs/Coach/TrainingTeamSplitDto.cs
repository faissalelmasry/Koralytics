namespace Koralytics.Application.DTOs.Coach
{
    public class TrainingTeamSplitDto
    {
        public int SessionId { get; set; }
        public List<SquadPlayerDto> TeamA { get; set; } = new();
        public List<SquadPlayerDto> TeamB { get; set; } = new();
    }
}
