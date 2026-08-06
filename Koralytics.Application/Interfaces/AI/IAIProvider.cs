using System.Threading;
using System.Threading.Tasks;

namespace Koralytics.Application.Interfaces.AI
{
    public interface IAIProvider
    {
        Task<string> GenerateTournamentReportAsync(string prompt, CancellationToken cancellationToken = default);
    }
}
