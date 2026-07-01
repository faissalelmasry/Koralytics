using Koralytics.Domain.Models.BaseModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Domain.Entities.Tournamet
{
    public class TournamentGroupTeam : BaseEntity
    {
        public int GroupId { get; set; }
        public int TournamentTeamId { get; set; }

        public TournamentGroup Group { get; set; } = null!;
        public TournamentTeam TournamentTeam { get; set; } = null!;
    }
}
