using Koralytics.Application.DTOs.Tournaments;
using System.Threading.Tasks;

namespace Koralytics.Application.Interfaces.Tournaments
{
    public interface ITournamentFixtureService
    {
        Task<TournamentFixtureDetailDto> GetFixtureDetailsAsync(int fixtureId);
        Task UpdateStandingsAsync(int groupId, int matchId);
        Task AdvanceKnockoutAsync(int tournamentId, int roundId);
        Task UpdateFixtureResultAsync(int fixtureId, int homeScore, int awayScore);
        Task GenerateKnockoutFromGroupsAsync(int tournamentId);
        Task UpdateFixtureStatsAsync(int fixtureId, UpdateFixtureStatsDto dto);
    }
}
