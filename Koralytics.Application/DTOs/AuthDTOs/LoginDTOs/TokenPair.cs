using System;

namespace Koralytics.Application.DTOs.AuthDTOs.LoginDTOs
{
    public record TokenPair(
        string AccessToken, 
        DateTime AccessTokenExpiresAt, 
        string RefreshToken, 
        DateTime RefreshTokenExpiresAt
    );
}
