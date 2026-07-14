using Koralytics.Domain.Enums;

namespace Koralytics.Application.DTOs.Match
{
    public class CreateMatchRequestDto
    {
        public int RequesterTeamId { get; set; }
        public int TargetTeamId { get; set; }
        public MatchFormat Format { get; set; }
        public DateTime ProposedDate { get; set; }
        public string? Location { get; set; }
    }

    public class MatchRequestResponseDto
    {
        public int Id { get; set; }
        public int RequesterTeamId { get; set; }
        public string RequesterTeamName { get; set; } = string.Empty;
        public int TargetTeamId { get; set; }
        public string TargetTeamName { get; set; } = string.Empty;
        public int RequesterCoachId { get; set; }
        public string RequesterCoachName { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public DateTime ProposedDate { get; set; }
        public string? Location { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? ResolvedByCoachId { get; set; }
        public string? ResolvedByCoachName { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public int? MatchId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
