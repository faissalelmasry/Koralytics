namespace Koralytics.Application.DTOs.Player
{
    public class DrillTimelineDto
    {
        public List<DrillTimelineEvent> Events { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class DrillTimelineEvent
    {
        public DateTime Date { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SessionId { get; set; }
        public string SessionType { get; set; } = string.Empty;
        public string? DrillCategoryName { get; set; }
        public decimal? FinalScore { get; set; }
        public string? DrillNotes { get; set; }
        public string? DrillTemplateName { get; set; }
    }
}
