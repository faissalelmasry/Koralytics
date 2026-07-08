namespace Koralytics.Application.DTOs.Coach
{
    public class GrantTempAccessDto
    {
        /// <summary>
        /// The UserId of the coach/user being granted access.
        /// </summary>
        public int GrantedToUserId { get; set; }

        /// <summary>
        /// Access level scope (e.g. "ReadOnly", "FullSquad").
        /// </summary>
        public string AccessLevel { get; set; } = string.Empty;

        /// <summary>
        /// When this access grant expires (UTC).
        /// </summary>
        public DateTime ExpiresAt { get; set; }
    }
}
