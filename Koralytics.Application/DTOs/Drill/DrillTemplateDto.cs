using Koralytics.Domain.Enums;

namespace Koralytics.Application.DTOs.Drill
{
        public class DrillTemplateDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int CategoryId { get; set; }
            public string CategoryName { get; set; }
            public int? AcademyId { get; set; }
            public string AcademyName { get; set; }
            public DifficultyLevel DifficultyLevel { get; set; }
            public DrillMode DrillMode { get; set; }
            public bool IsShared { get; set; }
            public int CreatedById { get; set; }
    }
        public class DrillCategoryDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }

