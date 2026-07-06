using Koralytics.Application.DTOs.AuthDTOs.LoginDTOs;
using Koralytics.Application.DTOs.AuthDTOs.RegisterDTOs;

namespace Koralytics.Application.Services.Auth.Register
{
    public interface IRegistrationService
    {
        Task<AuthResponseDto> RegisterPlayerAsync(RegisterPlayerRequestDto request);
        Task<AuthResponseDto> RegisterCoachAsync(RegisterCoachRequestDto request);
        Task<AuthResponseDto> RegisterScouterAsync(RegisterScouterRequestDto request);
        Task<AuthResponseDto> RegisterParentAsync(RegisterParentRequestDto request);
        Task<AuthResponseDto> RegisterAcademyAdminAsync(RegisterAcademyAdminRequestDto request);
    }
}
