using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Entities.Drill;
using Koralytics.Domain.Models.BaseModels;

namespace Koralytics.Domain.Entities.Player
{
    public class PlayerCategoryRating : BaseEntity
    {
        public int PlayerCardId { get; set; }
        public PlayerCard PlayerCard { get; set; } = default!;

        public int DrillCategoryId { get; set; }
        public DrillCategory DrillCategory { get; set; } = default!;

        public decimal Score { get; set; }
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    }

}
