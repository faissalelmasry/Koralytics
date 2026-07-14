namespace Koralytics.Application.DTOs.AuthDTOs.LoginDTOs
{
    public record AuthResultDto(AuthResponseDto User, TokenPair Tokens);
}
