using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.Drill
{
    public class UpdateSessionAttendanceDto
    {
        public List<PlayerAttendanceDto> Attendances { get; set; } = new List<PlayerAttendanceDto>();
    }

    public class PlayerAttendanceDto
    {
        public int PlayerId { get; set; }
        public bool IsPresent { get; set; }
    }
}
