namespace Koralytics.Application.Options
{
    /// <summary>
    /// Langflow configuration bound from appsettings.json ("Langflow" section).
    /// Used to connect to the AI Player Search pipeline.
    /// </summary>
    public class LangflowOptions
    {
        public const string SectionName = "Langflow";

        /// <summary>Base URL of the Langflow instance (e.g. http://localhost:7860).</summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>The Flow ID for the AI Player Search pipeline.</summary>
        public string FlowId { get; set; } = string.Empty;
    }
}
