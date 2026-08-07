using Koralytics.Application.DTOs.Parent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Koralytics.Application.Interfaces
{
    public interface IParentAiService
    {
        /// <summary>
        /// parentUserId and authorizedPlayerId are resolved server-side by the
        /// controller from authenticated claims + DB lookup — never pass
        /// values taken directly from client input here.
        /// </summary>
        Task<ParentChatResponse> ProcessParentQueryAsync(
            ParentChatRequest request,
            string parentUserId,
             IReadOnlyList<int> authorizedPlayerId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Streams raw SSE "data: ..." JSON lines (see ParentChatStreamChunk)
        /// as the Langflow run produces them, ending with one "meta" event
        /// carrying the same summary fields as ProcessParentQueryAsync.
        /// </summary>
        IAsyncEnumerable<string> StreamParentQueryAsync(
            ParentChatRequest request,
            string parentUserId,
             IReadOnlyList<int> authorizedPlayerId,
            CancellationToken cancellationToken = default);
    }
}