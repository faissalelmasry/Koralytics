namespace Koralytics.Application.DTOs.Coach
{
    public class TempAccessDto
    {
        public int Id { get; set; }
        public int CoachUserId { get; set; }
        public int GrantedToUserId { get; set; }
        public string GrantedToFullName { get; set; } = string.Empty;
        public string AccessLevel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
