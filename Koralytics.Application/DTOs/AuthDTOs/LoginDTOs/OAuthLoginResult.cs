namespace Koralytics.Application.DTOs.AuthDTOs.LoginDTOs
{
    public class OAuthLoginResult
    {
        public bool RequiresProfileCompletion { get; set; }
        public AuthResultDto? AuthResult { get; set; }
        public int? UserId { get; set; }
        public string? TemporaryToken { get; set; }
    }
}
