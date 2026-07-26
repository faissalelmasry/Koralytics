using Koralytics.Domain.Enums;

namespace Koralytics.Application.DTOs.Academies
{
    // ─── Coach Performance ───────────────────────────────────────────────────

    public class CoachPerformanceDto
    {
        public int CoachUserId { get; set; }
        public string CoachFullName { get; set; } = string.Empty;

        /// <summary>
        /// Average improvement rate across all players in the coach's teams.
        /// Calculated as (current OverallRating - baseline OverallTrainingAvg) per player, then averaged.
        /// </summary>
        public decimal AveragePlayerImprovementRate { get; set; }

        /// <summary>
        /// Bias score fetched from CoachAcademy.BiasScore.
        /// TODO: Value is populated by the AI/Analytics Module BiasScore calculation job.
        /// </summary>
        public decimal? BiasScore { get; set; }

        /// <summary>Rank position within the academy (1 = best).</summary>
        public int Rank { get; set; }

        public List<CoachTeamSummaryDto> Teams { get; set; } = [];
    }

    public class CoachTeamSummaryDto
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public int PlayerCount { get; set; }
    }

    // ─── Subscription Status ─────────────────────────────────────────────────

    public class SubscriptionStatusSummaryDto
    {
        public int TotalPaid { get; set; }
        public int TotalUnpaid { get; set; }
        public int TotalGrace { get; set; }
        public int TotalPlayers { get; set; }
        public List<UnpaidPlayerDto> UnpaidPlayers { get; set; } = [];
    }

    public class UnpaidPlayerDto
    {
        public int PlayerId { get; set; }
        public string PlayerFullName { get; set; } = string.Empty;
        public SubscriptionStatus Status { get; set; }

        /// <summary>Grace deadline (null if Status is Unpaid with no grace period).</summary>
        public DateTime? GraceUntil { get; set; }

        /// <summary>True if grace period has already expired.</summary>
        public bool IsGraceExpired { get; set; }
    }

    // ─── Analytics Internal Data ─────────────────────────────────────────────

    public class CoachData
    {
        public int CoachUserId { get; set; }
        public decimal BiasScore { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }

    public class CoachTeamData
    {
        public int CoachUserId { get; set; }
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
    }

    public class PlayerTeamData
    {
        public int TeamId { get; set; }
        public int PlayerId { get; set; }
    }

    public class PlayerCardData
    {
        public int PlayerId { get; set; }
        public decimal OverallRating { get; set; }
        public decimal OverallTrainingAvg { get; set; }
    }
}
