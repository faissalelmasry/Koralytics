using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.Scouter
{
    public class ProfileViewerDetailDto
    {
        public int ScouterId { get; set; }
        public string ScouterName { get; set; } = default!;
        public bool IsScouterVerified { get; set; }
        public DateTime ViewedAt { get; set; }
    }
}
