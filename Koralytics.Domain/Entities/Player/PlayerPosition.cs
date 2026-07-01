using System;
using Koralytics.Domain.Models.BaseModels;

namespace Koralytics.Domain.Entities.Player
{
    public class PlayerPosition : BaseEntity
    {
        public int PlayerId { get; set; }
        public string Position { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }

        public Player Player { get; set; } = null!;
    }
}
