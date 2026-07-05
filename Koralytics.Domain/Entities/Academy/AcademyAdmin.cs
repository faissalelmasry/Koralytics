using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Entities.Identity;

namespace Koralytics.Domain.Entities.Academy
{
    public class AcademyAdmin : User
    {
        public int AcademyId { get; set; }
        public Academy Academy { get; set; } = null!;
    }
}
