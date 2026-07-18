using System.ComponentModel.DataAnnotations;

namespace Koralytics.Application.DTOs.Analytics
{
    /// <summary>
    /// Payload sent by the Angular frontend to the AI Player Search endpoint.
    /// </summary>
    public class AiSearchRequestDto
    {
        /// <summary>
        /// The natural-language query (e.g. "show me left-footed strikers with a rating above 7").
        /// </summary>
        [Required(ErrorMessage = "A search query is required.")]
        [MinLength(3, ErrorMessage = "Query must be at least 3 characters long.")]
        public string Query { get; set; } = string.Empty;
    }
}
