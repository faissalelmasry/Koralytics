using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.Notification
{
    public class NotifyMultiplePlayersDto
    {
        public List<int> PlayerIds { get; set; } = new List<int>();
        public string Message { get; set; } = string.Empty;
    }
}
