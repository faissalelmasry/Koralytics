using System;

namespace Koralytics.Application.DTOs.AI
{
    public class AIReportDto
    {
        public int TournamentId { get; set; }
        public string ReportText { get; set; } = string.Empty;
        public bool IsPending { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? GeneratedAt { get; set; }

        // Structured statistics for dashboard visualization
        public string? WinnerTeamName { get; set; }
        public int? WinnerTeamId { get; set; }
        public string? BestPlayerName { get; set; }
        public int? BestPlayerId { get; set; }
        public string? TopScorerName { get; set; }
        public int? TopScorerId { get; set; }
        public int TopScorerGoals { get; set; }
        public string? TopAssisterName { get; set; }
        public int? TopAssisterId { get; set; }
        public int TopAssisterAssists { get; set; }
        public string? MostScoredClubName { get; set; }
        public int MostScoredClubGoals { get; set; }
        public string? MostConcededClubName { get; set; }
        public int MostConcededClubGoals { get; set; }
        public string? LeastScoredClubName { get; set; }
        public int LeastScoredClubGoals { get; set; }
    }
}
