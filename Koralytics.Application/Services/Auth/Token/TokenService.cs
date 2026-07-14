using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Koralytics.Application.DTOs.AuthDTOs.LoginDTOs;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Options;
using Koralytics.Domain.Entities.Identity;
using Koralytics.Domain.Exceptions;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Koralytics.Application.Services.Auth.Token
{
    public class TokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<User> _userManager;

        public TokenService(IOptions<JwtSettings> jwtSettings, IUnitOfWork unitOfWork, UserManager<User> userManager)
        {
            _jwtSettings = jwtSettings.Value;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<TokenPair> GenerateTokenPairAsync(User user, IList<string> roles, int? academyId, string? deviceInfo = null, string? ipAddress = null)
        {
            // 1. Generate Access Token
            var (accessToken, accessTokenExpiresAt) = GenerateAccessToken(user, roles, academyId);

            // 2. Generate Refresh Token
            var refreshTokenRaw = GenerateSecureToken();
            var refreshTokenHash = HashToken(refreshTokenRaw);
            var familyId = Guid.NewGuid().ToString();
            var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays);

            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenHash,
                JtiId = familyId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = refreshTokenExpiresAt,
                DeviceInfo = deviceInfo,
                IpAddress = ipAddress
            };

            await _unitOfWork.Repository<RefreshToken>().AddAsync(refreshTokenEntity);
            await _unitOfWork.SaveChangesAsync();

            return new TokenPair(accessToken, accessTokenExpiresAt, refreshTokenRaw, refreshTokenExpiresAt);
        }

        public async Task<TokenPair> RefreshTokensAsync(string refreshTokenRaw, System.Func<User, IList<string>, Task<int?>> getAcademyIdFunc, string? deviceInfo = null, string? ipAddress = null)
        {
            var refreshTokenHash = HashToken(refreshTokenRaw);

            var existingToken = await _unitOfWork.Repository<RefreshToken>()
                .GetQueryable()
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshTokenHash);

            if (existingToken == null || existingToken.IsDeleted)
            {
                throw new UnauthorizedException("Invalid refresh token.");
            }

            // Reuse detection: if the token was already revoked, revoke the entire family (token theft scenario)
            if (existingToken.RevokedAt != null)
            {
                await RevokeTokenFamilyAsync(existingToken.JtiId, "SecurityBreach - Token Reuse");
                throw new UnauthorizedException("Token reuse detected. All sessions have been revoked.");
            }

            if (existingToken.ExpiresAt <= DateTime.UtcNow)
            {
                throw new UnauthorizedException("Refresh token has expired.");
            }

            var user = existingToken.User;
            var roles = await _userManager.GetRolesAsync(user);

            var academyId = await getAcademyIdFunc(user, roles);

            // Generate new token pair
            var (newAccessToken, accessTokenExpiresAt) = GenerateAccessToken(user, roles, academyId);

            var newRefreshTokenRaw = GenerateSecureToken();
            var newRefreshTokenHash = HashToken(newRefreshTokenRaw);
            var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays);

            // Revoke the old token and link it to the new one
            existingToken.RevokedAt = DateTime.UtcNow;
            existingToken.RevokedReason = "Rotation";
            existingToken.ReplacedByToken = newRefreshTokenHash;

            // Persist new refresh token (inherits the same family JtiId for reuse-detection chain)
            var newRefreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshTokenHash,
                JtiId = existingToken.JtiId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = refreshTokenExpiresAt,
                DeviceInfo = deviceInfo ?? existingToken.DeviceInfo,
                IpAddress = ipAddress ?? existingToken.IpAddress
            };

            await _unitOfWork.Repository<RefreshToken>().AddAsync(newRefreshTokenEntity);
            await _unitOfWork.SaveChangesAsync();

            return new TokenPair(newAccessToken, accessTokenExpiresAt, newRefreshTokenRaw, refreshTokenExpiresAt);
        }

        private (string Token, DateTime ExpiresAt) GenerateAccessToken(User user, IList<string> roles, int? academyId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            if (academyId.HasValue)
            {
                claims.Add(new Claim("AcademyId", academyId.Value.ToString()));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenMinutes);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: creds
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }

        public async Task RevokeRefreshTokenAsync(string refreshTokenRaw, string reason = "ManualRevoke")
        {
            var hash = HashToken(refreshTokenRaw);
            var token = await _unitOfWork.Repository<RefreshToken>()
                .GetQueryable()
                .FirstOrDefaultAsync(rt => rt.Token == hash);
                
            if (token != null && token.RevokedAt == null)
            {
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedReason = reason;
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task RevokeAllUserTokensAsync(int userId, string reason = "ManualRevoke")
        {
            var activeTokens = await _unitOfWork.Repository<RefreshToken>()
                .GetQueryable()
                .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedReason = reason;
            }
            
            if (activeTokens.Any())
            {
                await _unitOfWork.SaveChangesAsync();
            }
        }
        
        private async Task RevokeTokenFamilyAsync(string jtiId, string reason)
        {
            var tokens = await _unitOfWork.Repository<RefreshToken>()
                .GetQueryable()
                .Where(rt => rt.JtiId == jtiId && rt.RevokedAt == null)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedReason = reason;
            }
            
            if (tokens.Any())
            {
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public ClaimsPrincipal ValidateAccessToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.Key);
            
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(_jwtSettings.ClockSkewMinutes)
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }

        private static string GenerateSecureToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private static string HashToken(string token)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(token);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
