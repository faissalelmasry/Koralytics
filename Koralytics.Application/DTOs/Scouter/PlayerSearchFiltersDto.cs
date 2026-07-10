using Koralytics.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.ScouterDtos
{
    public class PlayerSearchFiltersDto
    {
        public List<string>? Positions { get; set; }
        public PreferredFoot? PreferredFoot { get; set; }
        public MatchFormat? Format { get; set; }
        public int? AcademyId { get; set; }

        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public decimal? MinRating { get; set; }
        public decimal? MaxRating { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
