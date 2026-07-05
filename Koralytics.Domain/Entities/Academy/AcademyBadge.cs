using Koralytics.Domain.Models.BaseModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Enums;
namespace Koralytics.Domain.Entities.Academy
{
    public class AcademyBadge:BaseEntity
    {
        public int AcademyId { get; set; }
        public AcademyBadgeType BadgeType { get; set; }
        public DateTime AwardedAt { get; set; }
    }
}
