using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentValidation;

using Koralytics.Application.Common;
using Koralytics.Application.DTOs.AuthDTOs.LoginDTOs;
using Koralytics.Application.DTOs.AuthDTOs.RegisterDTOs;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Services.Auth.OAuth;
using Koralytics.Application.Services.Auth.Register;
using Koralytics.Application.Services.Auth.Token;
using Koralytics.Application.Validators.UserBusiness;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Coach;
using Koralytics.Domain.Entities.Identity;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Exceptions;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Koralytics.Application.Interfaces.Email;

namespace Koralytics.Application.Services.Auth.Login
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly IOAuthProviderFactory _oauthProviderFactory;
        private readonly IValidator<LoginRequestDto> _loginValidator;
        private readonly IValidator<ChangePasswordRequestDto> _changePasswordValidator;
        private readonly IValidator<ForgotPasswordRequestDto> _forgotPasswordValidator;
        private readonly IValidator<ResetPasswordRequestDto> _resetPasswordValidator;
        private readonly IUserBusinessValidator _businessValidator;
        private readonly ILogger<AuthService> _logger;
        private readonly IRegistrationService _registrationService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IUnitOfWork unitOfWork,
            ITokenService tokenService,
            IOAuthProviderFactory oauthProviderFactory,
            IValidator<LoginRequestDto> loginValidator,
            IValidator<ChangePasswordRequestDto> changePasswordValidator,
            IValidator<ForgotPasswordRequestDto> forgotPasswordValidator,
            IValidator<ResetPasswordRequestDto> resetPasswordValidator,
            IUserBusinessValidator businessValidator,
            ILogger<AuthService> logger,
            IRegistrationService registrationService,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _oauthProviderFactory = oauthProviderFactory;
            _loginValidator = loginValidator;
            _changePasswordValidator = changePasswordValidator;
            _forgotPasswordValidator = forgotPasswordValidator;
            _resetPasswordValidator = resetPasswordValidator;
            _businessValidator = businessValidator;
            _logger = logger;
            _registrationService = registrationService;
            _emailService = emailService;
            _configuration = configuration;
        }

        public async Task<AuthResultDto> LoginAsync(LoginRequestDto request)
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

            return await GenerateAuthResultAsync(user);
        }

        public async Task<AuthResultDto> RefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new BadRequestException("Refresh token is required.");
            }

            // TokenService validates the token, performs rotation, and returns the new pair
            var tokens = await _tokenService.RefreshTokensAsync(refreshToken, GetAcademyIdAsync);

            // Load the user for the response DTO — extract user ID from the new access token claims
            var principal = _tokenService.ValidateAccessToken(tokens.AccessToken);
            var userIdStr = principal.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdStr, out var userId))
            {
                throw new UnauthorizedException("Could not determine user from refreshed token.");
            }

            var user = await _businessValidator.GetUserOrThrowAsync(userId);
            // Reload roles from UserManager so they're always up-to-date
            var roles = (await _userManager.GetRolesAsync(user)).ToList();

            var academyId = await GetAcademyIdAsync(user, roles);
            var response = BuildAuthResponse(user, roles, academyId, tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiresAt, tokens.RefreshTokenExpiresAt);

            _logger.LogInformation("Tokens refreshed for user: {userId} ({email})", user.Id, user.Email);
            return new AuthResultDto(response, tokens);
        }

        public async Task ChangePasswordAsync(int userId, ChangePasswordRequestDto request)
        {
            var validationResult = await _changePasswordValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join(" ", validationResult.Errors.Select(e => e.ErrorMessage));
                throw new BadRequestException(errorMessage);
            }

            var user = await _businessValidator.GetUserOrThrowAsync(userId);

            var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                var errorMessage = result.Errors.FirstOrDefault()?.Description ?? "Unable to change password.";
                throw new BadRequestException(errorMessage);
            }

            // Revoke all existing sessions so the user has to log in again with new password
            await _tokenService.RevokeAllUserTokensAsync(userId, "PasswordChanged");
            _logger.LogInformation("Password changed successfully for user: {userId}. Sessions revoked.", userId);
        }

        public async Task LogoutAsync(string refreshToken)
        {
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await _tokenService.RevokeRefreshTokenAsync(refreshToken, "ManualRevoke");
            }
        }

        public async Task LogoutAllAsync(int userId)
        {
            await _tokenService.RevokeAllUserTokensAsync(userId, "ManualRevoke");
        }

        public async Task<OAuthLoginResult> OAuthLoginOrRegisterAsync(OAuthLoginRequestDto request)
        {
            var provider = _oauthProviderFactory.GetProvider(request.Provider);
            var userInfo = await provider.GetUserInfoAsync(request.IdToken);

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.GoogleId == userInfo.ProviderId)
                       ?? await _userManager.FindByEmailAsync(userInfo.Email);

            if (user == null)
            {
                // New user - create account but no role yet
                user = new User
                {
                    Email = userInfo.Email,
                    UserName = userInfo.Email, // default username
                    FirstName = userInfo.FirstName,
                    LastName = userInfo.LastName,
                    ProfileImageUrl = userInfo.ProfileImageUrl,
                    GoogleId = userInfo.ProviderId,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    throw new BadRequestException("Failed to create OAuth user.");
                }
                
                // Return requires profile completion along with a temporary token
                var tempTokens = await _tokenService.GenerateTokenPairAsync(user, new List<string> { "PendingProfile" }, null);
                return new OAuthLoginResult
                {
                    RequiresProfileCompletion = true,
                    UserId = user.Id,
                    TemporaryToken = tempTokens.AccessToken
                };
            }

            // Existing user - link Google ID if missing
            if (string.IsNullOrEmpty(user.GoogleId))
            {
                user.GoogleId = userInfo.ProviderId;
                await _userManager.UpdateAsync(user);
            }

            var roles = await _userManager.GetRolesAsync(user);
            
            if (!roles.Any() || roles.Contains("PendingProfile"))
            {
                // User was created but didn't finish the second step
                var tempTokens = await _tokenService.GenerateTokenPairAsync(user, new List<string> { "PendingProfile" }, null);
                return new OAuthLoginResult
                {
                    RequiresProfileCompletion = true,
                    UserId = user.Id,
                    TemporaryToken = tempTokens.AccessToken
                };
            }

            // Normal login
            var authResult = await GenerateAuthResultAsync(user, roles.ToList());
            return new OAuthLoginResult
            {
                RequiresProfileCompletion = false,
                AuthResult = authResult
            };
        }

        public async Task<AuthResultDto> CompleteOAuthProfileAsPlayerAsync(int userId, CompleteProfileAsPlayerDto request)
        {
            var user = await PreCompleteProfileChecksAsync(userId, request);
            await _registrationService.CompleteProfileAsPlayerAsync(user, request);
            return await GenerateAuthResultAsync(user, new List<string> { AuthConstants.Roles.Player });
        }

        public async Task<AuthResultDto> CompleteOAuthProfileAsCoachAsync(int userId, CompleteProfileAsCoachDto request)
        {
            var user = await PreCompleteProfileChecksAsync(userId, request);
            await _registrationService.CompleteProfileAsCoachAsync(user, request);
            return await GenerateAuthResultAsync(user, new List<string> { AuthConstants.Roles.Coach });
        }

        public async Task<AuthResultDto> CompleteOAuthProfileAsScouterAsync(int userId, CompleteProfileAsScouterDto request)
        {
            var user = await PreCompleteProfileChecksAsync(userId, request);
            await _registrationService.CompleteProfileAsScouterAsync(user, request);
            return await GenerateAuthResultAsync(user, new List<string> { AuthConstants.Roles.Scouter });
        }

        public async Task<AuthResultDto> CompleteOAuthProfileAsParentAsync(int userId, CompleteProfileAsParentDto request)
        {
            var user = await PreCompleteProfileChecksAsync(userId, request);
            await _registrationService.CompleteProfileAsParentAsync(user, request);
            return await GenerateAuthResultAsync(user, new List<string> { AuthConstants.Roles.Parent });
        }

        public async Task<AuthResultDto> CompleteOAuthProfileAsAcademyAdminAsync(int userId, CompleteProfileAsAcademyAdminDto request)
        {
            var user = await PreCompleteProfileChecksAsync(userId, request);
            await _registrationService.CompleteProfileAsAcademyAdminAsync(user, request);
            return await GenerateAuthResultAsync(user, new List<string> { AuthConstants.Roles.AcademyAdmin });
        }

        private async Task<User> PreCompleteProfileChecksAsync(int userId, CompleteProfileBaseDto request)
        {
            var user = await _businessValidator.GetUserOrThrowAsync(userId);
            
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Any(r => r != "PendingProfile"))
            {
                throw new BadRequestException("Profile is already completed.");
            }

            if (!string.IsNullOrEmpty(request.UserName))
            {
                await _businessValidator.EnsureUsernameNotExistsAsync(request.UserName);
                user.UserName = request.UserName;
            }
            if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                user.PhoneNumber = request.PhoneNumber;
            }

            await _userManager.UpdateAsync(user);
            return user;
        }

        private async Task<AuthResultDto> GenerateAuthResultAsync(User user, IList<string>? preFetchedRoles = null)
        {
            var roles = preFetchedRoles ?? (await _userManager.GetRolesAsync(user)).ToList();
            var academyId = await GetAcademyIdAsync(user, roles);
            var tokens = await _tokenService.GenerateTokenPairAsync(user, roles, academyId);
            var response = BuildAuthResponse(user, roles, academyId, tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiresAt, tokens.RefreshTokenExpiresAt);
            
            _logger.LogInformation("Successful authentication for user: {userId} ({email}), roles: {roles}", user.Id, user.Email, string.Join(", ", roles));
            return new AuthResultDto(response, tokens);
        }

        private static AuthResponseDto BuildAuthResponse(User user, IList<string> roles, int? academyId, string accessToken, string refreshToken, DateTime accessExpiresAt, DateTime refreshExpiresAt)
        {
            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresAt = accessExpiresAt,
                RefreshTokenExpiresAt = refreshExpiresAt,
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = string.Join(' ', new[] { user.FirstName, user.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))),
                AcademyId = academyId,
                Roles = roles
            };
        }

        private async Task<int?> GetAcademyIdAsync(User user, IList<string> roles)
        {
            if (roles.Contains(AuthConstants.Roles.Player))
            {
                var playerAcademy = await _unitOfWork.Repository<PlayerAcademy>()
                    .FindAsNoTrackingAsync(x => x.PlayerId == user.Id && x.LeftAt == null);
                return playerAcademy?.AcademyId;
            }

            if (roles.Contains(AuthConstants.Roles.Coach))
            {
                var coachAcademy = await _unitOfWork.Repository<CoachAcademy>()
                    .FindAsNoTrackingAsync(x => x.CoachUserId == user.Id && x.LeftAt == null);
                return coachAcademy?.AcademyId;
            }
            
            if (roles.Contains(AuthConstants.Roles.AcademyAdmin))
            {
                var admin = await _unitOfWork.Repository<AcademyAdmin>()
                    .FindAsNoTrackingAsync(x => x.Id == user.Id);
                return admin?.AcademyId;
            }

            return null;
        }
        public async Task ForgotPasswordAsync(ForgotPasswordRequestDto request)
        {
            await _forgotPasswordValidator.ValidateAndThrowAsync(request);

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                // We return immediately to avoid email enumeration attacks
                return;
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = Uri.EscapeDataString(token);

            var frontendUrl = _configuration["FrontendBaseUrl"] ?? "http://localhost:3000";
            var separator = frontendUrl.Contains('?') ? "&" : "?";
            var finalUrl = $"{frontendUrl}/auth/reset-password{separator}email={Uri.EscapeDataString(request.Email)}&token={encodedToken}";

            await _emailService.SendPasswordResetAsync(user.Email!, user.FirstName ?? user.UserName!, finalUrl);
        }

        public async Task ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            await _resetPasswordValidator.ValidateAndThrowAsync(request);

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                throw new BadRequestException("Invalid request.");
            }

            var decodedToken = Uri.UnescapeDataString(request.Token);
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new BadRequestException($"Password reset failed: {errors}");
            }
        }
    }
}