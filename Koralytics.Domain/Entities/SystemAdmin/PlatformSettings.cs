using Koralytics.Domain.Models.BaseModels;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Domain.Entities.SystemAdmin
{
    public class PlatformSettings: AuditableEntity
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string? Description { get; set; }
    }
}
