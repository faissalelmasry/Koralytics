using Koralytics.Application.DTOs.Tournaments;
namespace Koralytics.Application.Interfaces.Tournament
{
    public interface ITournamentReportService
    {
        Task CompleteTournamentAsync(int tournamentId);
        Task<BracketDto> GetBracketAsync(int tournamentId);
        Task<List<HallOfFameDto>> GetHallOfFameAsync(int tournamentId);
    }
}
