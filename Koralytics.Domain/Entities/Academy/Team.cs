using Koralytics.Domain.Models.BaseModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Domain.Entities.Academy
{
    public class Team:AuditableEntity
    {
        public string Name { get; set; }


        public int AgeGroupId { get; set; }
        public int AcademyId { get; set; }

        public virtual AgeGroup AgeGroup { get; set; } = null!;

        public int LocationId { get; set; }
        public virtual AcademyLocation Location { get; set; } = null!;

        public int? CoachId { get; set; }
        public virtual Coach.Coach? Coach { get; set; } 


        public virtual ICollection<Player.PlayerTeam> PlayerTeams { get; set; } = new HashSet<Player.PlayerTeam>();




    }
}
