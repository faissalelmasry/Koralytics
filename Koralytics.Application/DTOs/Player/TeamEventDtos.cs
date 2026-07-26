namespace Koralytics.Application.DTOs.Player
{
    public class TeamScheduledEventsResponseDto
    {
        public List<TeamScheduledEventDto> Events { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class TeamScheduledEventDto
    {
        public string EventType { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int? MatchId { get; set; }
        public string? MatchType { get; set; }
        public string? HomeTeamName { get; set; }
        public string? AwayTeamName { get; set; }
        public int? SessionId { get; set; }
        public string? SessionType { get; set; }
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
