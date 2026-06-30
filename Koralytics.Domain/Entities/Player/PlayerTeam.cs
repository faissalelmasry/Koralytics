using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Models.BaseModels;

namespace Koralytics.Domain.Entities.Player
{
    public class PlayerTeam : BaseEntity
    {
        public string PlayerId { get; set; } = string.Empty;
        public int TeamId { get; set; }
        public DateTime JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }

        public Player Player { get; set; } = null!;
        public Team Team { get; set; } = null!;
    }
}
