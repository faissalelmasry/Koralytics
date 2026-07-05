using System.Globalization;
using System.Text.RegularExpressions;

using Koralytics.Application.DTOs.AuthDTOs.RegisterDTOs;
using Koralytics.Application.DTOs.AuthDTOs.LoginDTOs;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Services.Auth.Login;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Coach;
using Koralytics.Domain.Entities.Identity;
using Koralytics.Domain.Entities.Parents;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Entities.Scouter;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Koralytics.Application.Services.Auth.Register
{

    internal static class RegistrationRoles
    {
        public const string Player = "Player";
        public const string Coach = "Coach";
        public const string Scouter = "Scouter";
        public const string Parent = "Parent";
    }


    public class RegistrationService : IRegistrationService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;
        private readonly ILogger<RegistrationService> _logger;

        // Password validation constraints
        private const int MinimumPasswordLength = 8;

        
        private const string PasswordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$";

        public RegistrationService(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            IUnitOfWork unitOfWork,
            IAuthService authService,
            ILogger<RegistrationService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Registers a new player account.
        /// </summary>
        /// <param name="request">The player registration details.</param>
        /// <returns>Authentication response with JWT tokens.</returns>
        public async Task<AuthResponseDto> RegisterPlayerAsync(RegisterPlayerRequestDto request)
        {
            _logger.LogInformation("Starting player registration for email: {email}", request.Email);

            await ValidateRegistrationAsync(request);

            var player = new Player
            {
                UserName = request.UserName,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                ProfileImageUrl = request.ProfileImageUrl,
                DateOfBirth = request.DateOfBirth,
                Nationality = request.Nationality,
                PreferredFoot = ParsePreferredFoot(request.PreferredFoot),
                WeakFootRating = request.WeakFootRating,
                PlayStyleTag = request.PlayStyleTag,
                ArchetypePlayerName = request.ArchetypePlayerName,
                ArchetypeText = request.ArchetypeText,
                AvailabilityStatus = AvailabilityStatus.Available,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedById = null,
                CreatedByUser = null!
            };

            await ExecuteRegistrationInTransactionAsync(async () =>
            {
                await CreateUserWithRoleAsync(player, request.Password, RegistrationRoles.Player);

                if (request.AcademyId > 0)
                {
                    var academy = await _unitOfWork.Repository<Academy>().GetByIdAsync(request.AcademyId);
                    if (academy is null)
                    {
                        _logger.LogWarning("Academy not found for player registration. AcademyId: {academyId}", request.AcademyId);
                        throw new NotFoundException("Academy not found.");
                    }

                    await _unitOfWork.Repository<PlayerAcademy>().AddAsync(new PlayerAcademy
                    {
                        PlayerId = player.Id,
                        AcademyId = academy.Id,
                        Status = PlayerAcademyStatus.Active,
                        JoinedAt = DateTime.UtcNow
                    });

                    await _unitOfWork.Repository<PlayerSubscription>().AddAsync(new PlayerSubscription
                    {
                        PlayerId = player.Id,
                        AcademyId = academy.Id,
                        PaidByUserId = player.Id,
                        Status = SubscriptionStatus.Unpaid,
                        PaidAt = null,
                        GraceUntil = null
                    });
                }

                await _unitOfWork.SaveChangesAsync();
                return true;
            });

            _logger.LogInformation("Player successfully registered. UserId: {userId}, Email: {email}", player.Id, player.Email);

            return await _authService.LoginAsync(new LoginRequestDto
            {
                EmailOrUserName = player.Email ?? player.UserName ?? string.Empty,
                Password = request.Password
            });
        }

        /// <summary>
        /// Registers a new coach account.
        /// </summary>
        /// <param name="request">The coach registration details.</param>
        /// <returns>Authentication response with JWT tokens.</returns>
        public async Task<AuthResponseDto> RegisterCoachAsync(RegisterCoachRequestDto request)
        {
            _logger.LogInformation("Starting coach registration for email: {email}", request.Email);

            await ValidateRegistrationAsync(request);

            var coach = new Coach
            {
                UserName = request.UserName,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                ProfileImageUrl = request.ProfileImageUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedById = null,
                CreatedByUser = null!
            };

            await ExecuteRegistrationInTransactionAsync(async () =>
            {
                await CreateUserWithRoleAsync(coach, request.Password, RegistrationRoles.Coach);

                if (request.AcademyId > 0)
                {
                    var academy = await _unitOfWork.Repository<Academy>().GetByIdAsync(request.AcademyId);
                    if (academy is null)
                    {
                        _logger.LogWarning("Academy not found for coach registration. AcademyId: {academyId}", request.AcademyId);
                        throw new NotFoundException("Academy not found.");
                    }

                    await _unitOfWork.Repository<CoachAcademy>().AddAsync(new CoachAcademy
                    {
                        CoachUserId = coach.Id,
                        AcademyId = academy.Id,
                        JoinedAt = DateTime.UtcNow
                    });
                }

                await _unitOfWork.SaveChangesAsync();
                return true;
            });

            _logger.LogInformation("Coach successfully registered. UserId: {userId}, Email: {email}", coach.Id, coach.Email);

            return await _authService.LoginAsync(new LoginRequestDto
            {
                EmailOrUserName = coach.Email ?? coach.UserName ?? string.Empty,
                Password = request.Password
            });
        }

        /// <summary>
        /// Registers a new scouter account.
        /// </summary>
        /// <param name="request">The scouter registration details.</param>
        /// <returns>Authentication response with JWT tokens.</returns>
        public async Task<AuthResponseDto> RegisterScouterAsync(RegisterScouterRequestDto request)
        {
            _logger.LogInformation("Starting scouter registration for email: {email}", request.Email);

            await ValidateRegistrationAsync(request);

            var scouter = new Scouter
            {
                UserName = request.UserName,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                ProfileImageUrl = request.ProfileImageUrl,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedById = null,
                CreatedByUser = null!
            };

            await ExecuteRegistrationInTransactionAsync(async () =>
            {
                await CreateUserWithRoleAsync(scouter, request.Password, RegistrationRoles.Scouter);
                await _unitOfWork.SaveChangesAsync();
                return true;
            });

            _logger.LogInformation("Scouter successfully registered. UserId: {userId}, Email: {email}", scouter.Id, scouter.Email);

            return await _authService.LoginAsync(new LoginRequestDto
            {
                EmailOrUserName = scouter.Email ?? scouter.UserName ?? string.Empty,
                Password = request.Password
            });
        }

        /// <summary>
        /// Registers a new parent account and links it to a player.
        /// </summary>
        /// <param name="request">The parent registration details with child player ID.</param>
        /// <returns>Authentication response with JWT tokens.</returns>
        public async Task<AuthResponseDto> RegisterParentAsync(RegisterParentRequestDto request)
        {
            _logger.LogInformation("Starting parent registration for email: {email}, child player id: {childPlayerId}", request.Email, request.ChildPlayerId);

            await ValidateRegistrationAsync(request);

            // Verify the child player exists BEFORE creating the parent's Identity account.
            // This avoids leaving behind an orphaned user with no role/link if the
            // child lookup fails.
            var child = await _unitOfWork.Repository<Player>().GetByIdAsync(request.ChildPlayerId);
            if (child is null)
            {
                _logger.LogWarning("Child player not found for parent registration. PlayerId: {playerId}", request.ChildPlayerId);
                throw new NotFoundException("Child player not found.");
            }

            var parent = new Parent
            {
                UserName = request.UserName,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                ProfileImageUrl = request.ProfileImageUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedById = null,
                CreatedByUser = null!
            };

            await ExecuteRegistrationInTransactionAsync(async () =>
            {
                await CreateUserWithRoleAsync(parent, request.Password, RegistrationRoles.Parent);

                await _unitOfWork.Repository<ParentPlayer>().AddAsync(new ParentPlayer
                {
                    ParentId = parent.Id,
                    PlayerId = child.Id
                });

                await _unitOfWork.SaveChangesAsync();
                return true;
            });

            _logger.LogInformation("Parent successfully registered. UserId: {userId}, Email: {email}, linked to player: {childPlayerId}",
                parent.Id, parent.Email, child.Id);

            return await _authService.LoginAsync(new LoginRequestDto
            {
                EmailOrUserName = parent.Email ?? parent.UserName ?? string.Empty,
                Password = request.Password
            });
        }

        /// <summary>
        /// Creates the Identity user record for the given entity and assigns it to the
        /// specified role, ensuring the role exists first.
        /// </summary>
        private async Task CreateUserWithRoleAsync<TUser>(TUser user, string password, string roleName) where TUser : User
        {
            await EnsureRoleExistsAsync(roleName);

            var identityResult = await _userManager.CreateAsync(user, password);
            if (!identityResult.Succeeded)
            {
                var errors = string.Join(", ", identityResult.Errors.Select(e => e.Description));
                _logger.LogWarning("Failed to create {role} account for email {email}. Errors: {errors}", roleName, user.Email, errors);
                throw new BadRequestException(identityResult.Errors.FirstOrDefault()?.Description ?? "Unable to create user account.");
            }

            var roleResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to assign role {role} to user {email}. Errors: {errors}", roleName, user.Email, errors);
                throw new BadRequestException("Account was created but role assignment failed. Please contact support.");
            }
        }

        /// <summary>
        /// Runs the given registration work inside a database transaction and commits on
        /// success. If any step throws (Identity creation, role assignment, related entity
        /// lookups/writes), the transaction is rolled back so no orphaned user or partial
        /// state is left behind. <paramref name="work"/> is responsible for calling
        /// SaveChangesAsync itself before returning, so it happens inside the transaction.
        /// </summary>
        private async Task<TResult> ExecuteRegistrationInTransactionAsync<TResult>(Func<Task<TResult>> work)
        {
            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var result = await work();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task ValidateRegistrationAsync(BaseRegistrationRequestDto request)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName) ||
                string.IsNullOrWhiteSpace(request.UserName) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                _logger.LogWarning("Registration validation failed: missing required fields");
                throw new BadRequestException("Please provide first name, last name, username, email and password.");
            }

            // Validate email format
            if (!IsValidEmail(request.Email))
            {
                _logger.LogWarning("Registration validation failed: invalid email format. Email: {email}", request.Email);
                throw new BadRequestException("Invalid email format.");
            }

            // Validate username format (alphanumeric and underscores only)
            if (!IsValidUsername(request.UserName))
            {
                _logger.LogWarning("Registration validation failed: invalid username format. Username: {username}", request.UserName);
                throw new BadRequestException("Username must contain only letters, numbers, and underscores.");
            }

            // Validate password strength
            if (!IsValidPassword(request.Password))
            {
                throw new BadRequestException(
                    $"Password must be at least {MinimumPasswordLength} characters and contain uppercase, lowercase, digit, and special character (@$!%*?&).");
            }

            // Validate password confirmation
            if (request.Password != request.ConfirmPassword)
            {
                _logger.LogWarning("Registration validation failed: password mismatch for email {email}", request.Email);
                throw new BadRequestException("Password and confirmation do not match.");
            }

            // Check email uniqueness
            if (await _userManager.FindByEmailAsync(request.Email) is not null)
            {
                _logger.LogWarning("Registration validation failed: email already registered. Email: {email}", request.Email);
                throw new ConflictException("Email already registered.");
            }

            // Check username uniqueness
            if (await _userManager.FindByNameAsync(request.UserName) is not null)
            {
                _logger.LogWarning("Registration validation failed: username already registered. Username: {username}", request.UserName);
                throw new ConflictException("Username already registered.");
            }
        }

        
        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsValidUsername(string username)
        {
            return !string.IsNullOrWhiteSpace(username) &&
                   Regex.IsMatch(username, @"^[a-zA-Z0-9_]{3,20}$");
        }

        private static bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < MinimumPasswordLength)
                return false;

            return Regex.IsMatch(password, PasswordPattern);
        }

        private async Task EnsureRoleExistsAsync(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new Role { Name = roleName });
            }
        }

        private static PreferredFoot ParsePreferredFoot(string preferredFoot)
        {
            return preferredFoot.ToLowerInvariant() switch
            {
                "left" => PreferredFoot.Left,
                "both" => PreferredFoot.Both,
                _ => PreferredFoot.Right
            };
        }
    }
}