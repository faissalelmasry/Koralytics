using System;
using Koralytics.Domain.Models.BaseModels;

namespace Koralytics.Domain.Entities.Player
{
    public class PlayerHighlight : BaseEntity
    {
        public int PlayerId { get; set; }
        public int AcademyId { get; set; }
        public string VideoUrl { get; set; } = string.Empty;
        public string? Title { get; set; }
        public bool IsPinned { get; set; }
        public DateTime UploadedAt { get; set; }

        public Player Player { get; set; } = null!;
        public Academy.Academy Academy { get; set; } = null!;
    }
}
