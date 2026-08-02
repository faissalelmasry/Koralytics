namespace Koralytics.Infrastructure.Configuration
{
    public class CohereOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "embed-multilingual-v3.0";
        public string BaseUrl { get; set; } = "https://api.cohere.com/v1";
    }
}
