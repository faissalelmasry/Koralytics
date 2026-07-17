using Koralytics.Application.DTOs.AuthDTOs.LoginDTOs;
using Koralytics.Application.DTOs.AuthDTOs.RegisterDTOs;
using Koralytics.Domain.Entities.Identity;

namespace Koralytics.Application.Services.Auth.Register
{
    public interface IRegistrationService
    {
        Task<AuthResultDto> RegisterPlayerAsync(RegisterPlayerRequestDto request);
        Task<AuthResultDto> RegisterCoachAsync(RegisterCoachRequestDto request);
        Task<AuthResultDto> RegisterScouterAsync(RegisterScouterRequestDto request);
        Task<AuthResultDto> RegisterParentAsync(RegisterParentRequestDto request);
        Task<AuthResultDto> RegisterAcademyAdminAsync(RegisterAcademyAdminRequestDto request);
        
        Task CompleteProfileAsPlayerAsync(User existingUser, CompleteProfileAsPlayerDto profileData);
        Task CompleteProfileAsCoachAsync(User existingUser, CompleteProfileAsCoachDto profileData);
        Task CompleteProfileAsScouterAsync(User existingUser, CompleteProfileAsScouterDto profileData);
        Task CompleteProfileAsParentAsync(User existingUser, CompleteProfileAsParentDto profileData);
        Task CompleteProfileAsAcademyAdminAsync(User existingUser, CompleteProfileAsAcademyAdminDto profileData);

        Task SendEmailConfirmationAsync(int userId);
        Task<bool> ConfirmEmailAsync(int userId, string token);
        Task<bool> IsEmailConfirmedAsync(int userId);
    }
}
