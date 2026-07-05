using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Models.BaseModels;

namespace Koralytics.Domain.Entities.Match
{
    public class MatchEvent : AuditableEntity
    {
        public int MatchId { get; set; }
        public int TeamId { get; set; }

        public int PlayerId { get; set; }

        public int? AssistPlayerId { get; set; }

        public MatchEventType EventType { get; set; }

        public int Minute { get; set; }
        public Match Match { get; set; } = default!;
        public Team Team { get; set; } = default!;

        public Player.Player Player { get; set; } = default!;

        public Player.Player? AssistPlayer { get; set; }
    }
}
