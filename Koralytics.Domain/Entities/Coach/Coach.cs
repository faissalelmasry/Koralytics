using Koralytics.Domain.Entities.Identity;
using System.Collections.Generic;

namespace Koralytics.Domain.Entities.Coach
{
    public class Coach : User
    {
        public ICollection<CoachAcademy> CoachAcademies { get; set; } = new List<CoachAcademy>();
        public ICollection<CoachTeam> CoachTeams { get; set; } = new List<CoachTeam>();
        public ICollection<CoachNote> CoachNotes { get; set; } = new List<CoachNote>();
        public ICollection<CoachTempAccess> CoachTempAccesses { get; set; } = new List<CoachTempAccess>();
    }
}
