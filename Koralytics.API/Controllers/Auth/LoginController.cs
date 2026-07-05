using System.Security.Claims;

using Koralytics.Application.DTOs.AuthDTOs.LoginDTOs;
using Koralytics.Application.Services.Auth.Login;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koralytics.API.Controllers.Auth
{
    /// <summary>
    /// Provides endpoints for user authentication including login, token refresh, and password management.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class LoginController : ControllerBase
    {
        private readonly IAuthService _authService;

        /// <summary>
        /// Initializes a new instance of the LoginController.
        /// </summary>
        /// <param name="authService">The authentication service.</param>
        public LoginController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Authenticates a user with email or username and password.
        /// </summary>
        /// <param name="request">The login credentials.</param>
        /// <returns>JWT tokens and user information on successful authentication.</returns>
        /// <response code="200">Returns the authentication response with access and refresh tokens.</response>
        /// <response code="400">Returned when credentials are missing or invalid format.</response>
        /// <response code="401">Returned when authentication fails.</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Refreshes the authentication tokens using a valid refresh token.
        /// </summary>
        /// <param name="request">The refresh token request.</param>
        /// <returns>New JWT tokens on successful refresh.</returns>
        /// <response code="200">Returns the refreshed authentication response.</response>
        /// <response code="400">Returned when refresh token is missing.</response>
        /// <response code="401">Returned when refresh token is invalid or expired.</response>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            var response = await _authService.RefreshTokenAsync(request.RefreshToken);
            return Ok(response);
        }

        /// <summary>
        /// Changes the password for the authenticated user.
        /// </summary>
        /// <param name="request">The password change request with old and new passwords.</param>
        /// <returns>Confirmation message on successful password change.</returns>
        /// <response code="200">Password changed successfully.</response>
        /// <response code="400">Returned when passwords don't match or validation fails.</response>
        /// <response code="401">Returned when user is not authenticated.</response>
        /// <response code="404">Returned when user is not found.</response>
        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid or missing user identifier." });
            }

            await _authService.ChangePasswordAsync(userId, request);
            return Ok(new { message = "Password changed successfully." });
        }

        // test endpoint to check if the user is authenticated
        [HttpGet("test-auth")]
        [Authorize]
        public IActionResult TestAuth()
        {
            return Ok(new { message = "User is authenticated." });
        }

    }
}
