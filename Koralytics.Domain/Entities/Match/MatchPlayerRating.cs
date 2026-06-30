using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Entities.Identity;
using Koralytics.Domain.Models.BaseModels;

namespace Koralytics.Domain.Entities.Match
{
    public class MatchPlayerRating : AuditableEntity
    {
        public int MatchId { get; set; }

        public int PlayerId { get; set; }

        public int CoachId { get; set; }

        public decimal Rating { get; set; }

        public int Goals { get; set; }

        public int Assists { get; set; }

        public int MinutesPlayed { get; set; }

        public bool IsMOTM { get; set; }

        public string? CoachNote { get; set; }


        public Match Match { get; set; } = default!;

        public Player.Player Player { get; set; } = default!;

        public User Coach { get; set; } = default!;
    }
}
