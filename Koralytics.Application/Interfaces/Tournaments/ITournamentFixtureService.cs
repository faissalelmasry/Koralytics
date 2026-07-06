using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.Interfaces.Tournaments
{
    public interface ITournamentFixtureService
    {
        Task UpdateStandingsAsync(int groupId, int matchId);
        Task AdvanceKnockoutAsync(int tournamentId, int roundId);
    }

}
