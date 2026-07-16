using System;
using Koralytics.Domain.Enums;

namespace Koralytics.Application.DTOs.SystemAdmin
{
    public class AcademyRequestResponseDto
    {
        public int Id { get; set; }
        public string AcademyName { get; set; } = string.Empty;
        public string ContactPersonName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public AcademyRequestStatus RequestStatus { get; set; }
        public DateTime RequestedAt { get; set; }
        public int RequestedById { get; set; }
        public string RequestedByFullName { get; set; } = string.Empty;
        public string? RejectedReason { get; set; }
    }

    public class RejectAcademyRequestDto
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class CreateAcademyRequestDto
    {
        public string AcademyName { get; set; } = string.Empty;
        public string ContactPersonName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
    }
}
