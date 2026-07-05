namespace Koralytics.Application.DTOs.AuthDTOs.RegisterDTOs
{
    public class RegisterParentRequestDto : BaseRegistrationRequestDto
    {
        public int ChildPlayerId { get; set; }
    }
}
