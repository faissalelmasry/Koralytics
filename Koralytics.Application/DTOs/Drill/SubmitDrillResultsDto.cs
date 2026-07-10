using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.Drill
{
    public class SubmitDrillResultsDto
    {
        public List<PlayerDrillResultDto> Results { get; set; } = new List<PlayerDrillResultDto>();
    }
}
