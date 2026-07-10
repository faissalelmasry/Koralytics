using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Models.BaseModels;

namespace Koralytics.Domain.Entities.Player
{
    public class PlayerCard : BaseEntity
    {
        public int PlayerId { get; set; }
        public Player Player { get; set; } = default!;

        public decimal OverallRating { get; set; }
        public decimal OverallTrainingAvg { get; set; }
        public decimal OverallTournamentAvg { get; set; }
        public bool NeedsRecalculation { get; set; } = false;
        public TransferClassification TransferClassification { get; set; }
        public DateTime LastCalculatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<PlayerCategoryRating> CategoryRatings { get; set; }
            = new HashSet<PlayerCategoryRating>();
    }


}
