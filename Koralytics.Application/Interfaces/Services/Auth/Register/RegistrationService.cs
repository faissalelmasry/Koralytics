using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using AutoMapper;

using FluentValidation;

using Koralytics.Application.Common;
using Koralytics.Application.DTOs.AuthDTOs.LoginDTOs;
using Koralytics.Application.DTOs.AuthDTOs.RegisterDTOs;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Services.Auth.Token;
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

using AcademyEntity = Koralytics.Domain.Entities.Academy.Academy;
using CoachEntity = Koralytics.Domain.Entities.Coach.Coach;

namespace Koralytics.Application.Services.Auth.Register
{
    public class RegistrationService : IRegistrationService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly IValidator<BaseRegistrationRequestDto> _requestValidator;
        private readonly IUserBusinessValidator _businessValidator;
        private readonly ILogger<RegistrationService> _logger;
        private readonly IMapper _mapper;

        public RegistrationService(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            IUnitOfWork unitOfWork,
            ITokenService tokenService,
            IValidator<BaseRegistrationRequestDto> requestValidator,
            IUserBusinessValidator businessValidator,
            ILogger<RegistrationService> logger,
            IMapper mapper)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _requestValidator = requestValidator;
            _businessValidator = businessValidator;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<AuthResultDto> RegisterPlayerAsync(RegisterPlayerRequestDto request)
        {
            _logger.LogInformation("Starting player registration for email: {email}", request.Email);
            await ValidateRegistrationRequestAsync(request);
            await _businessValidator.EnsureWeakFootRating(request.WeakFootRating);

            var player = _mapper.Map<Domain.Entities.Player.Player>(request);
            player.PreferredFoot = ParsePreferredFoot(request.PreferredFoot);
            player.AvailabilityStatus = AvailabilityStatus.Available;
            player.CreatedAt = DateTime.UtcNow;
            player.UpdatedAt = DateTime.UtcNow;

            await ExecuteRegistrationInTransactionAsync(async () =>
            {
                await CreateUserWithRoleAsync(player, request.Password, AuthConstants.Roles.Player);
                await CreatePlayerSpecificDataAsync(player.Id, request.AcademyId);
                await _unitOfWork.SaveChangesAsync();
                return true;
            });

            _logger.LogInformation("Player successfully registered. UserId: {userId}", player.Id);
            return await GenerateAuthResultAsync(player, AuthConstants.Roles.Player, request.AcademyId > 0 ? request.AcademyId : null);
        }

        public async Task<AuthResultDto> RegisterCoachAsync(RegisterCoachRequestDto request)
        {
            _logger.LogInformation("Starting coach registration for email: {email}", request.Email);
            await ValidateRegistrationRequestAsync(request);

            var coach = _mapper.Map<CoachEntity>(request);

            await ExecuteRegistrationInTransactionAsync(async () =>
            {
                await CreateUserWithRoleAsync(coach, request.Password, AuthConstants.Roles.Coach);
                await CreateCoachSpecificDataAsync(coach.Id, request.AcademyId);
                await _unitOfWork.SaveChangesAsync();
                return true;
            });

            _logger.LogInformation("Coach successfully registered. UserId: {userId}", coach.Id);
            return await GenerateAuthResultAsync(coach, AuthConstants.Roles.Coach, request.AcademyId > 0 ? request.AcademyId : null);
        }

        public async Task<AuthResultDto> RegisterScouterAsync(RegisterScouterRequestDto request)
        {
            _logger.LogInformation("Starting scouter registration for email: {email}", request.Email);
            await ValidateRegistrationRequestAsync(request);

            var scouter = _mapper.Map<Scouter>(request);
            scouter.IsVerified = false;
            scouter.CreatedAt = DateTime.UtcNow;
            scouter.UpdatedAt = DateTime.UtcNow;

            await ExecuteRegistrationInTransactionAsync(async () =>
            {
                await CreateUserWithRoleAsync(scouter, request.Password, AuthConstants.Roles.Scouter);
                await _unitOfWork.SaveChangesAsync();
                return true;
            });

            _logger.LogInformation("Scouter successfully registered. UserId: {userId}", scouter.Id);
            return await GenerateAuthResultAsync(scouter, AuthConstants.Roles.Scouter, null);
        }

