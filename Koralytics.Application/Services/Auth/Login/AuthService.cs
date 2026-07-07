using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using FluentValidation;

using Koralytics.Application.DTOs.AuthDTOs.LoginDTOs;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Services.Auth.Register;
using Koralytics.Application.Validators.UserBusiness;
using Koralytics.Domain.Entities.Coach;
using Koralytics.Domain.Entities.Identity;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Exceptions;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Koralytics.Application.Services.Auth.Login
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly IValidator<LoginRequestDto> _loginValidator;
        private readonly IValidator<ChangePasswordRequestDto> _changePasswordValidator;
        private readonly IUserBusinessValidator _businessValidator;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            IValidator<LoginRequestDto> loginValidator,
            IValidator<ChangePasswordRequestDto> changePasswordValidator,
            IUserBusinessValidator businessValidator,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _loginValidator = loginValidator;
            _changePasswordValidator = changePasswordValidator;
            _businessValidator = businessValidator;
            _logger = logger;
        }

        /// <summary>
        /// Authenticates a user with email/username and password.
        /// </summary>
        /// <param name="request">Login credentials.</param>
        /// <returns>Authentication response with JWT tokens.</returns>
        /// <exception cref="BadRequestException">Thrown when credentials are invalid.</exception>
        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
        {
            var validationResult = await _loginValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join(" ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Login validation failed: {errors}", errorMessage);
                throw new BadRequestException(errorMessage);
            }

            var user = await _businessValidator.GetUserByEmailOrUsernameOrThrowAsync(request.EmailOrUserName);

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed login attempt for user: {userId} ({emailOrUserName})", user.Id, user.Email ?? user.UserName);
                throw new UnauthorizedException("Invalid credentials.");
            }

            var roles = (await _userManager.GetRolesAsync(user)).ToList();
            var academyId = await GetAcademyIdAsync(user, roles);
            var accessToken = CreateToken(user, roles, academyId, tokenType: "access");
            var refreshToken = CreateToken(user, roles, academyId, tokenType: "refresh");

            _logger.LogInformation("Successful login for user: {userId} ({email}), roles: {roles}",
                user.Id, user.Email, string.Join(", ", roles));

            return BuildAuthResponse(user, roles, accessToken, refreshToken);
        }

        /// <summary>
        /// Refreshes authentication tokens using a valid refresh token.
        /// </summary>
        /// <param name="refreshToken">The refresh token.</param>
        /// <returns>New authentication response with updated tokens.</returns>
        /// <exception cref="BadRequestException">Thrown when refresh token is missing.</exception>
        /// <exception cref="UnauthorizedException">Thrown when refresh token is invalid or expired.</exception>
        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogWarning("Token refresh attempt with missing refresh token.");
                throw new BadRequestException("Refresh token is required.");
            }

            try
            {
                var principal = ValidateToken(refreshToken, expectedTokenType: "refresh");
                var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogWarning("Token refresh with invalid user ID claim in token.");
                    throw new UnauthorizedException("Invalid refresh token.");
                }

                var user = await _businessValidator.GetUserOrThrowAsync(userId);

                var roles = (await _userManager.GetRolesAsync(user)).ToList();
                var academyId = await GetAcademyIdAsync(user, roles);
                var newAccessToken = CreateToken(user, roles, academyId, tokenType: "access");
                var newRefreshToken = CreateToken(user, roles, academyId, tokenType: "refresh");

                _logger.LogInformation("Token refreshed for user: {userId}", user.Id);

                return BuildAuthResponse(user, roles, newAccessToken, newRefreshToken);
            }
            catch (Exception ex) when (ex is not (UnauthorizedException or NotFoundException))
            {
                _logger.LogError(ex, "Unexpected error during token refresh.");
                throw;
            }
        }

        /// <summary>
        /// Changes the password for an authenticated user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="request">Password change request with old and new passwords.</param>
        /// <exception cref="BadRequestException">Thrown when passwords don't match or change fails.</exception>
        /// <exception cref="NotFoundException">Thrown when user is not found.</exception>
        public async Task ChangePasswordAsync(int userId, ChangePasswordRequestDto request)
        {

            var validationResult = await _changePasswordValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join(" ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Change password validation failed for user {userId}: {errors}", userId, errorMessage);
                throw new BadRequestException(errorMessage);
            }

            var user = await _businessValidator.GetUserOrThrowAsync(userId);

            var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                var errorMessage = result.Errors.FirstOrDefault()?.Description ?? "Unable to change password.";
                _logger.LogWarning("Failed password change for user: {userId}. Error: {error}", userId, errorMessage);
                throw new BadRequestException(errorMessage);
            }

            _logger.LogInformation("Password changed successfully for user: {userId}", userId);
        }

        /// <summary>
        /// Builds the response DTO for a successful login or refresh, using the access
        /// token's own expiration rather than recomputing it, so the two can't drift apart.
        /// </summary>
        private static AuthResponseDto BuildAuthResponse(User user, IReadOnlyCollection<string> roles, TokenResult accessToken, TokenResult refreshToken)
        {
            return new AuthResponseDto
            {
                AccessToken = accessToken.Token,
                RefreshToken = refreshToken.Token,
                ExpiresAt = accessToken.ExpiresAt,
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = string.Join(' ', new[] { user.FirstName, user.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))),
                Roles = roles.ToList()
            };
        }

        private readonly record struct TokenResult(string Token, DateTime ExpiresAt);

        private TokenResult CreateToken(User user, IReadOnlyCollection<string> roles, int? academyId, string tokenType)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetRequiredJwtSigningKey()));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, string.Join(' ', new[] { user.FirstName, user.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)))),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new("username", user.UserName ?? string.Empty),
                new("tokenType", tokenType),
                new("academyId", academyId?.ToString() ?? string.Empty)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var accessTokenMinutes = GetConfigInt("Jwt:AccessTokenMinutes", defaultValue: 60);
            var refreshTokenDays = GetConfigInt("Jwt:RefreshTokenDays", defaultValue: 7);

            var expirationTime = tokenType == "refresh"
                ? DateTime.UtcNow.AddDays(refreshTokenDays)
                : DateTime.UtcNow.AddMinutes(accessTokenMinutes);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expirationTime,
                signingCredentials: credentials);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            _logger.LogDebug("Token created for user: {userId}, type: {tokenType}, expires: {expiration}",
                user.Id, tokenType, expirationTime);

            return new TokenResult(tokenString, expirationTime);
        }

        private ClaimsPrincipal ValidateToken(string token, string expectedTokenType)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(GetRequiredJwtSigningKey());
            var clockSkewMinutes = GetConfigInt("Jwt:ClockSkewMinutes", defaultValue: 1);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(clockSkewMinutes)
            };

            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                var tokenType = principal.FindFirst("tokenType")?.Value;
                if (!string.Equals(tokenType, expectedTokenType, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Token type mismatch. Expected: {expected}, Got: {actual}", expectedTokenType, tokenType);
                    throw new SecurityTokenException("Unexpected token type.");
                }

                return principal;
            }
            catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
            {
                _logger.LogWarning("Token validation failed: {message}", ex.Message);
                throw new UnauthorizedException("Invalid or expired refresh token.");
            }
        }

        /// <summary>
        /// Returns the configured JWT signing key. Throws rather than silently falling back
        /// to a hardcoded default: a missing key in any environment (including production
        /// misconfiguration) must fail loudly, since a default baked into source code would
        /// let anyone forge valid tokens for any user.
        /// </summary>
        private string GetRequiredJwtSigningKey()
        {
            var key = _configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(key))
            {
                _logger.LogCritical("Jwt:Key is not configured. Refusing to issue or validate tokens.");
                throw new InvalidOperationException("JWT signing key is not configured. Set 'Jwt:Key' in configuration.");
            }

            return key;
        }

        /// <summary>
        /// Reads an integer configuration value, falling back to <paramref name="defaultValue"/>
        /// if the key is missing or not a valid integer, instead of throwing FormatException
        /// at request time on a misconfigured value.
        /// </summary>
        private int GetConfigInt(string key, int defaultValue)
        {
            return int.TryParse(_configuration[key], out var value) ? value : defaultValue;
        }

        private async Task<int?> GetAcademyIdAsync(User user, IReadOnlyCollection<string> roles)
        {
            if (roles.Contains(RegistrationRoles.Player))
            {
                var playerAcademy = await _unitOfWork.Repository<PlayerAcademy>()
                    .FindAsNoTrackingAsync(x => x.PlayerId == user.Id && x.LeftAt==null);
                return playerAcademy?.AcademyId;
            }

            if (roles.Contains(RegistrationRoles.Coach))
            {
                var coachAcademy = await _unitOfWork.Repository<CoachAcademy>()
                    .FindAsNoTrackingAsync(x => x.CoachUserId == user.Id && x.LeftAt == null);
                return coachAcademy?.AcademyId;
            }
            if (roles.Contains(RegistrationRoles.AcademyAdmin))
            {
                var AcademyAdmin = await _unitOfWork.Repository<AcademyAdmin>()
                    .FindAsNoTrackingAsync(x => x.Id == user.Id);
                return AcademyAdmin?.AcademyId;
            }

            return null;
        }
    }
}