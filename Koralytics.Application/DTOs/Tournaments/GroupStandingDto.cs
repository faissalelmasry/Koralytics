using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.Tournaments
{
    public class GroupStandingDto
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public List<StandingRowDto> Standings { get; set; } = [];
        public List<FixtureDto> Fixtures { get; set; } = [];
    }
}
