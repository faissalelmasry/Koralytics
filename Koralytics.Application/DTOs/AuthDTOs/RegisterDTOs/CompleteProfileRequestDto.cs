using System;

namespace Koralytics.Application.DTOs.AuthDTOs.RegisterDTOs
{
    public class CompleteProfileRequestDto
    {
        public string Role { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string? PhoneNumber { get; set; }
        
        // Player
        public int? AcademyId { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Nationality { get; set; }
        public string? PreferredFoot { get; set; }
        public int? WeakFootRating { get; set; }
        
        // Parent
        public int? ChildPlayerId { get; set; }
    }
}
