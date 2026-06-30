using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Entities.Identity;

namespace Koralytics.Domain.Interfaces
{
    public interface IAuditable:ISoftDelete
    {
        public DateTime? UpdatedAt { get; set; }

        public int? UpdatedById { get; set; }

        public User? UpdatedByUser { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedById { get; set; }
        public User CreatedByUser { get; set; }
    }
}
