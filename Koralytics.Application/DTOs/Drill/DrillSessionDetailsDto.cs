using Koralytics.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.Drill { 
public class DrillSessionDetailsDto
{
        public int Id { get; set; }
        public int AcademyId { get; set; }
        public int TeamId { get; set; }
        public int CoachId { get; set; }
        public DateTime SessionDate { get; set; }
        public SessionType Type { get; set; }     // 🟢 FIXED
        public SessionStatus Status { get; set; } // 🟢 ADDED MISSING PROPERTY
        public string? Notes { get; set; }
        public ICollection<DrillDto> SessionDrills { get; set; } = new List<DrillDto>();
    }
}
