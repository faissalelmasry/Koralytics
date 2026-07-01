using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Models.BaseModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Domain.Entities.AI
{
    public class AIReport :BaseEntity
    {
        public AIReportType ReportType { get; set; }
        public int ReferenceId { get; set; }
        public int? AcademyId { get; set; }
        public string ReportText { get; set; } = string.Empty;
        public Academy.Academy? Academy { get; set; }


    }
}
