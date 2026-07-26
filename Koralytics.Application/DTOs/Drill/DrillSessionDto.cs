using Koralytics.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.Drill
{
    public class DrillSessionDto
    {
        public int Id { get; set; }
        public int AcademyId { get; set; }
        public int TeamId { get; set; }
        public int CoachId { get; set; }
        public string? Location { get; set; }
        public string? CoachName { get; set; }
        public string? TeamName { get; set; }
        public DateTime SessionDate { get; set; }
        public SessionType Type { get; set; }
        public SessionStatus Status { get; set; }
        public string? Notes { get; set; }
    }
}
