using System.ComponentModel.DataAnnotations;

namespace Koralytics.Application.DTOs.Scouter
{
    public class AIChatBotRequestDto
    {
        [Required(ErrorMessage = "Message is required.")]
        public string Message { get; set; } = string.Empty;

        public string? SessionId { get; set; }
    }
}
