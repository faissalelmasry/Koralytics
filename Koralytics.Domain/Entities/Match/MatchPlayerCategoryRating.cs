using Koralytics.Domain.Entities.Drill;
using Koralytics.Domain.Models.BaseModels;

namespace Koralytics.Domain.Entities.Match
{
    public class MatchPlayerCategoryRating : BaseEntity
    {
        public int MatchPlayerRatingId { get; set; }
        public MatchPlayerRating MatchPlayerRating { get; set; } = default!;

        public int DrillCategoryId { get; set; }
        public DrillCategory DrillCategory { get; set; } = default!;

        public decimal Rating { get; set; }
    }
}
