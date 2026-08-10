using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.Notification
{
    public class AcademyNotificationDto
    {
        public List<int> AcademyIds { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }
}
