using Koralytics.API.Controllers.BaseController;
using Koralytics.Application.DTOs.AuthDTOs.RegisterDTOs;
using Koralytics.Application.Interfaces.Auth;
using Koralytics.Application.Services.Auth.Register;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Koralytics.API.Controllers.Auth
{
    [Route("api/[controller]")]
    public class RegisterController : ApiBaseController
    {
        private readonly IRegistrationService _registrationService;
        private readonly ICookieService _cookieService;

        public RegisterController(IRegistrationService registrationService, ICookieService cookieService)
        {
            _registrationService = registrationService;
            _cookieService = cookieService;
        }

        [HttpPost("player")]
        public async Task<IActionResult> RegisterPlayer([FromBody] RegisterPlayerRequestDto request)
        {
            var result = await _registrationService.RegisterPlayerAsync(request);
            _cookieService.SetAuthCookies(Response, result.Tokens);
            return CreatedResponse(result.User, nameof(RegisterPlayer), null, "Player registered successfully.");
        }

        [HttpPost("coach")]
        public async Task<IActionResult> RegisterCoach([FromBody] RegisterCoachRequestDto request)
        {
            var result = await _registrationService.RegisterCoachAsync(request);
            _cookieService.SetAuthCookies(Response, result.Tokens);
            return CreatedResponse(result.User, nameof(RegisterCoach), null, "Coach registered successfully.");
        }

        [HttpPost("scouter")]
        public async Task<IActionResult> RegisterScouter([FromBody] RegisterScouterRequestDto request)
        {
            var result = await _registrationService.RegisterScouterAsync(request);
            _cookieService.SetAuthCookies(Response, result.Tokens);
            return CreatedResponse(result.User, nameof(RegisterScouter), null, "Scouter registered successfully. Waiting for verification.");
        }

        [HttpPost("parent")]
        public async Task<IActionResult> RegisterParent([FromBody] RegisterParentRequestDto request)
        {
            var result = await _registrationService.RegisterParentAsync(request);
            _cookieService.SetAuthCookies(Response, result.Tokens);
            return CreatedResponse(result.User, nameof(RegisterParent), null, "Parent registered successfully.");
        }

        [HttpPost("academy-admin")]
        public async Task<IActionResult> RegisterAcademyAdmin([FromBody] RegisterAcademyAdminRequestDto request)
        {
            var result = await _registrationService.RegisterAcademyAdminAsync(request);
            _cookieService.SetAuthCookies(Response, result.Tokens);
            return CreatedResponse(result.User, nameof(RegisterAcademyAdmin), null, "Academy Admin registered successfully.");
        }
    }
}
