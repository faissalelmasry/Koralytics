using System.Threading.Tasks;
using Koralytics.Application.DTOs.ProfileManagement;
using Microsoft.AspNetCore.Http;

namespace Koralytics.Application.Services.ProfileManagement
{
    public interface IProfileManagementService
    {
        Task<BaseUserProfileResponseDto> GetMyProfileAsync(int userId);
        Task<BaseUserProfileResponseDto> UpdateProfileAsync(int userId, UpdateProfileRequestDto dto);
        Task<string> UpdateProfileImageAsync(int userId, IFormFile image);
    }
}
