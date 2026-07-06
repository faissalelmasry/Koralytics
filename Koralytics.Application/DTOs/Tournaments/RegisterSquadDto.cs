using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.Tournament
{
    public class RegisterSquadDto
    {
        public int TournamentId { get; set; }
        public int TeamId { get; set; }
        public List<int> PlayerIds { get; set; } = [];
    }
}
