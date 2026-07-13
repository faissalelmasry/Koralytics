namespace Koralytics.Application.DTOs.Match
{
    public class SubmitMatchRatingsDto
    {
        public List<SubmitMatchRatingPlayerDto> Ratings { get; set; } = [];
    }

    public class SubmitMatchRatingPlayerDto
    {
        public int PlayerId { get; set; }
        public bool IsMOTM { get; set; }
        public int MinutesPlayed { get; set; }
        public string? CoachNote { get; set; }
        public List<CategoryRatingDto> CategoryRatings { get; set; } = [];
    }

    public class CategoryRatingDto
    {
        public int DrillCategoryId { get; set; }
        public decimal Rating { get; set; }
    }
}
