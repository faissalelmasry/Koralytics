using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Entities.Identity;
using Koralytics.Domain.Entities.Match;
using Koralytics.Domain.Entities.Parents;
using Koralytics.Domain.Enums;

namespace Koralytics.Domain.Entities.Player
{
    public class Player : User
    {
        public DateTime DateOfBirth { get; set; }
        public string? Nationality { get; set; }
        public PreferredFoot PreferredFoot { get; set; }
        public int WeakFootRating { get; set; }
        public string? PlayStyleTag { get; set; }
        public string? ArchetypePlayerName { get; set; }
        public string? ArchetypeText { get; set; }
        public AvailabilityStatus AvailabilityStatus { get; set; }

        public ICollection<PlayerAcademy> PlayerAcademies { get; set; } = new List<PlayerAcademy>();
        public ICollection<PlayerTeam> PlayerTeams { get; set; } = new List<PlayerTeam>();
        public ICollection<PlayerPosition> PlayerPositions { get; set; } = new List<PlayerPosition>();
        public ICollection<PlayerSubscription> PlayerSubscriptions { get; set; } = new List<PlayerSubscription>();
        public ICollection<PlayerGoal> PlayerGoals { get; set; } = new List<PlayerGoal>();
        public ICollection<PlayerHighlight> PlayerHighlights { get; set; } = new List<PlayerHighlight>();
        public ICollection<PlayerAchievement> PlayerAchievements { get; set; } = new List<PlayerAchievement>();
        public ICollection<ScouterView> ScouterViews { get; set; } = new List<ScouterView>();
        public ICollection<Parent> ParentPlayers { get; set; } = new List<Parent>();

        public ICollection<MatchPlayerRating> PlayerRatings { get; set; } = new List<MatchPlayerRating>();
    }
}
