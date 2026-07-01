using System;
using Koralytics.Domain.Models.BaseModels;

namespace Koralytics.Domain.Entities.Player
{
    public class ScouterView : BaseEntity
    {
        public int PlayerId { get; set; }
        public int ScouterId { get; set; }
        public DateTime ViewedAt { get; set; }

        public Player Player { get; set; } = null!;
        public Scouter.Scouter Scouter { get; set; } = null!;
    }
}
