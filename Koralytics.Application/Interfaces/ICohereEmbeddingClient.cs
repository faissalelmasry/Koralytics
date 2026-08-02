using System.Threading;
using System.Threading.Tasks;

namespace Koralytics.Application.Interfaces
{
    public interface ICohereEmbeddingClient
    {
        Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default);
    }
}
