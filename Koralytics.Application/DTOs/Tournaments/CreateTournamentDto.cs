using Koralytics.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.Tournament
{
    public class CreateTournamentDto
    {
        public string Name { get; set; } = string.Empty;
        public MatchFormat Format { get; set; }
        public TournamentStructure Structure { get; set; }
        public int AgeGroupId { get; set; }
        public bool HasTwoLegs { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
