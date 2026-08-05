using Koralytics.Application.DTOs.AI;
using System.Threading.Tasks;

namespace Koralytics.Application.Interfaces.AI
{
    public interface IAIReportService
    {
        Task<AIReportDto?> GetTournamentReportAsync(int tournamentId);
        Task GenerateTournamentReportAsync(int tournamentId, CancellationToken cancellationToken = default);
    }
}
