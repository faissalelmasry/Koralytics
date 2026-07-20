namespace Koralytics.Application.Services.Player.PlayerCardService
{
    public class MiniPlayerCardDto
    {
        public int PlayerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
        public decimal OverallRating { get; set; }
    }
}
