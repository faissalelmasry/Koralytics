namespace Koralytics.Application.Options
{
    /// <summary>
    /// Cloudflare R2 configuration bound from appsettings.json ("CloudflareR2" section).
    /// R2 is S3-compatible, so we use the AWS SDK pointing at the R2 endpoint.
    /// </summary>
    public class CloudflareR2Options
    {
        public const string SectionName = "CloudflareR2";

        public string AccessKeyId { get; set; } = string.Empty;
        public string SecretAccessKey { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string BucketName { get; set; } = string.Empty;

        /// <summary>
        /// Public base URL for serving files (e.g. https://pub-hash.r2.dev).
        /// </summary>
        public string PublicUrl { get; set; } = string.Empty;

        /// <summary>Max upload size in MB (default 100).</summary>
        public int MaxFileSizeMb { get; set; } = 100;

        /// <summary>Comma-separated allowed video extensions e.g. "mp4,mov,avi".</summary>
        public string AllowedExtensions { get; set; } = "mp4,mov,avi,mkv,webm";
    }
}
