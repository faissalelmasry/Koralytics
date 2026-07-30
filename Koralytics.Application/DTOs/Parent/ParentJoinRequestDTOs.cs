using System;
using Koralytics.Domain.Enums;

namespace Koralytics.Application.DTOs.Parent
{
    public class ParentPlayerSearchResponseDto
    {
        public int PlayerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Position { get; set; }
        public string? TeamName { get; set; }
        public string? PhotoUrl { get; set; }
        public bool HasPendingRequest { get; set; }
        public bool IsAlreadyLinked { get; set; }
    }

    public class ParentPlayerJoinRequestResponseDto
    {
        public int Id { get; set; }
        public int ParentId { get; set; }
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public string? PlayerPhotoUrl { get; set; }
        public string? PlayerPosition { get; set; }
        public string ParentName { get; set; } = string.Empty;
        public string ParentEmail { get; set; } = string.Empty;
        public JoinRequestStatus Status { get; set; }
        public string StatusName => Status.ToString();
        public DateTime RequestedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
    }

    public class RespondParentJoinRequestDto
    {
        public JoinRequestStatus Status { get; set; }
    }
}
