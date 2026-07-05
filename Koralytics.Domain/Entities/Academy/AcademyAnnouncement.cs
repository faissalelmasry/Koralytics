using Koralytics.Domain.Enums;
using Koralytics.Domain.Models.BaseModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Domain.Entities.Academy
{
    public class AcademyAnnouncement:AuditableEntity
    {
        public int AcademyId { get; set; }
        public int SentByUserId { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public AnnouncementTargetType TargetType { get; set; }
        public int TargetId { get; set; }
        public virtual Academy Academy { get; set; } = null!;
    }
}
