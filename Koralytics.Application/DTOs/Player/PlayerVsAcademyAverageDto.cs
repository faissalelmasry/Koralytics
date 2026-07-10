namespace Koralytics.Application.DTOs.Player
{
    public class PlayerVsAcademyAverageDto
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public int AcademyId { get; set; }
        public string AcademyName { get; set; } = string.Empty;
        public string? AgeGroupName { get; set; }
        public List<CategoryComparison> Categories { get; set; } = [];
    }

    public class CategoryComparison
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal PlayerAverage { get; set; }
        public decimal AcademyAverage { get; set; }
        public decimal Difference { get; set; }
    }
}
