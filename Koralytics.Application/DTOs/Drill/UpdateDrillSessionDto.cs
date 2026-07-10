using Koralytics.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.Drill
{
    public class UpdateDrillSessionDto
    {
        public DateTime SessionDate { get; set; }
        public SessionType Type { get; set; }
        public SessionStatus Status { get; set; }
        public string? Notes { get; set; }
    }
}
