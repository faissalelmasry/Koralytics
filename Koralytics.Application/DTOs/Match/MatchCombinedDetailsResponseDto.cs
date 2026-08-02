namespace Koralytics.Application.DTOs.Match
{
    public class MatchCombinedDetailsResponseDto
    {
        public int MatchId { get; set; }
        public string HomeTeamName { get; set; } = string.Empty;
        public string AwayTeamName { get; set; } = string.Empty;
        public string HomeTeamAcademyName { get; set; } = string.Empty;
        public string AwayTeamAcademyName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? TournamentName { get; set; }
        public DateTime MatchDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public int? HomePenaltyScore { get; set; }
        public int? AwayPenaltyScore { get; set; }
        public string? WinningTeamName { get; set; }
        public string? HomeFormation { get; set; }
        public string? AwayFormation { get; set; }

        public List<CombinedMatchEventDto> Events { get; set; } = [];
        public List<CombinedMatchPlayerRatingDto> Ratings { get; set; } = [];
    }

    public class CombinedMatchEventDto
    {
        public int Id { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public string? AssistPlayerName { get; set; }
        public bool? IsHomeSide { get; set; }
        public string EventType { get; set; } = string.Empty;
        public int Minute { get; set; }
    }

    public class CombinedMatchPlayerRatingDto
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public int Goals { get; set; }
        public int Assists { get; set; }
        public bool IsMOTM { get; set; }
        public string? CoachNote { get; set; }
        public decimal OverallAvgRating { get; set; }
        public decimal AvgMatchRating { get; set; }
        public List<CombinedCategoryRatingDto> CategoryRatings { get; set; } = [];
    }

    public class CombinedCategoryRatingDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal Rating { get; set; }
    }
}
