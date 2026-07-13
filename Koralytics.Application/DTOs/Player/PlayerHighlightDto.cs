using System;

namespace Koralytics.Application.DTOs.Player
{
    public class PlayerHighlightDto
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public int AcademyId { get; set; }
        public string VideoUrl { get; set; } = string.Empty;
        public string? Title { get; set; }
        public bool IsPinned { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
