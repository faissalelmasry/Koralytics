using System.Threading;
using System.Threading.Tasks;

namespace Koralytics.Application.Interfaces
{
    public interface ISearchableEntityIndexer
    {
        Task IndexAsync(string entityType, int referenceId, string textValue, CancellationToken ct = default);
    }
}
