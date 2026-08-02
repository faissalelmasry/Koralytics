using AutoMapper;
using Koralytics.Application.DTOs.ProfileManagement;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Services.Storage;
using Koralytics.Domain.Entities.Academy;
using CoachEntity = Koralytics.Domain.Entities.Coach.Coach;
using Koralytics.Domain.Entities.Identity;
using ParentEntity = Koralytics.Domain.Entities.Parents.Parent;
using PlayerEntity = Koralytics.Domain.Entities.Player.Player;
using PlayerPosition = Koralytics.Domain.Entities.Player.PlayerPosition;
using ScouterEntity = Koralytics.Domain.Entities.Scouter.Scouter;
using Koralytics.Domain.Entities.SystemAdmin;
using Koralytics.Domain.Exceptions;
using Koralytics.Application.Services.Player.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Koralytics.Application.Services.ProfileManagement
{
    public class ProfileManagementService : IProfileManagementService
    {
        private readonly UserManager<User> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IStorageService _storageService;
        private readonly ICardInvalidationList _cardInvalidationList;
        private readonly ILogger<ProfileManagementService> _logger;

        public ProfileManagementService(
            UserManager<User> userManager,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IStorageService storageService,
            ICardInvalidationList cardInvalidationList,
            ILogger<ProfileManagementService> logger)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _storageService = storageService;
            _cardInvalidationList = cardInvalidationList;
            _logger = logger;
        }

        public async Task<BaseUserProfileResponseDto> GetMyProfileAsync(int userId)
        {
            _logger.LogInformation("Retrieving profile for user ID: {UserId}", userId);

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new NotFoundException($"User with ID {userId} was not found.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? "User";

            BaseUserProfileResponseDto dto;

            if (user is PlayerEntity || primaryRole == "Player")
            {
                var player = await _unitOfWork.Repository<PlayerEntity>()
                    .GetQueryableAsNoTracking()
                    .Include(p => p.PlayerPositions)
                    .FirstOrDefaultAsync(p => p.Id == userId);
                dto = _mapper.Map<PlayerProfileResponseDto>(player ?? (object)user);
            }
            else if (user is AcademyAdmin || primaryRole == "AcademyAdmin")
            {
                var admin = await _unitOfWork.Repository<AcademyAdmin>()
                    .GetQueryableAsNoTracking()
                    .Include(a => a.Academy)
                    .FirstOrDefaultAsync(a => a.Id == userId);
                dto = _mapper.Map<AcademyAdminProfileResponseDto>(admin ?? (object)user);
            }
            else if (user is ScouterEntity || primaryRole == "Scouter")
            {
                var scouter = await _unitOfWork.Repository<ScouterEntity>()
                    .GetQueryableAsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == userId);
                dto = _mapper.Map<ScouterProfileResponseDto>(scouter ?? (object)user);
            }
            else if (user is CoachEntity || primaryRole == "Coach")
            {
                var coach = await _unitOfWork.Repository<CoachEntity>()
                    .GetQueryableAsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == userId);
                dto = _mapper.Map<CoachProfileResponseDto>(coach ?? (object)user);
            }
            else if (user is ParentEntity || primaryRole == "Parent")
            {
                var parent = await _unitOfWork.Repository<ParentEntity>()
                    .GetQueryableAsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == userId);
                dto = _mapper.Map<ParentProfileResponseDto>(parent ?? (object)user);
            }
            else if (user is SystemAdminUser || primaryRole == "SystemAdmin")
            {
                var sysAdmin = await _unitOfWork.Repository<SystemAdminUser>()
                    .GetQueryableAsNoTracking()
                    .FirstOrDefaultAsync(sa => sa.Id == userId);
                dto = _mapper.Map<SystemAdminProfileResponseDto>(sysAdmin ?? (object)user);
            }
            else
            {
                dto = _mapper.Map<BaseUserProfileResponseDto>(user);
            }

            dto.Role = primaryRole;
            return dto;
        }

        public async Task<BaseUserProfileResponseDto> UpdateProfileAsync(int userId, UpdateProfileRequestDto dto)
        {
            _logger.LogInformation("Updating profile for user ID: {UserId}", userId);

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new NotFoundException($"User with ID {userId} was not found.");
            }

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.PhoneNumber = dto.PhoneNumber;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                _logger.LogWarning("Failed to update profile for user ID {UserId}: {Errors}", userId, errors);
                throw new BadRequestException($"Failed to update profile: {errors}");
            }

            if (user is PlayerEntity)
            {
                var player = await _unitOfWork.Repository<PlayerEntity>()
                    .GetQueryable()
                    .Include(p => p.PlayerPositions)
                    .FirstOrDefaultAsync(p => p.Id == userId);

                if (player != null)
                {
                    player.Nationality = dto.Nationality;

                    if (dto.PreferredFoot.HasValue)
                        player.PreferredFoot = dto.PreferredFoot.Value;

                    if (dto.WeakFootRating.HasValue)
                        player.WeakFootRating = dto.WeakFootRating.Value;

                    player.HeightCm = dto.HeightCm;
                    player.WeightKg = dto.WeightKg;
                    player.PlayStyleTag = dto.PlayStyleTag;

                    if (dto.Positions != null)
                    {
                        var oldPrimary = player.PlayerPositions.FirstOrDefault(p => p.IsPrimary)?.Position;
                        var newPrimary = dto.Positions.FirstOrDefault(p => p.IsPrimary)?.Position;

                        player.PlayerPositions.Clear();
                        foreach (var posDto in dto.Positions)
                        {
                            player.PlayerPositions.Add(new PlayerPosition
                            {
                                PlayerId = userId,
                                Position = posDto.Position.Trim().ToUpperInvariant(),
                                IsPrimary = posDto.IsPrimary
                            });
                        }

                        if (!string.Equals(oldPrimary, newPrimary, StringComparison.OrdinalIgnoreCase))
                        {
                            _cardInvalidationList.Invalidate(userId);
                        }
                    }

                    await _unitOfWork.SaveChangesAsync();
                }
            }

            return await GetMyProfileAsync(userId);
        }

        public async Task<string> UpdateProfileImageAsync(int userId, IFormFile image)
        {
            _logger.LogInformation("Updating profile image for user ID: {UserId}", userId);

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new NotFoundException($"User with ID {userId} was not found.");
            }

            if (!string.IsNullOrEmpty(user.ProfileImageUrl))
            {
                try
                {
                    await _storageService.DeleteFileAsync(user.ProfileImageUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old profile image {Url} for user ID {UserId}", user.ProfileImageUrl, userId);
                }
            }

            var newImageUrl = await _storageService.UploadImageAsync(image, "profile-images");
            user.ProfileImageUrl = newImageUrl;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                _logger.LogWarning("Failed to update user profile image URL for user ID {UserId}: {Errors}", userId, errors);
                throw new BadRequestException($"Failed to update profile image: {errors}");
            }

            return newImageUrl;
        }
    }
}
