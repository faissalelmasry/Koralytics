using Koralytics.Domain.Enums;

namespace Koralytics.Application.DTOs.Match
{
    public class CreateFriendlyMatchDto
    {
        public int HomeTeamId { get; set; }
        public int AwayTeamId { get; set; }
        public MatchFormat Format { get; set; }
        public DateTime MatchDate { get; set; }
        public string? Location { get; set; }
    }
}
