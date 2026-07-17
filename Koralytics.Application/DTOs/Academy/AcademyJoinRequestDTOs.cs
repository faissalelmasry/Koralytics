using System;

using Koralytics.Domain.Enums;

namespace Koralytics.Application.DTOs.Academy
{

    public class RespondJoinRequestDto
    {
        public JoinRequestStatus Status { get; set; }
    }

    public class AcademyPlayerJoinRequestResponseDto
    {
        public int Id { get; set; }
        public int AcademyId { get; set; }
        public string AcademyName { get; set; } = string.Empty;
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public JoinRequestStatus Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
    }

    public class AcademyCoachJoinRequestResponseDto
    {
        public int Id { get; set; }
        public int AcademyId { get; set; }
        public string AcademyName { get; set; } = string.Empty;
        public int CoachId { get; set; }
        public string CoachName { get; set; } = string.Empty;
        public JoinRequestStatus Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
    }

    public class PlayerSearchResponseDto
    {
        public int PlayerId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }

    public class CoachSearchResponseDto
    {
        public int CoachId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }
}
