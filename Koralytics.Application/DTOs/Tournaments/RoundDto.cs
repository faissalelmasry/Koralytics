using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.Tournaments
{
    public class RoundDto
    {
        public int RoundId { get; set; }
        public string RoundName { get; set; } = string.Empty;
        public int RoundNumber { get; set; }
        public List<FixtureDto> Fixtures { get; set; } = [];
    }
}
