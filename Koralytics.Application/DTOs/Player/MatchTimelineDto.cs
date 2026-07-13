namespace Koralytics.Application.DTOs.Player
{
    public class MatchTimelineDto
    {
        public List<MatchTimelineEvent> Events { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class MatchTimelineEvent
    {
        public DateTime Date { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MatchId { get; set; }
        public string MatchType { get; set; } = string.Empty;
        public string? HomeTeamName { get; set; }
        public string? AwayTeamName { get; set; }
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public int? HomePenaltyScore { get; set; }
        public int? AwayPenaltyScore { get; set; }
        public int Goals { get; set; }
        public int Assists { get; set; }
        public int MinutesPlayed { get; set; }
        public bool IsMOTM { get; set; }
        public decimal? Rating { get; set; }
        public string? CoachNote { get; set; }
    }
}
