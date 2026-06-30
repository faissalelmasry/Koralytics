using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Entities;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Models.BaseModels;

namespace Koralytics.Domain.Entities.Player
{
    public class PlayerAcademy : BaseEntity
    {
        public string PlayerId { get; set; } = string.Empty;
        public int AcademyId { get; set; }
        public DateTime JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }
        public PlayerAcademyStatus Status { get; set; }

        public Player Player { get; set; } = null!;
        public Academy.Academy Academy { get; set; } = null!;
    }
}
