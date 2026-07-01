using Koralytics.Domain.Models.BaseModels;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Domain.Entities.SystemAdmin
{
    public class PlatformAuditLog : AuditableEntity
    {
        public string Action { get; set; }
        public string TargetEntity { get; set; }
        public int TargetEntityId { get; set; }
        public string? Details { get; set; }
    }

}
