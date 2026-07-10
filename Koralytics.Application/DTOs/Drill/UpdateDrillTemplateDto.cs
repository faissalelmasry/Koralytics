using Koralytics.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.Drill
{
    public class UpdateDrillTemplateDto
    {
        public string Name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public DifficultyLevel DifficultyLevel { get; set; }
        public DrillMode DrillMode { get; set; }
    }
}
