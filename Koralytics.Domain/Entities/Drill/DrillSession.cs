using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Identity;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Models.BaseModels;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Domain.Entities.Drill
{
    public class DrillSession : AuditableEntity
    {
        public int AcademyId { get; set; }
        public Academy.Academy? DrillSessionAcademy { get; set; }

        public int TeamId { get; set; }
        public Team? DrillSessionTeam { get; set; }

        public int CoachId { get; set; }
        public User? DrillSessionCoach { get; set; }

        public string? Location { get; set; }
        public DateTime SessionDate { get; set; }

        public SessionType Type { get; set; }
        public SessionStatus Status { get; set; }

        public string? Notes { get; set; }

        public ICollection<Drill> SessionDrills { get; set; } = new HashSet<Drill>();
        public ICollection<SessionAttendance> SessionAttendances { get; set; } = new HashSet<SessionAttendance>();
    }
}
