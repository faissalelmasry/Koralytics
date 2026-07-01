using Koralytics.Domain.Entities.Identity;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Models.BaseModels;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Domain.Entities.SystemAdmin
{
    public class AcademyRequest:AuditableEntity
    {
        public int RequestedById { get; set; }
        public User? RequestedBy { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public string AcademyName { get; set; }
        public string ContactPersonName { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string Location { get; set; }


        public AcademyRequestStatus RequestStatus { get; set; } = AcademyRequestStatus.Pending;

        public int ? ReviewedById { get; set; }
        public User? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }

        public string? RejectedReason { get; set; } 
    }
}
