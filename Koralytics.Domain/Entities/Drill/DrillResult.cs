using Koralytics.Domain.Models.BaseModels;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Domain.Entities.Drill
{
    public class DrillResult : AuditableEntity
    {
        public int DrillId { get; set; }
        public Drill? Drill { get; set; }
        public int PlayerId { get; set; }
        public Player.Player? Player { get; set; }

        public decimal? ManualScore { get; set; } // Score achieved in the drill
        public int DoneCount { get; set; } // Number of times the drill was completed
        public int MissedCount { get; set; } // Number of times the drill was missed
        public decimal FinalScore { get; set; } // Final score after calculations
        public string? CoachNotes { get; set; } // Notes specific to this drill result
    }
}
