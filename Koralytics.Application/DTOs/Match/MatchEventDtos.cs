using Koralytics.Domain.Enums;

namespace Koralytics.Application.DTOs.Match
{
    public class LogMatchEventDto
    {
        public int TeamId { get; set; }
        public int PlayerId { get; set; }
        public int? AssistPlayerId { get; set; }
        public MatchEventType EventType { get; set; }
        public int Minute { get; set; }
    }

    public class LogSessionMatchEventDto
    {
        public int PlayerId { get; set; }
        public int? AssistPlayerId { get; set; }
        public MatchEventType EventType { get; set; }
        public int Minute { get; set; }
        public bool IsHomeSide { get; set; }
    }

    public class MatchEventResponseDto
    {
        public int Id { get; set; }
        public int MatchId { get; set; }
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public int? AssistPlayerId { get; set; }
        public string? AssistPlayerName { get; set; }
        public string EventType { get; set; } = string.Empty;
        public int Minute { get; set; }
        public bool? IsHomeSide { get; set; }
    }

    public class MatchTimelineResponseDto
    {
        public int MatchId { get; set; }
        public List<MatchEventResponseDto> Events { get; set; } = [];
    }
}
