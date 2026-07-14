namespace Koralytics.Application.DTOs.AuthDTOs.LoginDTOs
{
    public class OAuthLoginRequestDto
    {
        public string Provider { get; set; } = string.Empty;
        public string IdToken { get; set; } = string.Empty;
    }
}
