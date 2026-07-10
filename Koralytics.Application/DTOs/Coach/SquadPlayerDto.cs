using Koralytics.Domain.Enums;

namespace Koralytics.Application.DTOs.Coach
{
    public class SquadPlayerDto
    {
        public int PlayerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }

        // Position
        public string PrimaryPosition { get; set; } = string.Empty;

        // Availability
        public AvailabilityStatus AvailabilityStatus { get; set; }

        // FIFA-card style ratings (0 if card not yet calculated)
        public decimal OverallRating { get; set; }
        public decimal PaceRating { get; set; }
        public decimal DribblingRating { get; set; }
        public decimal ShootingRating { get; set; }
        public decimal DefendingRating { get; set; }
        public decimal PassingRating { get; set; }
        public decimal PhysicalRating { get; set; }
        public decimal? GoalkeepingRating { get; set; }

        // Player attributes
        public PreferredFoot PreferredFoot { get; set; }
        public int WeakFootRating { get; set; }
        public string? ArchetypePlayerName { get; set; }
        public string? PlayStyleTag { get; set; }
    }
}
