namespace Koralytics.Application.DTOs.Coach
{
    public class SquadOverviewDto
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public List<SquadPlayerDto> Players { get; set; } = new();
    }
}
