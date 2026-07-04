using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Koralytics.Application.DTOs.AuthDTOs.LoginDTOs;
using Koralytics.Application.Interfaces;
using Koralytics.Domain.Entities.Coach;
using Koralytics.Domain.Entities.Identity;
using Koralytics.Domain.Entities.Player;
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
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
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
            if (string.IsNullOrWhiteSpace(request.EmailOrUserName) || string.IsNullOrWhiteSpace(request.Password))
            {
                _logger.LogWarning("Login attempt with missing credentials (email/username or password).");
                throw new BadRequestException("Email/username and password are required.");
            }

            var user = await _userManager.FindByEmailAsync(request.EmailOrUserName)
                ?? await _userManager.FindByNameAsync(request.EmailOrUserName);

            if (user is null)
            {
                _logger.LogWarning("Login attempt for non-existent user: {emailOrUserName}", request.EmailOrUserName);
                throw new UnauthorizedException("Invalid credentials.");
            }

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

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:AccessTokenMinutes"] ?? "60")),
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = string.Join(' ', new[] { user.FirstName, user.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))),
                Roles = roles
            };
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

                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user is null)
                {
                    _logger.LogWarning("Token refresh attempted for non-existent user: {userId}", userId);
                    throw new NotFoundException("User not found.");
                }

                var roles = (await _userManager.GetRolesAsync(user)).ToList();
                var academyId = await GetAcademyIdAsync(user, roles);
                var newAccessToken = CreateToken(user, roles, academyId, tokenType: "access");
                var newRefreshToken = CreateToken(user, roles, academyId, tokenType: "refresh");

                _logger.LogInformation("Token refreshed for user: {userId}", user.Id);

                return new AuthResponseDto
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:AccessTokenMinutes"] ?? "60")),
                    UserId = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    FullName = string.Join(' ', new[] { user.FirstName, user.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))),
                    Roles = roles
                };
            }
            catch (UnauthorizedException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
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
            if (request.NewPassword != request.ConfirmPassword)
            {
                _logger.LogWarning("Password change attempt with mismatched new password and confirmation for user: {userId}", userId);
                throw new BadRequestException("New password and confirmation do not match.");
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                _logger.LogWarning("Password change attempted for non-existent user: {userId}", userId);
                throw new NotFoundException("User not found.");
            }

            var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                var errorMessage = result.Errors.FirstOrDefault()?.Description ?? "Unable to change password.";
                _logger.LogWarning("Failed password change for user: {userId}. Error: {error}", userId, errorMessage);
                throw new BadRequestException(errorMessage);
            }

            _logger.LogInformation("Password changed successfully for user: {userId}", userId);
        }

        private string CreateToken(User user, IReadOnlyCollection<string> roles, int? academyId, string tokenType)
        {
            var key = _configuration["Jwt:Key"] ?? "SuperSecretJwtKeyForDevelopmentOnly!123456";
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
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

            var accessTokenMinutes = int.Parse(_configuration["Jwt:AccessTokenMinutes"] ?? "60");
            var refreshTokenDays = int.Parse(_configuration["Jwt:RefreshTokenDays"] ?? "7");

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

            return tokenString;
        }

        private ClaimsPrincipal ValidateToken(string token, string expectedTokenType)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "SuperSecretJwtKeyForDevelopmentOnly!123456");
            var clockSkewMinutes = int.Parse(_configuration["Jwt:ClockSkewMinutes"] ?? "1");

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

        private async Task<int?> GetAcademyIdAsync(User user, IReadOnlyCollection<string> roles)
        {
            if (roles.Contains("Player"))
            {
                var playerAcademy = await _unitOfWork.Repository<PlayerAcademy>()
                    .FindAsNoTrackingAsync(x => x.PlayerId == user.Id);
                return playerAcademy?.AcademyId;
            }

            if (roles.Contains("Coach"))
            {
                var coachAcademy = await _unitOfWork.Repository<CoachAcademy>()
                    .FindAsNoTrackingAsync(x => x.CoachUserId == user.Id);
                return coachAcademy?.AcademyId;
            }

            return null;
        }
    }
}
