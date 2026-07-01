using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Models.BaseModels;
using PlayerEntity = Koralytics.Domain.Entities.Player.Player;

namespace Koralytics.Domain.Entities.Tournamet
{
    public class TournamentSquad : AuditableEntity
    {
        
        public int TournamentId { get; set; }
        public int TeamId { get; set; }
        public int PlayerId { get; set; }
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public Tournament Tournament { get; set; } = null!;
        public Team Team { get; set; } = null!;
        public PlayerEntity Player { get; set; } = null!;
    }
}
