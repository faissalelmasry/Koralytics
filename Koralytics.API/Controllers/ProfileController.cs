using Koralytics.API.Controllers.BaseController;
using Koralytics.Application.DTOs.ProfileManagement;
using Koralytics.Application.Services.ProfileManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Koralytics.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProfileController : ApiBaseController
    {
        private readonly IProfileManagementService _profileService;

        public ProfileController(IProfileManagementService profileService)
        {
            _profileService = profileService;
        }

        /// <summary>
        /// Retrieves the profile of the currently authenticated user.
        /// Works for Player, Coach, Scouter, Academy Admin, System Admin, or Parent.
        /// </summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = GetCurrentUserId();
            var profile = await _profileService.GetMyProfileAsync(userId);
            return OkResponse<object>(profile, "Profile retrieved successfully.");
        }

        /// <summary>
        /// Retrieves the user profile for a given user ID.
        /// </summary>
        [HttpGet("{userId:int}")]
        public async Task<IActionResult> GetUserProfile(int userId)
        {
            var profile = await _profileService.GetMyProfileAsync(userId);
            return OkResponse<object>(profile, "Profile retrieved successfully.");
        }

        /// <summary>
        /// Updates the profile fields for the currently authenticated user (full record update).
        /// The frontend should pass all form fields; omitted/null optional fields will overwrite the existing record.
        /// </summary>
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequestDto dto)
        {
            var userId = GetCurrentUserId();
            var updatedProfile = await _profileService.UpdateProfileAsync(userId, dto);
            return OkResponse<object>(updatedProfile, "Profile updated successfully.");
        }

        /// <summary>
        /// Updates the profile image for the currently authenticated user in a dedicated endpoint.
        /// Deletes the old image if one existed.
        /// </summary>
        [HttpPatch("me/image")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateProfileImage([FromForm] UpdateProfileImageDto dto)
        {
            var userId = GetCurrentUserId();
            var imageUrl = await _profileService.UpdateProfileImageAsync(userId, dto.Image);
            return OkResponse(imageUrl, "Profile image updated successfully.");
        }

        /// <summary>
        /// Deletes the profile image for the currently authenticated user.
        /// </summary>
        [HttpDelete("me/image")]
        public async Task<IActionResult> RemoveProfileImage()
        {
            var userId = GetCurrentUserId();
            await _profileService.RemoveProfileImageAsync(userId);
            return OkResponse<object>(null, "Profile image removed successfully.");
        }
    }
}
