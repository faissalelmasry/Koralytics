namespace Koralytics.Application.Options
{
    public class GroqOptions
    {
        public const string SectionName = "Groq";

        public string ApiKey { get; set; } = string.Empty;
        public string ModelName { get; set; } = "llama-3.3-70b-versatile";
        public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1/";
    }
}
