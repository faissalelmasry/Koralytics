using System;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Models.BaseModels;
using Koralytics.Domain.Entities.Identity;

namespace Koralytics.Domain.Entities.Coach
{
    public class CoachTempAccess : BaseEntity
    {
        public int CoachUserId { get; set; }
        public int GrantedToUserId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public TempAccessAccessLevel AccessLevel { get; set; }
        public TempAccessStatus Status { get; set; }

        public Coach Coach { get; set; } = null!;
        public User GrantedToUser { get; set; } = null!;
    }
}
