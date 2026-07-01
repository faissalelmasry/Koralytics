
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Identity;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Models.BaseModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Domain.Entities.Player
{
    public class PlayerSubscription:BaseEntity
    {
       
        public int PlayerId { get; set; }
        public int AcademyId { get; set; }
        public int PaidByUserId { get; set; }
        public SubscriptionStatus Status { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? GraceUntil { get; set; }
        public virtual Player Player { get; set; } = null!;
        public virtual Academy.Academy  Academy { get; set; } = null!;
        public virtual User PaidByUser { get; set; } = null!;

    }
}
