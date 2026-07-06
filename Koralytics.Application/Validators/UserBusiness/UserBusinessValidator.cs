using Koralytics.Application.Interfaces;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Identity;
using Koralytics.Domain.Exceptions;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.Validators.UserBusiness
{
    public class UserBusinessValidator : IUserBusinessValidator
    {
        private readonly UserManager<User> _userManager;
        private readonly ILogger<UserBusinessValidator> _logger;
        private readonly RoleManager<Role> _roleManager;
        private readonly IUnitOfWork _unitOfWork;

        public UserBusinessValidator(
            UserManager<User> userManager,
            ILogger<UserBusinessValidator> logger,
            RoleManager<Role> roleManager,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _logger = logger;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
        }

        public async Task EnsureEmailNotExistsAsync(string email)
        {
            if (await _userManager.FindByEmailAsync(email) != null)
            {
                _logger.LogWarning("Email already registered: {Email}", email);
                throw new ConflictException("Email already registered.");
            }
        }

        public async Task EnsureUsernameNotExistsAsync(string username)
        {
            if (await _userManager.FindByNameAsync(username) != null)
            {
                _logger.LogWarning("Username already registered: {Username}", username);
                throw new ConflictException("Username already registered.");
            }
        }

        public async Task EnsureAcademyExistsAsync(int academyId)
        {
            if (await _unitOfWork.Repository<Academy>().GetByIdAsync(academyId) == null)
            {
                _logger.LogWarning("Academy not found: {AcademyId}", academyId);
                throw new NotFoundException("Academy not found.");
            }
        }

        public async Task EnsureWeakFootRating(int weakFootRating)
        {
            if (weakFootRating < 1 || weakFootRating > 5)
            {
                _logger.LogWarning("Invalid weak foot rating: {WeakFootRating}", weakFootRating);
                throw new BadRequestException("Weak foot rating must be between 1 and 5.");
            }
        }

        public async Task<User> GetUserOrThrowAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                throw new NotFoundException("User not found.");
            }

            return user;
        }

        public async Task<User> GetUserByEmailOrUsernameOrThrowAsync(string emailOrUsername)
        {
            var user = await _userManager.FindByEmailAsync(emailOrUsername)
                ?? await _userManager.FindByNameAsync(emailOrUsername);

            if (user == null)
            {
                _logger.LogWarning("User not found: {Value}", emailOrUsername);
                throw new UnauthorizedException("Invalid credentials.");
            }

            return user;
        }

        public async Task EnsureRoleExistsAsync(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                _logger.LogWarning("Role not found: {RoleName}", roleName);
                throw new NotFoundException("Role not found.");
            }
        }
    }
}
