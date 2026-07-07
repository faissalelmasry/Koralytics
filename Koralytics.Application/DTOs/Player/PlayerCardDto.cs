using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Enums;

namespace Koralytics.Application.DTOs.Player
{
    public class PlayerCardDto
    {

        public string PlayerName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;  // "GK" or "Field"

        public decimal OverallRating { get; set; }

        public decimal PaceRating { get; set; }
        public decimal DribblingRating { get; set; }
        public decimal ShootingRating { get; set; }
        public decimal DefendingRating { get; set; }
        public decimal PassingRating { get; set; }
        public decimal PhysicalRating { get; set; }
        public decimal? GoalkeepingRating { get; set; }

        public decimal OverallTrainingAvg { get; set; }
        public decimal OverallTournamentAvg { get; set; }
        public string TransferClassification { get; set; } = string.Empty;
        public string? ArchetypePlayerName { get; set; }  
        public string? PlayStyleTag { get; set; }         
        public PreferredFoot PreferredFoot { get; set; }
        public int WeakFootRating { get; set; }     
        public string? ProfileImageUrl { get; set; }
        public DateTime LastCalculatedAt { get; set; }

    }

}
