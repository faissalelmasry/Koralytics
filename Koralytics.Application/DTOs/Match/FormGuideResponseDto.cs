namespace Koralytics.Application.DTOs.Match
{
    public class FormGuideResponseDto
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string FormFormat { get; set; } = string.Empty;
        public List<string> Results { get; set; } = [];
    }
}
