using System.Collections.Generic;

namespace Koralytics.Application.DTOs.Drill
{
    public class PlayerBiasComparisonDto
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public decimal AvgPracticeScore { get; set; }
        public decimal AvgMatchScore { get; set; }
        public decimal Delta { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class CoachBiasReportDto
    {
        public int CoachId { get; set; }
        public string CoachName { get; set; } = string.Empty;
        public decimal TrustPercentage { get; set; }
        public int PlayersAnalyzedCount { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public List<PlayerBiasComparisonDto> PlayerComparisons { get; set; } = new();
    }
}
