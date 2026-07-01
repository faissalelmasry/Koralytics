using Koralytics.Domain.Models.BaseModels;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Domain.Entities.Drill
{
    public class DrillCategory : AuditableEntity
    {
        public string Name { get; set; }

        // Navigation property to DrillTemplates
        public ICollection<DrillTemplate> DrillTemplates { get; set; } = new HashSet<DrillTemplate>();

    }
}
