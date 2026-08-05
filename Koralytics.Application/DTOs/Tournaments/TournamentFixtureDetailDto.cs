using Koralytics.Domain.Enums;

namespace Koralytics.Application.DTOs.Tournaments
{
    public class TournamentFixtureDetailDto
    {
        public int FixtureId { get; set; }
        public int? MatchId { get; set; }
        public int HomeTeamId { get; set; }
        public int HomeRealTeamId { get; set; }
        public string HomeTeamName { get; set; } = string.Empty;
        public string? HomeAcademyName { get; set; }
        public int AwayTeamId { get; set; }
        public int AwayRealTeamId { get; set; }
        public string AwayTeamName { get; set; } = string.Empty;
        public string? AwayAcademyName { get; set; }
        public int TournamentId { get; set; }
        public string TournamentName { get; set; } = string.Empty;
        public string? GroupOrRoundName { get; set; }
        public MatchFormat Format { get; set; }
        public MatchStatus Status { get; set; }
        public int? LegNumber { get; set; }
    }

    public class UpdateFixtureResultDto
    {
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
    }
}
