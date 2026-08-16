namespace Koralytics.Application.Options
{
    public class ItiChatOptions
    {
        public const string SectionName = "ItiChat";

        public string ApiKey { get; set; } = string.Empty;
        public string ModelId { get; set; } = "openai.gpt-oss-120b-1:0";
        public string BaseUrl { get; set; } = "http://apiaccess.iti.net.eg/api/v1/student/chat";
        public int MaxTokens { get; set; } = 1024;
    }
}
