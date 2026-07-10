using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.ScouterDtos
{
    public class ScouterShortlistDto
    {
        public int Id { get; set; }
        public int ScouterUserId { get; set; }
        public int PlayerId { get; set; }
        public DateTime AddedAt { get; set; }
    }
}
