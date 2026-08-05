using Koralytics.Domain.Enums;

namespace Koralytics.Application.DTOs.Tournaments
{
    public class TournamentTeamDto
    {
        public int TournamentTeamId { get; set; }
        public int TournamentId { get; set; }
        public string TournamentName { get; set; } = string.Empty;
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public int? AcademyId { get; set; }
        public string AcademyName { get; set; } = string.Empty;
        public TournamentTeamStatus Status { get; set; }
        public int? SeedNumber { get; set; }
        public DateTime RegisteredAt { get; set; }
    }
}
