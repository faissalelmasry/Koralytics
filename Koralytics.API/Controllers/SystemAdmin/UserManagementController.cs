using System.Threading.Tasks;
using Koralytics.API.Controllers.BaseController;
using Koralytics.Application.DTOs.SystemAdmin;
using Koralytics.Application.Services.SystemAdmin.UserManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koralytics.API.Controllers.SystemAdmin
{
    [ApiController]
    [Route("api/SystemAdmin/users")]
    [Authorize(Roles = "SystemAdmin")]
    [Produces("application/json")]
    public class UserManagementController : ApiBaseController
    {
        private readonly IUserManagementService _userManagementService;

        public UserManagementController(IUserManagementService userManagementService)
        {
            _userManagementService = userManagementService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] UserListRequestDto request)
        {
            var result = await _userManagementService.GetUsersAsync(request);
            return OkResponse(result, "Users retrieved successfully.");
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var result = await _userManagementService.GetUserByIdAsync(id);
            return OkResponse(result, "User details retrieved successfully.");
        }

        [HttpPut("{id}/roles")]
        public async Task<IActionResult> UpdateUserRoles(int id, [FromBody] UpdateUserRolesDto dto)
        {
            var result = await _userManagementService.UpdateUserRolesAsync(id, dto, GetCurrentUserId());
            return OkResponse(result, "User roles updated successfully.");
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> ToggleUserStatus(int id, [FromBody] UpdateUserStatusDto dto)
        {
            await _userManagementService.ToggleUserStatusAsync(id, dto.IsDeleted, GetCurrentUserId());
            return OkResponse<object?>(null, "User status updated successfully.");
        }
    }
}
