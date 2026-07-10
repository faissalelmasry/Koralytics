using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.Drill
{
    public class PlayerProgressionDto
    {
        public int PlayerId { get; set; }
        public string CategoryName { get; set; }
        public List<ProgressionDataPointDto> ProgressionChart { get; set; } = new List<ProgressionDataPointDto>();
    }
}
