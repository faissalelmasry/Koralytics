using System;
using Koralytics.Domain.Enums;

namespace Koralytics.Application.DTOs.Subscription
{
    public class PlayerSubscriptionDto
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;

        public int AcademyId { get; set; }
        public string AcademyName { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public SubscriptionStatus Status { get; set; }
        public SubscriptionDuration Duration { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? GraceUntil { get; set; }

        public int? PaidByUserId { get; set; }
        public string? PaidByUserName { get; set; }
    }

    public class CreateSubscriptionDto
    {
        public int PlayerId { get; set; }
        public int AcademyId { get; set; }
        public decimal Amount { get; set; }
        public SubscriptionDuration Duration { get; set; } = SubscriptionDuration.OneMonth;
        public DateTime? StartDate { get; set; }
    }
}