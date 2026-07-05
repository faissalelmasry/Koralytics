using Koralytics.Domain.Models.BaseModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Entities.Identity;

namespace Koralytics.Domain.Entities.Academy
{
    public class RoleAuditLog:AuditableEntity
    {
        public int AcademyId { get; set; }
        public int PerformedByUserId { get; set; }
        public int AffectedUserId { get; set; }
        public RoleAuditAction Action { get; set; }
        public string Details { get; set; } = string.Empty;
        public Academy Academy { get; set; } = null!;
        public User PerformedByUser { get; set; } = null!;
        public User AffectedUser { get; set; } = null!;

    }
}
