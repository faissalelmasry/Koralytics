using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Models.BaseModels;

namespace Koralytics.Domain.Entities.Tournamet
{
    public class TournamentGroup: AuditableEntity
    {
        public int TournamentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsDummy { get; set; }

        public virtual Tournament Tournament { get; set; } = null!;

        public virtual ICollection<TournamentGroupTeam> TournamentGroupTeams { get; set; } = new List<TournamentGroupTeam>();
        public virtual ICollection<TournamentStanding> TournamentStandings { get; set; } = new List<TournamentStanding>();
        public virtual ICollection<TournamentFixture> TournamentFixtures { get; set; } = new List<TournamentFixture>();
    }
}
