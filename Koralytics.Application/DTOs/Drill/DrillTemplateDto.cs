using Koralytics.Domain.Enums;

namespace Koralytics.Application.DTOs.Drill
{
    public class DrillTemplateDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CategoryId { get; set; }
        public int? AcademyId { get; set; }
        public DifficultyLevel DifficultyLevel { get; set; }
        public DrillMode DrillMode { get; set; }
        public bool IsShared { get; set; }
    }
}
