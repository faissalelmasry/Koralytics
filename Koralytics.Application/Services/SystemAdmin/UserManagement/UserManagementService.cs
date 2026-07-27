using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Koralytics.Application.DTOs.SystemAdmin;
using Koralytics.Domain.Entities.Identity;
using Koralytics.Domain.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Koralytics.Application.Services.SystemAdmin.UserManagement
{
    public class UserManagementService : IUserManagementService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IMapper _mapper;

        public UserManagementService(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            IMapper mapper)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _mapper = mapper;
        }

        public async Task<UserListResponseDto> GetUsersAsync(UserListRequestDto request)
        {
            var query = _userManager.Users.IgnoreQueryFilters().AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                query = query.Where(u =>
                    u.FirstName.ToLower().Contains(term) ||
                    u.LastName.ToLower().Contains(term) ||
                    (u.Email != null && u.Email.ToLower().Contains(term)) ||
                    (u.UserName != null && u.UserName.ToLower().Contains(term)));
            }

            if (request.IsDeletedFilter.HasValue)
            {
                query = query.Where(u => u.IsDeleted == request.IsDeletedFilter.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.RoleFilter) && !request.RoleFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(request.RoleFilter);
                var userIdsInRole = usersInRole.Select(u => u.Id).ToHashSet();
                query = query.Where(u => userIdsInRole.Contains(u.Id));
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var items = new List<UserSummaryDto>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                var dto = _mapper.Map<UserSummaryDto>(u);
                dto.Roles = roles.ToList();
                items.Add(dto);
            }

            return new UserListResponseDto
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        public async Task<UserDetailDto> GetUserByIdAsync(int userId)
        {
            var user = await _userManager.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new NotFoundException($"User with Id {userId} not found.");
            }

            var roles = await _userManager.GetRolesAsync(user);

            var dto = _mapper.Map<UserDetailDto>(user);
            dto.Roles = roles.ToList();
            
            return dto;
        }

        public async Task<UserSummaryDto> UpdateUserRolesAsync(int userId, UpdateUserRolesDto dto, int currentUserId)
        {
            var user = await _userManager.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new NotFoundException($"User with Id {userId} not found.");
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            var toRemove = currentRoles.Except(dto.Roles, StringComparer.OrdinalIgnoreCase).ToList();
            var toAdd = dto.Roles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToList();

            if (userId == currentUserId && toRemove.Contains("SystemAdmin", StringComparer.OrdinalIgnoreCase))
            {
                throw new BadRequestException("Cannot remove SystemAdmin role from your own account.");
            }

            foreach (var roleName in toAdd)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    throw new BadRequestException($"Role '{roleName}' does not exist.");
                }
            }

            if (toRemove.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, toRemove);
                if (!removeResult.Succeeded)
                {
                    throw new BadRequestException("Failed to remove roles from user.");
                }
            }

            if (toAdd.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(user, toAdd);
                if (!addResult.Succeeded)
                {
                    throw new BadRequestException("Failed to add roles to user.");
                }
            }

            var updatedRoles = await _userManager.GetRolesAsync(user);

            var dtoToReturn = _mapper.Map<UserSummaryDto>(user);
            dtoToReturn.Roles = updatedRoles.ToList();

            return dtoToReturn;
        }

        public async Task ToggleUserStatusAsync(int userId, bool isDeleted, int currentUserId)
        {
            if (userId == currentUserId && isDeleted)
            {
                throw new BadRequestException("Cannot deactivate your own account.");
            }

            var user = await _userManager.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new NotFoundException($"User with Id {userId} not found.");
            }

            user.IsDeleted = isDeleted;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                throw new BadRequestException("Failed to update user status.");
            }
        }
    }
}
