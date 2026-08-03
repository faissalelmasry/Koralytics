namespace Koralytics.Application.DTOs.Player
{
    public class PlayerArchetypeDto
    {
        public int PlayerId { get; set; }
        public string ArchetypePlayerName { get; set; } = string.Empty;
        public string ArchetypeText { get; set; } = string.Empty;
        public DateTime? ArchetypeLastRevealedAt { get; set; }
    }
}
