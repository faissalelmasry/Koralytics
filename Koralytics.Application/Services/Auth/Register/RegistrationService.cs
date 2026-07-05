using AutoMapper;

using FluentValidation;

using Koralytics.Application.DTOs.AuthDTOs.LoginDTOs;
using Koralytics.Application.DTOs.AuthDTOs.RegisterDTOs;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Services.Auth.Login;
using Koralytics.Application.Validators.UserBusiness;
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
    /// <summary>
    /// Well-known role names used during registration. Centralized to avoid
    /// magic strings scattered across the service.
    /// </summary>
    internal static class RegistrationRoles
    {
        public const string Player = "Player";
        public const string Coach = "Coach";
        public const string Scouter = "Scouter";
        public const string Parent = "Parent";
        public const string AcademyAdmin = "AcademyAdmin";
    }

    /// <summary>
    /// Provides user registration services for different user roles (Player, Coach, Scouter, Parent, AcademyAdmin).
    /// Request-shape validation is delegated to <see cref="IValidator{BaseRegistrationRequestDto}"/>
    /// (FluentValidation) and DB-backed business checks are delegated to
    /// <see cref="IUserBusinessValidator"/>, so this class only orchestrates entity creation,
    /// role assignment, and related-entity linking.
    /// </summary>
    public class RegistrationService : IRegistrationService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;
        private readonly IValidator<BaseRegistrationRequestDto> _requestValidator;
        private readonly IUserBusinessValidator _businessValidator;
        private readonly ILogger<RegistrationService> _logger;
        private readonly IMapper _mapper;

        public RegistrationService(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            IUnitOfWork unitOfWork,
            IAuthService authService,
            IValidator<BaseRegistrationRequestDto> requestValidator,
            IUserBusinessValidator businessValidator,
            ILogger<RegistrationService> logger,
            IMapper mapper)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
            _authService = authService;
            _requestValidator = requestValidator;
            _businessValidator = businessValidator;
            _logger = logger;
            _mapper = mapper;
        }

        /// <summary>
        /// Registers a new player account.
        /// </summary>
        /// <param name="request">The player registration details.</param>
        /// <returns>Authentication response with JWT tokens.</returns>
        public async Task<AuthResponseDto> RegisterPlayerAsync(RegisterPlayerRequestDto request)
        {
            _logger.LogInformation("Starting player registration for email: {email}", request.Email);

            await ValidateRegistrationRequestAsync(request);
            await _businessValidator.EnsureWeakFootRating(request.WeakFootRating);


            var player = _mapper.Map<Player>(request);
            player.PreferredFoot = ParsePreferredFoot(request.PreferredFoot);
            player.AvailabilityStatus = AvailabilityStatus.Available;
            player.CreatedAt = DateTime.UtcNow;
            player.UpdatedAt = DateTime.UtcNow;


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

            await ValidateRegistrationRequestAsync(request);

            var coach = _mapper.Map<Coach>(request);

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

            await ValidateRegistrationRequestAsync(request);

            var scouter = _mapper.Map<Scouter>(request);

            scouter.IsVerified = false;
            scouter.CreatedAt = DateTime.UtcNow;
            scouter.UpdatedAt = DateTime.UtcNow;

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

            await ValidateRegistrationRequestAsync(request);

            // Verify the child player exists BEFORE creating the parent's Identity account.
            // This avoids leaving behind an orphaned user with no role/link if the
            // child lookup fails.
            var child = await _unitOfWork.Repository<Player>().GetByIdAsync(request.ChildPlayerId);
            if (child is null)
            {
                _logger.LogWarning("Child player not found for parent registration. PlayerId: {playerId}", request.ChildPlayerId);
                throw new NotFoundException("Child player not found.");
            }

            var parent = _mapper.Map<Parent>(request);

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

        // create Academy admin registration method
        public async Task<AuthResponseDto> RegisterAcademyAdminAsync(RegisterAcademyAdminRequestDto request)
        {
            _logger.LogInformation("Starting academy admin registration for email: {email}", request.Email);

            await ValidateRegistrationRequestAsync(request);

            // ensure the academy exists before creating the admin account 
            await _businessValidator.EnsureAcademyExistsAsync(request.AcademyId);

            var academyAdmin = _mapper.Map<AcademyAdmin>(request);// error here, should be AcademyAdmin entity instead of Coach

            await ExecuteRegistrationInTransactionAsync(async () =>
            {

                await CreateUserWithRoleAsync(academyAdmin, request.Password, RegistrationRoles.AcademyAdmin);



                await _unitOfWork.SaveChangesAsync();
                return true;
            });

            _logger.LogInformation("Academy admin successfully registered. UserId: {userId}, Email: {email}", academyAdmin.Id, academyAdmin.Email);

            return await _authService.LoginAsync(new LoginRequestDto
            {
                EmailOrUserName = academyAdmin.Email ?? academyAdmin.UserName ?? string.Empty,
                Password = request.Password
            });
        }

        /// <summary>
        /// Runs FluentValidation's field/format rules, then the DB-backed uniqueness checks.
        /// Kept as one call site per Register method so callers don't need to know there are
        /// two separate validators behind it.
        /// </summary>
        private async Task ValidateRegistrationRequestAsync(BaseRegistrationRequestDto request)
        {
            var validationResult = await _requestValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join(" ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Registration validation failed for email {email}: {errors}", request.Email, errorMessage);
                throw new BadRequestException(errorMessage);
            }

            await _businessValidator.EnsureEmailNotExistsAsync(request.Email);
            await _businessValidator.EnsureUsernameNotExistsAsync(request.UserName);
        }

        /// <summary>
        /// Creates the Identity user record for the given entity and assigns it to the
        /// specified role, ensuring the role exists first. Centralizes the
        /// create-account-and-assign-role flow that was previously duplicated across
        /// each Register*Async method.
        /// </summary>
        private async Task CreateUserWithRoleAsync<TUser>(TUser user, string password, string roleName) where TUser : User
        {
            await _businessValidator.EnsureRoleExistsAsync(roleName);

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