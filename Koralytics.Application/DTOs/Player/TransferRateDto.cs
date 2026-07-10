using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.Player
{
    public class TransferRateDto
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public decimal OverallTrainingAvg { get; set; }
        public decimal OverallTournamentAvg { get; set; }
        public decimal TransferGap { get; set; }
        public string Classification { get; set; } = string.Empty;
    }

}
