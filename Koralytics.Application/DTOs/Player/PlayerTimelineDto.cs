namespace Koralytics.Application.DTOs.Player
{
    public class PlayerTimelineDto
    {
        public List<TimelineEvent> Events { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class TimelineEvent
    {
        public DateTime Date { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        public int? MatchId { get; set; }
        public string? MatchType { get; set; }
        public string? HomeTeamName { get; set; }
        public string? AwayTeamName { get; set; }
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public int? Goals { get; set; }
        public int? Assists { get; set; }
        public int? MinutesPlayed { get; set; }
        public bool IsMOTM { get; set; }
        public decimal? Rating { get; set; }
        public string? CoachNote { get; set; }

        public int? SessionId { get; set; }
        public string? SessionType { get; set; }
        public string? DrillCategoryName { get; set; }
        public decimal? FinalScore { get; set; }
        public string? DrillNotes { get; set; }
        public string? DrillTemplateName { get; set; }

        public int? AchievementId { get; set; }
        public string? AchievementType { get; set; }
    }
}
