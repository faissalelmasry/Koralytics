using Koralytics.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.Tournaments
{
    public class BracketDto
    {
        public int TournamentId { get; set; }
        public string TournamentName { get; set; } = string.Empty;
        public TournamentStatus Status { get; set; }
        public List<GroupStandingDto> Groups { get; set; } = [];
        public List<RoundDto> Rounds { get; set; } = [];
    }
}
