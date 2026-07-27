using System.Threading.Tasks;
using Koralytics.Application.DTOs.SystemAdmin;

namespace Koralytics.Application.Services.SystemAdmin.UserManagement
{
    public interface IUserManagementService
    {
        Task<UserListResponseDto> GetUsersAsync(UserListRequestDto request);
        Task<UserDetailDto> GetUserByIdAsync(int userId);
        Task<UserSummaryDto> UpdateUserRolesAsync(int userId, UpdateUserRolesDto dto, int currentUserId);
        Task ToggleUserStatusAsync(int userId, bool isDeleted, int currentUserId);
    }
}
