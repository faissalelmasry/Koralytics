namespace Koralytics.Application.DTOs.Parent
{
    public class PlayerParentDto
    {
        public int ParentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? PhotoUrl { get; set; }
    }
}
