using Koralytics.Application.DTOs.AuthDTOs.LoginDTOs;
using Microsoft.AspNetCore.Http;

namespace Koralytics.Application.Interfaces.Auth
{
    public interface ICookieService
    {
        void SetAuthCookies(HttpResponse response, TokenPair tokens);
        void ClearAuthCookies(HttpResponse response);
        string? GetAccessTokenFromCookie(HttpRequest request);
        string? GetRefreshTokenFromCookie(HttpRequest request);
    }
}
