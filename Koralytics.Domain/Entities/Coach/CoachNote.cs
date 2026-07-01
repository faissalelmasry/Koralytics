using System;
using Koralytics.Domain.Models.BaseModels;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Entities.Drill;
using Koralytics.Domain.Entities.Match;

namespace Koralytics.Domain.Entities.Coach
{
    public class CoachNote : BaseEntity
    {
        public int CoachUserId { get; set; }
        public int PlayerId { get; set; }
        public int AcademyId { get; set; }
        public int? SessionId { get; set; }
        public int? MatchId { get; set; }
        public string Note { get; set; } = string.Empty;
        public bool IsPublic { get; set; }

        public Coach Coach { get; set; } = null!;
        public Player.Player Player { get; set; } = null!;
        public Academy.Academy Academy { get; set; } = null!;
        public DrillSession? Session { get; set; }
        public Match.Match? Match { get; set; }
    }
}
