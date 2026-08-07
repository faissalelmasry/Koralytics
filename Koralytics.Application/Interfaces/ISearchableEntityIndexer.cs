using System.Threading;
using System.Threading.Tasks;

namespace Koralytics.Application.Interfaces
{
    public interface ISearchableEntityIndexer
    {
        Task IndexAsync(
            string entityType,
            int referenceId,
            string textValue,
            CancellationToken ct = default,
            int? academyId = null);
        Task UpdateAcademyIdAsync(
            string entityType,
            int referenceId,
            int? academyId,
            CancellationToken ct = default);
    }
}