namespace Koralytics.Application.DTOs.AuthDTOs.RegisterDTOs;

public class SendEmailConfirmationDto
{
    public int UserId { get; set; }
}

public class ConfirmEmailDto
{
    public int UserId { get; set; }
    public string Token { get; set; } = string.Empty;
}
