using Koralytics.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.Tournaments
{
    public class FixtureDto
    {
        public int FixtureId { get; set; }
        public string HomeTeamName { get; set; } = string.Empty;
        public string AwayTeamName { get; set; } = string.Empty;
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public string? WinnerTeamName { get; set; }
        public MatchStatus Status { get; set; }
        public int? LegNumber { get; set; }
    }
}
