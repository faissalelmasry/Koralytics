using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.Drill
{
    public class ProgressionDataPointDto
    {
        public DateTime SessionDate { get; set; }
        public decimal FinalScore { get; set; }
        public string DrillName { get; set; } // Helpful for the tooltip when hovering over the chart
    }
}
