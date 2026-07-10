using Koralytics.Domain.Enums;

namespace Koralytics.Application.DTOs.Player
{
    public class PlayerProfileDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public int Age { get; set; }
        public string? Nationality { get; set; }
        public string? ArchetypeText { get; set; }
        public AvailabilityStatus AvailabilityStatus { get; set; }

        public List<PlayerPositionDto> Positions { get; set; } = [];
        public PlayerAcademyDto? CurrentAcademy { get; set; }
        public List<PlayerTeamDto> Teams { get; set; } = [];

        public PlayerCardDto? PlayerCard { get; set; }

        public int TotalMatches { get; set; }
        public int TotalGoals { get; set; }
        public int TotalAssists { get; set; }
        public int TotalMOTMs { get; set; }

        public MatchTypeStats SessionStats { get; set; } = new();
        public MatchTypeStats FriendlyStats { get; set; } = new();
        public MatchTypeStats TournamentStats { get; set; } = new();
    }

    public class PlayerPositionDto
    {
        public string Position { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
    }

    public class PlayerAcademyDto
    {
        public int AcademyId { get; set; }
        public string AcademyName { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
    }

    public class PlayerTeamDto
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string? AgeGroupName { get; set; }
    }

    public class MatchTypeStats
    {
        public int Matches { get; set; }
        public int Goals { get; set; }
        public int Assists { get; set; }
        public int MOTMs { get; set; }
    }
}
