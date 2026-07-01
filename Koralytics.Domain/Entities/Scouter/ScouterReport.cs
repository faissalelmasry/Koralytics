using System;
using Koralytics.Domain.Models.BaseModels;

namespace Koralytics.Domain.Entities.Scouter
{
    public class ScouterReport : BaseEntity
    {
        public int ScouterUserId { get; set; }
        public int PlayerId { get; set; }
        public string ReportText { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }

        public Scouter Scouter { get; set; } = null!;
        public Player.Player Player { get; set; } = null!;
    }
}
