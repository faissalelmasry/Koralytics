using Koralytics.Application.DTOs.AuthDTOs.LoginDTOs;

namespace Koralytics.Application.Services.Auth.Login
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
        Task ChangePasswordAsync(int userId, ChangePasswordRequestDto request);
    }
}
