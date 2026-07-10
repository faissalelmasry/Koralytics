using Koralytics.Domain.Enums;

namespace Koralytics.Application.DTOs.Academies
{

    public class SendAnnouncementDto
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public AnnouncementTargetType TargetType { get; set; }
        public int TargetId { get; set; }
    }


    public class AnnouncementResponseDto
    {
        public int Id { get; set; }
        public int AcademyId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public AnnouncementTargetType TargetType { get; set; }
        public int TargetId { get; set; }
        public int SentByUserId { get; set; }
        public string SentByFullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
