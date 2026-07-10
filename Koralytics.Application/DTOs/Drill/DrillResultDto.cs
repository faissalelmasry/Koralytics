using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.Drill
{
    public class DrillResultDto
    {
        public int Id { get; set; }
        public int DrillId { get; set; }
        public int PlayerId { get; set; }

        public decimal? ManualScore { get; set; }
        public int DoneCount { get; set; }
        public int MissedCount { get; set; }
        public decimal FinalScore { get; set; }

        public string? CoachNotes { get; set; }
    }
}
