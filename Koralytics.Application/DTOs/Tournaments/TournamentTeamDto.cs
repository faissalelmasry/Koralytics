using Koralytics.Domain.Enums;

namespace Koralytics.Application.DTOs.Tournaments
{
    public class TournamentTeamDto
    {
        public int TournamentTeamId { get; set; }
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string AcademyName { get; set; } = string.Empty;
        public TournamentTeamStatus Status { get; set; }
        public int? SeedNumber { get; set; }
        public DateTime RegisteredAt { get; set; }
    }
}
