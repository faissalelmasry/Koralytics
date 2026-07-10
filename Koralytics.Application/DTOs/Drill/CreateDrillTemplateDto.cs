using Koralytics.Domain.Enums;

namespace Koralytics.Application.DTOs.Drill
{
    public class CreateDrillTemplateDto
    {
        public string Name { get; set; }
        public int CategoryId { get; set; }
        public DifficultyLevel DifficultyLevel { get; set; }
        public DrillMode DrillMode { get; set; }
    }
}