using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Identity;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Models.BaseModels;

namespace Koralytics.Domain.Entities.Match
{
    public class MatchRequest : BaseEntity
    {
        public int RequesterTeamId { get; set; }
        public int TargetTeamId { get; set; }
        public int RequesterCoachId { get; set; }
        public MatchFormat Format { get; set; }
        public DateTime ProposedDate { get; set; }
        public string? Location { get; set; }
        public MatchRequestStatus Status { get; set; }
        public int? ResolvedByCoachId { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public int? MatchId { get; set; }

        public virtual Team RequesterTeam { get; set; } = null!;
        public virtual Team TargetTeam { get; set; } = null!;
        public virtual User RequesterCoach { get; set; } = null!;
        public virtual User? ResolvedByCoach { get; set; }
        public virtual Match? Match { get; set; }
    }
}
