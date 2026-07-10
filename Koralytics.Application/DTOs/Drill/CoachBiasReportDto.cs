using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.Drill
{
    public class CoachBiasReportDto
    {
        public int CoachId { get; set; }
        public decimal TrustPercentage { get; set; }
        public int PlayersAnalyzedCount { get; set; }
        public string Remarks { get; set; } = string.Empty;
    }
}
