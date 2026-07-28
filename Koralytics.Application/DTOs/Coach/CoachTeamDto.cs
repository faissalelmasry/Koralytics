namespace Koralytics.Application.DTOs.Coach
{
    public class CoachTeamDto
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string AgeGroupName { get; set; } = string.Empty;
        public int AcademyId { get; set; }
    }
}
