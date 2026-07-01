using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Models.BaseModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Domain.Entities.Player
{
    public class PlayerGoal:BaseEntity
    {
        public int PlayerId { get; set; }
        public int AcademyId { get; set; }
        public string Category { get; set; } = string.Empty;
        public decimal TargetScore { get; set; }
        public DateTime Deadline { get; set; }
        public bool Achieved { get; set; }
        public virtual Player Player { get; set; } = null!;
        public virtual Academy.Academy Academy { get; set; } = null!;


    }
}
