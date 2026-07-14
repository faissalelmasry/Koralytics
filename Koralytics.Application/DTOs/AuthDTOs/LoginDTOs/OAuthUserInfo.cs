namespace Koralytics.Application.DTOs.AuthDTOs.LoginDTOs
{
    public record OAuthUserInfo(
        string ProviderId,
        string Email,
        string FirstName,
        string LastName,
        string? ProfileImageUrl
    );
}
