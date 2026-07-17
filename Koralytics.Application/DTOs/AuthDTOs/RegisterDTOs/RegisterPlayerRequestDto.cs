namespace Koralytics.Application.DTOs.AuthDTOs.RegisterDTOs
{
    public class RegisterPlayerRequestDto : BaseRegistrationRequestDto
    {
        public DateTime DateOfBirth { get; set; }
        public string? Nationality { get; set; }
        public string PreferredFoot { get; set; } = "Right";
        public int WeakFootRating { get; set; } = 3;
        public string? PlayStyleTag { get; set; }
        public string? ArchetypePlayerName { get; set; }
        public string? ArchetypeText { get; set; }
    }
}
