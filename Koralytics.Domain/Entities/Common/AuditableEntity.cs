using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Entities.Identity;

namespace Koralytics.Domain.Models.BaseModels
{
    public abstract class AuditableEntity : BaseEntity
    {
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public int? UpdatedById { get; set; }
        public User? UpdatedByUser { get; set; }

    }
}
