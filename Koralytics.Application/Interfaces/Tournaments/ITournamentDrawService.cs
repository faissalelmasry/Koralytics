
namespace Koralytics.Application.Interfaces.Tournament
{
    public interface ITournamentDrawService
    {
        Task GenerateSeedingAsync(int tournamentId);
        Task GenerateDrawAsync(int tournamentId);
    }
}