using Koralytics.API.Controllers.BaseController;
using Koralytics.Application.DTOs.AuthDTOs.LoginDTOs;
using Koralytics.Application.DTOs.AuthDTOs.RegisterDTOs;
using Koralytics.Application.Interfaces.Auth;
using Koralytics.Application.Services.Auth.Login;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Koralytics.API.Controllers.Auth
{
    [Route("api/[controller]")]
    public class AuthController : ApiBaseController
    {
        private readonly IAuthService _authService;
        private readonly ICookieService _cookieService;

        public AuthController(IAuthService authService, ICookieService cookieService)
        {
            _authService = authService;
            _cookieService = cookieService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var result = await _authService.LoginAsync(request);
            _cookieService.SetAuthCookies(Response, result.Tokens);
            return OkResponse(result.User, "Login successful");
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto? request)
        {
            var refreshToken = request?.RefreshToken;
            if (string.IsNullOrEmpty(refreshToken))
            {
                refreshToken = _cookieService.GetRefreshTokenFromCookie(Request);
            }
            if (string.IsNullOrEmpty(refreshToken))
            {
                return Unauthorized(new { message = "Refresh token is missing from cookies." });
            }

            var result = await _authService.RefreshTokenAsync(refreshToken);
            _cookieService.SetAuthCookies(Response, result.Tokens);
            return OkResponse(result.User, "Tokens refreshed successfully");
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _authService.ChangePasswordAsync(userId, request);
            
            // Password change revokes all tokens, so clear cookies on this client
            _cookieService.ClearAuthCookies(Response);
            return NoContentResponse("Password changed successfully. Please login again.");
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto? request)
        {
            var refreshToken = request?.RefreshToken;
            if (string.IsNullOrEmpty(refreshToken))
            {
                refreshToken = _cookieService.GetRefreshTokenFromCookie(Request);
            }
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _authService.LogoutAsync(refreshToken);
            }
            _cookieService.ClearAuthCookies(Response);
            return NoContentResponse("Logged out successfully.");
        }

        [Authorize]
        [HttpPost("logout-all")]
        public async Task<IActionResult> LogoutAll()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _authService.LogoutAllAsync(userId);
            _cookieService.ClearAuthCookies(Response);
            return NoContentResponse("Logged out from all devices successfully.");
        }

        [HttpPost("oauth")]
        public async Task<IActionResult> OAuthLogin([FromBody] OAuthLoginRequestDto request)
        {
            var result = await _authService.OAuthLoginOrRegisterAsync(request);
            if (result.RequiresProfileCompletion)
            {
                return OkResponse(result, "Profile completion required");
            }

            _cookieService.SetAuthCookies(Response, result.AuthResult!.Tokens);
            return OkResponse(result.AuthResult.User, "OAuth login successful");
        }

        [Authorize]
        [HttpPost("complete-profile/player")]
        public async Task<IActionResult> CompleteOAuthProfileAsPlayer([FromBody] CompleteProfileAsPlayerDto request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _authService.CompleteOAuthProfileAsPlayerAsync(userId, request);
            
            _cookieService.SetAuthCookies(Response, result.Tokens);
            return OkResponse(result.User, "Player profile completed successfully");
        }

        [Authorize]
        [HttpPost("complete-profile/coach")]
        public async Task<IActionResult> CompleteOAuthProfileAsCoach([FromBody] CompleteProfileAsCoachDto request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _authService.CompleteOAuthProfileAsCoachAsync(userId, request);
            
            _cookieService.SetAuthCookies(Response, result.Tokens);
            return OkResponse(result.User, "Coach profile completed successfully");
        }

        [Authorize]
        [HttpPost("complete-profile/scouter")]
        public async Task<IActionResult> CompleteOAuthProfileAsScouter([FromBody] CompleteProfileAsScouterDto request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _authService.CompleteOAuthProfileAsScouterAsync(userId, request);
            
            _cookieService.SetAuthCookies(Response, result.Tokens);
            return OkResponse(result.User, "Scouter profile completed successfully");
        }

        [Authorize]
        [HttpPost("complete-profile/parent")]
        public async Task<IActionResult> CompleteOAuthProfileAsParent([FromBody] CompleteProfileAsParentDto request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _authService.CompleteOAuthProfileAsParentAsync(userId, request);
            
            _cookieService.SetAuthCookies(Response, result.Tokens);
            return OkResponse(result.User, "Parent profile completed successfully");
        }

        [Authorize]
        [HttpPost("complete-profile/academy-admin")]
        public async Task<IActionResult> CompleteOAuthProfileAsAcademyAdmin([FromBody] CompleteProfileAsAcademyAdminDto request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _authService.CompleteOAuthProfileAsAcademyAdminAsync(userId, request);
            
            _cookieService.SetAuthCookies(Response, result.Tokens);
            return OkResponse(result.User, "Academy Admin profile completed successfully");
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email);
            var name = User.FindFirstValue(ClaimTypes.Name);
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            return OkResponse(new
            {
                UserId = userId,
                Email = email,
                Name = name,
                Roles = roles
            }, "Current user retrieved successfully.");
        }
    }
}
