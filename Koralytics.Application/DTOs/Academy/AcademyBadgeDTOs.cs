using System;

namespace Koralytics.Application.DTOs.Academies
{
    public class CreateAcademyBadgeDto
    {
        public Koralytics.Domain.Enums.AcademyBadgeType BadgeType { get; set; }
        public DateTime AwardedAt { get; set; }
    }

    public class AcademyBadgeResponseDto
    {
        public int Id { get; set; }
        public int AcademyId { get; set; }
        public Koralytics.Domain.Enums.AcademyBadgeType BadgeType { get; set; }
        public DateTime AwardedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