        public async Task<AuthResultDto> RegisterParentAsync(RegisterParentRequestDto request)
        {
            _logger.LogInformation("Starting parent registration for email: {email}", request.Email);
            await ValidateRegistrationRequestAsync(request);

            var child = await _unitOfWork.Repository<Domain.Entities.Player.Player>().GetByIdAsync(request.ChildPlayerId);
            if (child is null) throw new NotFoundException("Child player not found.");

            var parent = _mapper.Map<Parent>(request);

            await ExecuteRegistrationInTransactionAsync(async () =>
            {
                await CreateUserWithRoleAsync(parent, request.Password, AuthConstants.Roles.Parent);
                await CreateParentSpecificDataAsync(parent.Id, request.ChildPlayerId);
                await _unitOfWork.SaveChangesAsync();
                return true;
            });

            _logger.LogInformation("Parent successfully registered. UserId: {userId}", parent.Id);
            return await GenerateAuthResultAsync(parent, AuthConstants.Roles.Parent, null);
        }

        public async Task<AuthResultDto> RegisterAcademyAdminAsync(RegisterAcademyAdminRequestDto request)
        {
            _logger.LogInformation("Starting academy admin registration for email: {email}", request.Email);
            await ValidateRegistrationRequestAsync(request);
            await _businessValidator.EnsureAcademyExistsAsync(request.AcademyId);

            var academyAdmin = _mapper.Map<AcademyAdmin>(request);

            await ExecuteRegistrationInTransactionAsync(async () =>
            {
                await CreateUserWithRoleAsync(academyAdmin, request.Password, AuthConstants.Roles.AcademyAdmin);
                await _unitOfWork.SaveChangesAsync();
                return true;
            });

            _logger.LogInformation("Academy admin successfully registered. UserId: {userId}", academyAdmin.Id);
            return await GenerateAuthResultAsync(academyAdmin, AuthConstants.Roles.AcademyAdmin, request.AcademyId);
        }

        public async Task CompleteProfileAsPlayerAsync(User existingUser, CompleteProfileAsPlayerDto profileData)
        {
            await _businessValidator.EnsureWeakFootRating(profileData.WeakFootRating ?? 3);

            await ExecuteRegistrationInTransactionAsync(async () =>
            {
                await ReplacePendingProfileRoleAsync(existingUser, AuthConstants.Roles.Player);

                var dob = profileData.DateOfBirth ?? throw new BadRequestException("DateOfBirth is required for players.");
                var prefFoot = profileData.PreferredFoot != null ? (int)ParsePreferredFoot(profileData.PreferredFoot) : 1;
                var weakFoot = profileData.WeakFootRating ?? 3;

                await _unitOfWork.ExecuteSqlRawAsync(
                    "INSERT INTO Players (Id, DateOfBirth, PreferredFoot, WeakFootRating, AvailabilityStatus) VALUES ({0}, {1}, {2}, {3}, {4})",
                    existingUser.Id, dob, prefFoot, weakFoot, (int)AvailabilityStatus.Available);

                if (profileData.AcademyId > 0)
                {
                    await CreatePlayerSpecificDataAsync(existingUser.Id, profileData.AcademyId.Value);
                }

                await _unitOfWork.SaveChangesAsync();
                return true;
            });
        }

        public async Task CompleteProfileAsCoachAsync(User existingUser, CompleteProfileAsCoachDto profileData)
        {
            await ExecuteRegistrationInTransactionAsync(async () =>
            {
                await ReplacePendingProfileRoleAsync(existingUser, AuthConstants.Roles.Coach);

                await _unitOfWork.ExecuteSqlRawAsync("INSERT INTO Coaches (Id) VALUES ({0})", existingUser.Id);

                if (profileData.AcademyId > 0)
                {
                    await CreateCoachSpecificDataAsync(existingUser.Id, profileData.AcademyId.Value);
                }

                await _unitOfWork.SaveChangesAsync();
                return true;
            });
        }

        public async Task CompleteProfileAsParentAsync(User existingUser, CompleteProfileAsParentDto profileData)
        {
            var child = await _unitOfWork.Repository<Domain.Entities.Player.Player>().GetByIdAsync(profileData.ChildPlayerId);
            if (child is null) throw new NotFoundException("Child player not found.");

            await ExecuteRegistrationInTransactionAsync(async () =>
            {
                await ReplacePendingProfileRoleAsync(existingUser, AuthConstants.Roles.Parent);

                await _unitOfWork.ExecuteSqlRawAsync("INSERT INTO Parents (Id) VALUES ({0})", existingUser.Id);
                await CreateParentSpecificDataAsync(existingUser.Id, profileData.ChildPlayerId);

                await _unitOfWork.SaveChangesAsync();
                return true;
            });
        }

        public async Task CompleteProfileAsScouterAsync(User existingUser, CompleteProfileAsScouterDto profileData)
        {
            await ExecuteRegistrationInTransactionAsync(async () =>
            {
                await ReplacePendingProfileRoleAsync(existingUser, AuthConstants.Roles.Scouter);

                await _unitOfWork.ExecuteSqlRawAsync("INSERT INTO Scouters (Id, IsVerified) VALUES ({0}, {1})", existingUser.Id, false);

                await _unitOfWork.SaveChangesAsync();
                return true;
            });
        }

