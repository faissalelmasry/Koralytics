namespace Koralytics.Application.DTOs.Match
{
    public class LiveMatchScoreUpdateDto
    {
        public int MatchId { get; set; }
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public int? HomePenaltyScore { get; set; }
        public int? AwayPenaltyScore { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class LiveMatchEventUpdateDto
    {
        public int MatchId { get; set; }
        public MatchEventResponseDto Event { get; set; } = null!;
    }
}
