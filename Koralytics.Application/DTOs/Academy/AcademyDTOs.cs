using Koralytics.Domain.Enums;

namespace Koralytics.Application.DTOs.Academies
{
    // ─── Request DTOs ────────────────────────────────────────────────────────

    public class CreateAcademyDto
    {
        /// <summary>The AcademyRequest.Id that was approved.</summary>
        public int AcademyRequestId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string? PrimaryColor { get; set; }
        public string? SecondaryColor { get; set; }
        public DateTime FoundedAt { get; set; }

        /// <summary>The User.Id who will be the AcademyAdmin.</summary>
        public int AdminUserId { get; set; }
    }

    public class UpdateAcademyDto
    {
        public string? Name { get; set; }
        public string? LogoUrl { get; set; }
        public string? PrimaryColor { get; set; }
        public string? SecondaryColor { get; set; }
    }

    public class AddLocationDto
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }

    public class AcademyListRequestDto
    {
        public string? SearchQuery { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    // ─── Response DTOs ───────────────────────────────────────────────────────

    public class AcademyMemberResponseDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // e.g., "Player", "Coach"
        public string? Position { get; set; }
        public string? SquadStatus { get; set; }
        public DateTime JoinedAt { get; set; }
    }

    public class AcademyAdminResponseDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsOwner { get; set; }
    }

    public class UpdatePlayerSubscriptionDto
    {
        public SubscriptionStatus Status { get; set; }
        public DateTime? GraceUntil { get; set; }
    }

    public class AcademyResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string? PrimaryColor { get; set; }
        public string? SecondaryColor { get; set; }
        public DateTime FoundedAt { get; set; }
        public AcademyStatus Status { get; set; }
        public int AdminUserId { get; set; }
        public string AdminFullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int LocationCount { get; set; }
    }

    public class AcademyListResponseDto
    {
        public List<AcademyResponseDto> Academies { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class AcademyLocationResponseDto
    {
        public int Id { get; set; }
        public int AcademyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public bool IsMain { get; set; }
    }
}
