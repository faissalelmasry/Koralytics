namespace Koralytics.Application.DTOs.Parent
{
    public class ParentChildDto
    {
        public int PlayerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Position { get; set; }
        public string? TeamName { get; set; }
        public string? PhotoUrl { get; set; }
        public string? AcademyTier { get; set; }
        public bool IsEliteTier { get; set; }
    }
}