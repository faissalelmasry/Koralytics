using Koralytics.Domain.Models.BaseModels;
using PlayerEntity = Koralytics.Domain.Entities.Player.Player;

namespace Koralytics.Domain.Entities.Tournamet
{
    public class TournamentHallOfFame : AuditableEntity
    {
        public int TournamentId { get; set; }
        public int PlayerId { get; set; }
        public string AwardType { get; set; } = string.Empty;

        public Tournament Tournament { get; set; } = null!;
        public PlayerEntity Player { get; set; } = null!;
    }
}
