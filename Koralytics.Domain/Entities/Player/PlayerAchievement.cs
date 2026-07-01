using Koralytics.Domain.Models.BaseModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Domain.Entities.Player
{
    public class PlayerAchievement :BaseEntity
    {
       
        public int PlayerId { get; set; }
        public string AchievementType { get; set; } = string.Empty;
        public int? ReferenceId {  get; set; }
        public string? ReferenceType { get; set; }
        public DateTime AwardedAt { get; set; }
        public virtual Player Player { get; set; } = null!;
    }
}
