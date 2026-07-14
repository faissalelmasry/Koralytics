using System;

namespace Koralytics.Application.DTOs.Match
{
    public class PlayerReadinessDto
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public int ReadinessScore { get; set; }
        public string Status { get; set; } = string.Empty;
        public int MatchesPlayedLast7Days { get; set; }
        public string Recommendation { get; set; } = string.Empty;
    }
}
