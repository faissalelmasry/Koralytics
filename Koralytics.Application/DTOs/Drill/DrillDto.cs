using Koralytics.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.Drill
{
    public class DrillDto
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public int DrillTemplateId { get; set; }
        public DrillMode Mode { get; set; }
        public DifficultyLevel DifficultyLevel { get; set; }
        public string? Notes { get; set; }
    }
}
