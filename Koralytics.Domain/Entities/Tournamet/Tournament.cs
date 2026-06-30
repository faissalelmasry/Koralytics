using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Models.BaseModels;

namespace Koralytics.Domain.Entities.Tournamet
{
    public class Tournament : AuditableEntity
    {
        public string Name { get; set; } = string.Empty;

        public MatchFormat Format { get; set; }

        public TournamentStructure Structure { get; set; }

        public int AgeGroupId { get; set; }

        public bool HasTwoLegs { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public TournamentStatus Status { get; set; }


        public AgeGroup AgeGroup { get; set; } = default!;

        public ICollection<TournamentTeam> TournamentTeams { get; set; } = [];

        public ICollection<TournamentGroup> TournamentGroups { get; set; } = [];

        public ICollection<TournamentRound> TournamentRounds { get; set; } = [];

        public ICollection<TournamentSquad> TournamentSquads { get; set; } = [];

        public ICollection<TournamentHallOfFame> TournamentHallOfFames { get; set; } = [];
    }
}
