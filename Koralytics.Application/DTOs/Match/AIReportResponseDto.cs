using System;
using Koralytics.Domain.Enums;

namespace Koralytics.Application.DTOs.Match
{
    public class AIReportResponseDto
    {
        public int Id { get; set; }
        public AIReportType ReportType { get; set; }
        public int ReferenceId { get; set; }
        public int? AcademyId { get; set; }
        public string ReportText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
