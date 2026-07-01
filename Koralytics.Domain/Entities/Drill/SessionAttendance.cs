using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Models.BaseModels;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Domain.Entities.Drill
{
    public class SessionAttendance : AuditableEntity
    {
        public int SessionId { get; set; }
        public DrillSession? DrillSession { get; set; }
       
        public int playerId { get; set; }
        public Player.Player? Player { get; set; }

        public bool IsPresent { get; set; } = false;
    }
}
