using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Domain.Models.BaseModels
{
    public class AuditableEntity : BaseEntity
    {
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public int UpdatedById { get; set; }
        public virtual AuditableEntity Auditable { get; set; }
    }
}
