using Koralytics.Application.DTOs.AuthDTOs.LoginDTOs;
using Koralytics.Application.DTOs.AuthDTOs.RegisterDTOs;
using Koralytics.Application.Services.Auth.Register;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koralytics.API.Controllers.Auth
{
    /// <summary>
    /// Provides endpoints for user registration of different roles (Player, Coach, Scouter, Parent).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class RegisterController : ControllerBase
    {
        private readonly IRegistrationService _registrationService;

        /// <summary>
        /// Initializes a new instance of the RegisterController.
        /// </summary>
        /// <param name="registrationService">The registration service.</param>
        public RegisterController(IRegistrationService registrationService)
        {
            _registrationService = registrationService;
        }

        /// <summary>
        /// Registers a new player account.
        /// </summary>
        /// <param name="request">The player registration details.</param>
        /// <returns>JWT tokens and user information on successful registration.</returns>
        /// <response code="200">Returns the authentication response with access and refresh tokens.</response>
        /// <response code="400">Returned when registration data is invalid or incomplete.</response>
        /// <response code="409">Returned when email or username is already registered.</response>
        [HttpPost("player")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AuthResponseDto>> RegisterPlayer([FromBody] RegisterPlayerRequestDto request)
        {
            var response = await _registrationService.RegisterPlayerAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Registers a new coach account.
        /// </summary>
        /// <param name="request">The coach registration details.</param>
        /// <returns>JWT tokens and user information on successful registration.</returns>
        /// <response code="200">Returns the authentication response with access and refresh tokens.</response>
        /// <response code="400">Returned when registration data is invalid or incomplete.</response>
        /// <response code="409">Returned when email or username is already registered.</response>
        [HttpPost("coach")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AuthResponseDto>> RegisterCoach([FromBody] RegisterCoachRequestDto request)
        {
            var response = await _registrationService.RegisterCoachAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Registers a new scouter account.
        /// </summary>
        /// <param name="request">The scouter registration details.</param>
        /// <returns>JWT tokens and user information on successful registration.</returns>
        /// <response code="200">Returns the authentication response with access and refresh tokens.</response>
        /// <response code="400">Returned when registration data is invalid or incomplete.</response>
        /// <response code="409">Returned when email or username is already registered.</response>
        [HttpPost("scouter")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AuthResponseDto>> RegisterScouter([FromBody] RegisterScouterRequestDto request)
        {
            var response = await _registrationService.RegisterScouterAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Registers a new parent account and links it to a player.
        /// </summary>
        /// <param name="request">The parent registration details with child player ID.</param>
        /// <returns>JWT tokens and user information on successful registration.</returns>
        /// <response code="200">Returns the authentication response with access and refresh tokens.</response>
        /// <response code="400">Returned when registration data is invalid or incomplete.</response>
        /// <response code="404">Returned when the child player is not found.</response>
        /// <response code="409">Returned when email or username is already registered.</response>
        [HttpPost("parent")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AuthResponseDto>> RegisterParent([FromBody] RegisterParentRequestDto request)
        {
            var response = await _registrationService.RegisterParentAsync(request);
            return Ok(response);
        }
    }
}
