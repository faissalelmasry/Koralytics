using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Drill;
using Koralytics.Domain.Entities.Tournamet;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Models.BaseModels;
using static System.Collections.Specialized.BitVector32;

namespace Koralytics.Domain.Entities.Match
{
    public class Match : AuditableEntity
    {
        public int HomeTeamId { get; set; }

        public int AwayTeamId { get; set; }

        public int? TournamentId { get; set; }

        public int? SessionId { get; set; }

        public Enums.MatchType Type { get; set; }

        public MatchFormat Format { get; set; }

        public DateTime MatchDate { get; set; }

        public string Location { get; set; } = string.Empty;

        public MatchStatus Status { get; set; }

        public int HomeScore { get; set; }

        public int AwayScore { get; set; }

        public int? HomePenaltyScore { get; set; }

        public int? AwayPenaltyScore { get; set; }

        public int? WinningTeamId { get; set; }

        public Team HomeTeam { get; set; } = default!;

        public Team AwayTeam { get; set; } = default!;

        public Team? WinningTeam { get; set; }

        public Tournament? Tournament { get; set; }

        public DrillSession? Session { get; set; }

        public ICollection<MatchLineup> MatchLineups { get; set; } = [];

        public ICollection<MatchEvent> MatchEvents { get; set; } = [];

        public ICollection<MatchPlayerRating> MatchPlayerRatings { get; set; } = [];
    }
}
