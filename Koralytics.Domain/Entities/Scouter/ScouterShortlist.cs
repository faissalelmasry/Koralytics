using System;
using Koralytics.Domain.Models.BaseModels;

namespace Koralytics.Domain.Entities.Scouter
{
    public class ScouterShortlist : BaseEntity
    {
        public int ScouterUserId { get; set; }
        public int PlayerId { get; set; }
        public DateTime AddedAt { get; set; }

        public Scouter Scouter { get; set; } = null!;
        public Player.Player Player { get; set; } = null!;
    }
}
