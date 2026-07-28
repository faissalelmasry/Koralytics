using System;
using System.Collections.Generic;
using Koralytics.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Koralytics.Application.DTOs.ProfileManagement
{
    public class PlayerPositionDto
    {
        public string Position { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
    }

    public class BaseUserProfileResponseDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class PlayerProfileResponseDto : BaseUserProfileResponseDto
    {
        public DateTime? DateOfBirth { get; set; }
        public int? Age { get; set; }
        public string? Nationality { get; set; }
        public PreferredFoot? PreferredFoot { get; set; }
        public int? WeakFootRating { get; set; }
        public string? PlayStyleTag { get; set; }
        public string? ArchetypePlayerName { get; set; }
        public string? ArchetypeText { get; set; }
        public AvailabilityStatus? AvailabilityStatus { get; set; }
        public List<PlayerPositionDto> Positions { get; set; } = new();
    }

    public class ScouterProfileResponseDto : BaseUserProfileResponseDto
    {
        public bool? IsVerified { get; set; }
        public DateTime? VerifiedAt { get; set; }
    }

    public class AcademyAdminProfileResponseDto : BaseUserProfileResponseDto
    {
        public int? AcademyId { get; set; }
        public string? AcademyName { get; set; }
    }

    public class CoachProfileResponseDto : BaseUserProfileResponseDto
    {
    }

    public class ParentProfileResponseDto : BaseUserProfileResponseDto
    {
    }

    public class SystemAdminProfileResponseDto : BaseUserProfileResponseDto
    {
    }

    public class UpdateProfileRequestDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }

        // Player-specific properties
        public string? Nationality { get; set; }
        public PreferredFoot? PreferredFoot { get; set; }
        public int? WeakFootRating { get; set; }
        public string? PlayStyleTag { get; set; }
        public string? ArchetypePlayerName { get; set; }
        public string? ArchetypeText { get; set; }
        public List<PlayerPositionDto>? Positions { get; set; }
    }

    public class UpdateProfileImageDto
    {
        public IFormFile Image { get; set; } = default!;
    }
}
