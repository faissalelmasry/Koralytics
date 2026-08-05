namespace Koralytics.Application.Options
{
    public class AIOptions
    {
        public const string SectionName = "AI";

        public string Provider { get; set; } = "Local";
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.openai.com/v1";
        public string Model { get; set; } = "gpt-4o-mini";
        public int MaxTokens { get; set; } = 1200;
    }
}
