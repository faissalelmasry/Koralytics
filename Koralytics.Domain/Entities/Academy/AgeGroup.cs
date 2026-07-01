using Koralytics.Domain.Models.BaseModels;
using System;
using AcademyEntity = Koralytics.Domain.Entities.Academy.Academy;


namespace Koralytics.Domain.Entities.Academy
{
    public class AgeGroup : BaseEntity
    {
        public int AcademyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MinAge { get; set; }
        public int MaxAge { get; set; }

        public AcademyEntity Academy { get; set; } = null!;

        public ICollection<Team> Teams { get; set; } = [];
    }
}
