namespace Koralytics.Application.DTOs.Match
{
    public class MatchRatingsResponseDto
    {
        public int MatchId { get; set; }
        public List<MatchPlayerRatingDto> Ratings { get; set; } = [];
    }

    public class MatchPlayerRatingDto
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public int CoachId { get; set; }
        public string CoachName { get; set; } = string.Empty;
        public int Goals { get; set; }
        public int Assists { get; set; }
        public int MinutesPlayed { get; set; }
        public bool IsMOTM { get; set; }
        public string? CoachNote { get; set; }
        public List<CategoryRatingDto> CategoryRatings { get; set; } = [];
    }
}
