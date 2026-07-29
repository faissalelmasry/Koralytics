namespace Koralytics.Application.DTOs.Subscription
{
    public class PaymentIntentResponseDto
    {
        public string ClientSecret { get; set; } = string.Empty;
        public string PublishableKey { get; set; } = string.Empty;
    }
}