using Koralytics.Application.DTOs.AuthDTOs.LoginDTOs;
using Koralytics.Application.DTOs.AuthDTOs.RegisterDTOs;

namespace Koralytics.Application.Services.Auth.Login
{
    public interface IAuthService
    {
        Task<AuthResultDto> LoginAsync(LoginRequestDto request);
        Task<AuthResultDto> RefreshTokenAsync(string refreshToken);
        Task ChangePasswordAsync(int userId, ChangePasswordRequestDto request);
        Task LogoutAsync(string refreshToken);
        Task LogoutAllAsync(int userId);
        Task ForgotPasswordAsync(ForgotPasswordRequestDto request);
        Task ResetPasswordAsync(ResetPasswordRequestDto request);
        
        // OAuth
        Task<OAuthLoginResult> OAuthLoginOrRegisterAsync(OAuthLoginRequestDto request);
        Task<AuthResultDto> CompleteOAuthProfileAsPlayerAsync(int userId, CompleteProfileAsPlayerDto request);
        Task<AuthResultDto> CompleteOAuthProfileAsCoachAsync(int userId, CompleteProfileAsCoachDto request);
        Task<AuthResultDto> CompleteOAuthProfileAsScouterAsync(int userId, CompleteProfileAsScouterDto request);
        Task<AuthResultDto> CompleteOAuthProfileAsParentAsync(int userId, CompleteProfileAsParentDto request);
        Task<AuthResultDto> CompleteOAuthProfileAsAcademyAdminAsync(int userId, CompleteProfileAsAcademyAdminDto request);
    }
}
