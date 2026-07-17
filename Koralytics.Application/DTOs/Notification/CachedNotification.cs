using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.Notification
{
    public class CachedNotification
    {
        public string Id { get; set; } = Guid.NewGuid().ToString(); 
        public string Title { get; set; }
        public string Content { get; set; }
        public string Type { get; set; } 
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;
        public object Payload { get; set; } 
    }
}
