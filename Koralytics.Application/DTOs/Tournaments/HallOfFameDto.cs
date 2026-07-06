using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.Tournaments
{
    public class HallOfFameDto
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public string AwardType { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
    }
}
