namespace Koralytics.Application.DTOs.Player
{
    public class AchievementTimelineDto
    {
        public List<AchievementTimelineEvent> Events { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class AchievementTimelineEvent
    {
        public DateTime Date { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int AchievementId { get; set; }
        public string AchievementType { get; set; } = string.Empty;
    }
}
