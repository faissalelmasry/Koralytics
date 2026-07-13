namespace Koralytics.Application.DTOs.Match
{
    public class MatchResponseDto
    {
        public int Id { get; set; }
        public int HomeTeamId { get; set; }
        public int AwayTeamId { get; set; }
        public string HomeTeamName { get; set; } = string.Empty;
        public string AwayTeamName { get; set; } = string.Empty;
        public int? TournamentId { get; set; }
        public int? SessionId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public DateTime MatchDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public int? HomePenaltyScore { get; set; }
        public int? AwayPenaltyScore { get; set; }
        public int? WinningTeamId { get; set; }
    }
}
