using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Models.BaseModels;

namespace Koralytics.Domain.Entities.Tournamet
{
    public class TournamentFixture:BaseEntity
    {
        public int? MatchId { get; set; }
        public Match.Match? Match { get; set; }
        public int? GroupId { get; set; }
        public TournamentGroup? Group { get; set; }

        public int? RoundId { get; set; }
        public TournamentRound? Round { get; set; }

        public int HomeTeamId { get; set; }
        public TournamentTeam HomeTeam { get; set; }

        public int AwayTeamId { get; set; }
        public TournamentTeam AwayTeam { get; set; }

        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }

        public int? WinnerTeamId { get; set; }
        public TournamentTeam? WinnerTeam { get; set; }

        public int? LegNumber { get; set; }

        public MatchStatus Status { get; set; }
    }
}
