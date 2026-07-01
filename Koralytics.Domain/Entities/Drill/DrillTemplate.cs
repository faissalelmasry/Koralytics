using Koralytics.Domain.Enums;
using Koralytics.Domain.Models.BaseModels;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Domain.Entities.Drill
{
    public class DrillTemplate : AuditableEntity
    {
        // FK to DrillCategory
        public int CategoryId { get; set; }
        // Navigation property to DrillCategory
        public DrillCategory DrillCategory { get; set; }


        public int? AcademyId { get; set; } // null=system wide
        // Navigation property to Academy
        public Academy.Academy? DrillTemplateAcademy { get; set; }

        public string Name { get; set; } 


        public DifficultyLevel DifficultyLevel { get; set; }


        public DrillMode DrillMode { get; set; }

        public bool IsShared { get; set; } = false; // Indicates if the drill template is shared with other academies

        public ICollection<Drill> TemplateDrills { get; set; } = new HashSet<Drill>();
    }
}
