using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.Drill
{
    public class PlayerDrillResultDto
    {
        public int PlayerId { get; set; }
        public decimal? ManualScore { get; set; }
        public int DoneCount { get; set; }    // Defaults to 0 if not sent
        public int MissedCount { get; set; }  // Defaults to 0 if not sent
        public string? CoachNotes { get; set; }
    }
}
