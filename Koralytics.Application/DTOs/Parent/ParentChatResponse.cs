using System;
using System.Text.Json.Serialization;

namespace Koralytics.Application.DTOs.Parent
{
    /// <summary>
    /// Inbound request from the parent's browser.
    /// IMPORTANT: This DTO intentionally has NO ParentId field. The parent's
    /// identity is always resolved server-side from the authenticated user's
    /// claims — never trust an identity value coming from the client.
    /// </summary>
    public class ParentChatRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Optional. Only meaningful for parents with more than one child.
        /// This value is NEVER trusted directly — the controller always
        /// verifies it against the authenticated parent's actual children
        /// before use. If omitted, the parent's first/only linked child
        /// is used.
        /// </summary>
        public int? RequestedPlayerId { get; set; }
    }

    public class ParentChatResponse
    {
        public string SessionId { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public bool UsedSQL { get; set; }
        public bool UsedRAG { get; set; }
        public double ResponseTime { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Success { get; set; }

        /// <summary>
        /// Server-side diagnostics only. Marked JsonIgnore so it can NEVER
        /// accidentally be serialized back to the client, even if a future
        /// change forgets to strip it before returning the DTO.
        /// </summary>
        [JsonIgnore]
        public string? InternalErrorDetail { get; set; }
    }

    /// <summary>
    /// A single Server-Sent-Event chunk relayed to the client during streaming.
    /// "token" events carry partial answer text; the final "meta" event carries
    /// the same summary fields as ParentChatResponse once the run completes.
    /// </summary>
    public class ParentChatStreamChunk
    {
        public string EventType { get; set; } = "token"; // "token" | "meta" | "error"
        public string? Text { get; set; }
        public ParentChatResponse? Meta { get; set; }
    }
}
