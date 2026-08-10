using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.Notification
{
    public class NotifyParentsOfPlayersDto
    {
        public List<int> PlayerIds { get; set; } = new List<int>();
        public string EventType { get; set; } = string.Empty;
    }
}
