using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Entities.Identity;
using Koralytics.Domain.Interfaces;

namespace Koralytics.Domain.Models.BaseModels
{
    public abstract class BaseEntity:ISoftDelete
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedById { get; set; }
        public User? CreatedByUser { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
