using Koralytics.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.Notification
{
    public class CreateAnnouncementDto
    {
        public string Title { get; set; }
        public string Body { get; set; } = string.Empty;
        public AnnouncementTargetType TargetType { get; set; }
        public string Role { get; set; } = string.Empty;
        public int TargetId { get; set; }
    }
}