        public async Task CompleteProfileAsAcademyAdminAsync(User existingUser, CompleteProfileAsAcademyAdminDto profileData)
        {
            await _businessValidator.EnsureAcademyExistsAsync(profileData.AcademyId);

            await ExecuteRegistrationInTransactionAsync(async () =>
            {
                await ReplacePendingProfileRoleAsync(existingUser, AuthConstants.Roles.AcademyAdmin);

                await _unitOfWork.ExecuteSqlRawAsync("INSERT INTO AcademyAdmins (Id, AcademyId) VALUES ({0}, {1})", existingUser.Id, profileData.AcademyId);

                await _unitOfWork.SaveChangesAsync();
                return true;
            });
        }

        private async Task ReplacePendingProfileRoleAsync(User user, string newRole)
        {
            await _userManager.RemoveFromRoleAsync(user, "PendingProfile");
            await _businessValidator.EnsureRoleExistsAsync(newRole);
            var roleResult = await _userManager.AddToRoleAsync(user, newRole);
            if (!roleResult.Succeeded) 
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                throw new BadRequestException($"Failed to assign role: {errors}");
            }
        }

        private async Task CreatePlayerSpecificDataAsync(int playerId, int academyId)
        {
            if (academyId > 0)
            {
                var academy = await _unitOfWork.Repository<AcademyEntity>().GetByIdAsync(academyId);
                if (academy == null) throw new NotFoundException("Academy not found.");

                await _unitOfWork.Repository<PlayerAcademy>().AddAsync(new PlayerAcademy
                {
                    PlayerId = playerId,
                    AcademyId = academy.Id,
                    Status = PlayerAcademyStatus.Active,
                    JoinedAt = DateTime.UtcNow
                });

                await _unitOfWork.Repository<PlayerSubscription>().AddAsync(new PlayerSubscription
                {
                    PlayerId = playerId,
                    AcademyId = academy.Id,
                    PaidByUserId = playerId,
                    Status = SubscriptionStatus.Unpaid
                });
            }
        }

        private async Task CreateCoachSpecificDataAsync(int coachId, int academyId)
        {
            if (academyId > 0)
            {
                var academy = await _unitOfWork.Repository<AcademyEntity>().GetByIdAsync(academyId);
                if (academy == null) throw new NotFoundException("Academy not found.");

                await _unitOfWork.Repository<CoachAcademy>().AddAsync(new CoachAcademy
                {
                    CoachUserId = coachId,
                    AcademyId = academy.Id,
                    JoinedAt = DateTime.UtcNow
                });
            }
        }

        private async Task CreateParentSpecificDataAsync(int parentId, int childPlayerId)
        {
            await _unitOfWork.Repository<ParentPlayer>().AddAsync(new ParentPlayer
            {
                ParentId = parentId,
                PlayerId = childPlayerId
            });
        }

        private async Task ValidateRegistrationRequestAsync(BaseRegistrationRequestDto request)
        {
            var validationResult = await _requestValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join(" ", validationResult.Errors.Select(e => e.ErrorMessage));
                throw new BadRequestException(errorMessage);
            }

            await _businessValidator.EnsureEmailNotExistsAsync(request.Email);
            await _businessValidator.EnsureUsernameNotExistsAsync(request.UserName);
        }

        private async Task CreateUserWithRoleAsync<TUser>(TUser user, string password, string roleName) where TUser : User
        {
            await _businessValidator.EnsureRoleExistsAsync(roleName);

            var identityResult = await _userManager.CreateAsync(user, password);
            if (!identityResult.Succeeded)
            {
                throw new BadRequestException(identityResult.Errors.FirstOrDefault()?.Description ?? "Unable to create account.");
            }

            var roleResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!roleResult.Succeeded)
            {
                throw new BadRequestException("Account was created but role assignment failed.");
            }
        }

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
            return preferredFoot?.ToLowerInvariant() switch
            {
                "left" => PreferredFoot.Left,
                "both" => PreferredFoot.Both,
                _ => PreferredFoot.Right
            };
        }

        private async Task<AuthResultDto> GenerateAuthResultAsync(User user, string role, int? academyId)
        {
            var roles = new List<string> { role };
            var tokens = await _tokenService.GenerateTokenPairAsync(user, roles, academyId);
            
            var response = new AuthResponseDto
            {
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                AccessTokenExpiresAt = tokens.AccessTokenExpiresAt,
                RefreshTokenExpiresAt = tokens.RefreshTokenExpiresAt,
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = string.Join(' ', new[] { user.FirstName, user.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))),
                Roles = roles
            };
            
            return new AuthResultDto(response, tokens);
        }
    }
}