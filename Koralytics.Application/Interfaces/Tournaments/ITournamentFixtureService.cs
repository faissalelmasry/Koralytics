using Koralytics.Application.DTOs.Tournaments;
using System.Threading.Tasks;

namespace Koralytics.Application.Interfaces.Tournaments
{
    public interface ITournamentFixtureService
    {
        Task<TournamentFixtureDetailDto> GetFixtureDetailsAsync(int fixtureId);
        Task UpdateStandingsAsync(int groupId, int matchId);
        Task AdvanceKnockoutAsync(int tournamentId, int roundId);
    }
}
