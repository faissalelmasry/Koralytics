using System;
using System.Collections.Generic;
using Koralytics.Domain.Entities.Identity;

namespace Koralytics.Domain.Entities.Scouter
{
    public class Scouter : User
    {
        public bool IsVerified { get; set; }
        public DateTime? VerifiedAt { get; set; }

        public ICollection<ScouterShortlist> ScouterShortlists { get; set; } = new List<ScouterShortlist>();
        public ICollection<ScouterFollow> ScouterFollows { get; set; } = new List<ScouterFollow>();
        public ICollection<ScouterReport> ScouterReports { get; set; } = new List<ScouterReport>();
    }
}
