using System;

namespace Koralytics.Application.DTOs.AuthDTOs.RegisterDTOs
{
    public class CompleteProfileBaseDto
    {
        public string? UserName { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class CompleteProfileAsPlayerDto : CompleteProfileBaseDto
    {
        public int? AcademyId { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Nationality { get; set; }
        public string? PreferredFoot { get; set; }
        public int? WeakFootRating { get; set; }
    }

    public class CompleteProfileAsCoachDto : CompleteProfileBaseDto
    {
        public int? AcademyId { get; set; }
    }

    public class CompleteProfileAsAcademyAdminDto : CompleteProfileBaseDto
    {
        public int AcademyId { get; set; }
    }

    public class CompleteProfileAsParentDto : CompleteProfileBaseDto
    {
        public int ChildPlayerId { get; set; }
    }

    public class CompleteProfileAsScouterDto : CompleteProfileBaseDto
    {
    }
}
