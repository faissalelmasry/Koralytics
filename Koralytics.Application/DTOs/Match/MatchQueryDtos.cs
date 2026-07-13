namespace Koralytics.Application.DTOs.Match
{
    public class HeadToHeadResponseDto
    {
        public int TeamAId { get; set; }
        public string TeamAName { get; set; } = string.Empty;
        public int TeamBId { get; set; }
        public string TeamBName { get; set; } = string.Empty;
        public int TotalMatches { get; set; }
        public int TeamAWins { get; set; }
        public int TeamBWins { get; set; }
        public int Draws { get; set; }
        public List<HeadToHeadMatchDto> Matches { get; set; } = [];
    }

    public class HeadToHeadMatchDto
    {
        public int MatchId { get; set; }
        public DateTime MatchDate { get; set; }
        public string HomeTeamName { get; set; } = string.Empty;
        public string AwayTeamName { get; set; } = string.Empty;
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
    }

    public class PostMatchAnalysisResponseDto
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public List<PostMatchAnalysisMatchDto> RecentMatches { get; set; } = [];
    }

    public class PostMatchAnalysisMatchDto
    {
        public int MatchId { get; set; }
        public DateTime MatchDate { get; set; }
        public string OpponentName { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
    }
}
