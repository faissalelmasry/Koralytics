namespace Koralytics.Application.DTOs.AuthDTOs.LoginDTOs
{
    public class LoginRequestDto
    {
        public string EmailOrUserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
