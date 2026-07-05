using Koralytics.Domain.Entities.Identity;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.Validators.UserBusiness
{
    public interface IUserBusinessValidator
    {
        Task EnsureEmailNotExistsAsync(string email);

        Task EnsureUsernameNotExistsAsync(string username);

        Task EnsureRoleExistsAsync(string roleName);

        Task EnsureAcademyExistsAsync(int academyId);

        Task<User> GetUserOrThrowAsync(int userId);

        Task<User> GetUserByEmailOrUsernameOrThrowAsync(string emailOrUsername);

        
    }
}
