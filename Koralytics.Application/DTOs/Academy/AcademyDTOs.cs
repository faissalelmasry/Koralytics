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

    // ─── Response DTOs ───────────────────────────────────────────────────────

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
