using Koralytics.Domain.Models.BaseModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Domain.Entities.SystemAdmin
{
    public class SystemAdminUser:BaseEntity
    {
        public int UserId { get; set; }
    }
}
