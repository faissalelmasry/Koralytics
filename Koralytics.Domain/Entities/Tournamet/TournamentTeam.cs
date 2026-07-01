using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Models.BaseModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Domain.Entities.Tournamet
{
    public class TournamentTeam : AuditableEntity
    {
        public int TournamentId { get; set; }
        public int TeamId { get; set; }
        public int? SeedNumber { get; set; }
        public TournamentTeamStatus Status { get; set; }
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        public Tournament Tournament { get; set; } = null!;
        public Team Team { get; set; } = null!;

        public ICollection<TournamentGroupTeam> TournamentGroupTeams { get; set; } = [];
        public ICollection<TournamentStanding> TournamentStandings { get; set; } = [];
        public ICollection<TournamentFixture> HomeFixtures { get; set; } = [];
        public ICollection<TournamentFixture> AwayFixtures { get; set; } = [];
    }
}
