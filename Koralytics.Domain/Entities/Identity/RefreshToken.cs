using System;
using Koralytics.Domain.Interfaces;

namespace Koralytics.Domain.Entities.Identity
{
    public class RefreshToken : ISoftDelete
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        
        public string Token { get; set; } = string.Empty;        // SHA-256 hash of the actual token
        public string JtiId { get; set; } = string.Empty;        // Unique ID for token family (rotation tracking)
        
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? ReplacedByToken { get; set; }              // Points to the hash of the new token (rotation chain)
        public string? RevokedReason { get; set; }                // "Rotation", "ManualRevoke", "PasswordChanged", "SecurityBreach"
        
        public string? DeviceInfo { get; set; }                   // Optional: User-Agent for session management
        public string? IpAddress { get; set; }                    // Optional: For security audit
        
        public bool IsDeleted { get; set; }
        
        public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
    }
}
