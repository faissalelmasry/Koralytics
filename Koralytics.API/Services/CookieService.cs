using System;
using Koralytics.Application.DTOs.AuthDTOs.LoginDTOs;
using Koralytics.Application.Interfaces.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Koralytics.API.Services
{
    public class CookieService : ICookieService
    {
        private readonly IConfiguration _configuration;

        public CookieService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void SetAuthCookies(HttpResponse response, TokenPair tokens)
        {
            var domain = _configuration["CookieSettings:Domain"];
            var secure = bool.TryParse(_configuration["CookieSettings:Secure"], out var s) && s;
            var sameSite = Enum.TryParse<SameSiteMode>(_configuration["CookieSettings:SameSite"], out var ss) ? ss : SameSiteMode.Lax;

            var accessCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = secure,
                SameSite = sameSite,
                Domain = domain,
                Path = "/",
                Expires = tokens.AccessTokenExpiresAt
            };

            var refreshCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = secure,
                SameSite = sameSite,
                Domain = domain,
                Path = "/",
                Expires = tokens.RefreshTokenExpiresAt
            };

            response.Cookies.Append("access_token", tokens.AccessToken, accessCookieOptions);
            response.Cookies.Append("refresh_token", tokens.RefreshToken, refreshCookieOptions);
        }

        public void ClearAuthCookies(HttpResponse response)
        {
            var domain = _configuration["CookieSettings:Domain"];
            var secure = bool.TryParse(_configuration["CookieSettings:Secure"], out var s) && s;

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = secure,
                Domain = domain,
                Path = "/",
                Expires = DateTime.UtcNow.AddDays(-1)
            };

            response.Cookies.Append("access_token", "", cookieOptions);
            response.Cookies.Append("refresh_token", "", cookieOptions);
        }

        public string? GetAccessTokenFromCookie(HttpRequest request)
        {
            return request.Cookies["access_token"];
        }

        public string? GetRefreshTokenFromCookie(HttpRequest request)
        {
            return request.Cookies["refresh_token"];
        }
    }
}
