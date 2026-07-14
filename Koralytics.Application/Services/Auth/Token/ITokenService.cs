using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

using Koralytics.Application.DTOs.AuthDTOs.LoginDTOs;
using Koralytics.Domain.Entities.Identity;

namespace Koralytics.Application.Services.Auth.Token
{
    public interface ITokenService
    {
        Task<TokenPair> GenerateTokenPairAsync(User user, IList<string> roles, int? academyId, string? deviceInfo = null, string? ipAddress = null);
        Task<TokenPair> RefreshTokensAsync(string refreshToken, System.Func<User, IList<string>, Task<int?>> getAcademyIdFunc, string? deviceInfo = null, string? ipAddress = null);
        Task RevokeRefreshTokenAsync(string refreshToken, string reason = "ManualRevoke");
        Task RevokeAllUserTokensAsync(int userId, string reason = "ManualRevoke");
        ClaimsPrincipal ValidateAccessToken(string token);
    }
}
