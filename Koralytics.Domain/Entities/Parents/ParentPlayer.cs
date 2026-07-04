using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Models.BaseModels;

namespace Koralytics.Domain.Entities.Parents
{
    public class ParentPlayer : BaseEntity
    {
        public int ParentId { get; set; }
        public int PlayerId { get; set; }

        public Parent Parent { get; set; } = null!;
        public Player.Player Player { get; set; } = null!;
    }
}
