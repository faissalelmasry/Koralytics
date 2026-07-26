using Koralytics.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.Drill
{
    public class CreateDrillSessionDto
    {
        public int TeamId { get; set; }
        public DateTime SessionDate { get; set; }
        public SessionType Type { get; set; }
        public SessionStatus Status { get; set; }
        public string? Notes { get; set; }
        public string? Location { get; set; }

        // The list of players the coach expects at this session
        public List<int> PlayerIds { get; set; } = new List<int>();
    }
}
