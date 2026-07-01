using System;
using Koralytics.Domain.Models.BaseModels;

namespace Koralytics.Domain.Entities.Scouter
{
    public class ScouterFollow : BaseEntity
    {
        public int ScouterUserId { get; set; }
        public int PlayerId { get; set; }
        public DateTime FollowedAt { get; set; }

        public Scouter Scouter { get; set; } = null!;
        public Player.Player Player { get; set; } = null!;
    }
}
