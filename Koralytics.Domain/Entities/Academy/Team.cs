using Koralytics.Domain.Models.BaseModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Domain.Entities.Academy
{
    public class Team:AuditableEntity
    {
        public int AgeGroupId { get; set; }
        public int AcademyId { get; set; }
        public int LocationId { get; set; }
        public string Name { get; set; }
        public virtual AgeGroup AgeGroup { get; set; } = null!;
        public virtual AcademyLocation Location { get; set; } = null!;

    }
}
