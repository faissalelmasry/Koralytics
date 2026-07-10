using Koralytics.API.Controllers.BaseController;
using Koralytics.Application.DTOs.Academies;
using Koralytics.Application.Services.Academy.AcademyService;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;

namespace Koralytics.API.Controllers.Academies
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class AcademyController : ApiBaseController
    {
        private readonly IAcademyService _academyService;

        public AcademyController(IAcademyService academyService)
        {
            _academyService = academyService;
        }

        [HttpPost]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> CreateAcademy([FromBody] CreateAcademyDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _academyService.CreateAcademyAsync(dto, userId);
            
            return CreatedResponse(
                result, 
                nameof(GetAcademy), 
                new { academyId = result.Id }, 
                "Academy created successfully.");
        }

        [HttpGet]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> GetAllAcademies()
        {
            var result = await _academyService.GetAllAcademiesAsync();
            return OkResponse(result, "Academies retrieved successfully.");
        }


        [HttpGet("{academyId}")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> GetAcademy(int academyId)
        {
            var result = await _academyService.GetAcademyAsync(academyId);
            return OkResponse(result, "Academy retrieved successfully.");
        }

        [HttpPut("{academyId}")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> UpdateAcademy(int academyId, [FromBody] UpdateAcademyDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _academyService.UpdateAcademyAsync(academyId, dto, userId);
            return OkResponse(result, "Academy updated successfully.");
        }

        [HttpPost("{academyId}/locations")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> AddLocation(int academyId, [FromBody] AddLocationDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _academyService.AddLocationAsync(academyId, dto, userId);
            return OkResponse(result, "Location added successfully.");
        }

        [HttpGet("{academyId}/locations")]
        [Authorize(Roles = "Admin,SystemAdmin,Coach")]
        public async Task<IActionResult> GetLocations(int academyId)
        {
            var result = await _academyService.GetLocationsAsync(academyId);
            return OkResponse(result, "Locations retrieved successfully.");
        }

        [HttpPut("{academyId}/locations/{locationId}/set-main")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> SetMainLocation(int academyId, int locationId)
        {
            var userId = GetCurrentUserId();
            await _academyService.SetMainLocationAsync(academyId, locationId, userId);
            return NoContentResponse("Location set as main successfully.");
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user token");
            }
            return userId;
        }
    }
}
