using Koralytics.Domain.Enums;
using Koralytics.Domain.Models.BaseModels;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Domain.Entities.Drill
{
    public class Drill : AuditableEntity
    {
        public int SessionId { get; set; }
        public DrillSession? DrillSession { get; set; }
        public int DrillTemplateId { get; set; }
        public DrillTemplate? DrillTemplate { get; set; }
        public DrillMode Mode { get; set; }

        // Difficulty level for this drill in the session, can be different from the template's difficulty level
        public DifficultyLevel DifficultyLevel { get; set; }

        public string? Notes { get; set; } // Notes specific to this drill in the session

        public ICollection<DrillResult> DrillResults { get; set; } = new HashSet<DrillResult>();
    }
}
