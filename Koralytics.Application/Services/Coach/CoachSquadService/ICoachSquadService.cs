using Koralytics.Application.DTOs.Coach;

namespace Koralytics.Application.Services.Coach.CoachSquadService
{
    public interface ICoachSquadService
    {
        /// <summary>
        /// Returns the full squad for a team managed by the given coach,
        /// including each player's FIFA-card style rating and availability.
        /// </summary>
        Task<SquadOverviewDto> GetSquadAsync(int coachId, int teamId);

        /// <summary>
        /// Fetches all attending players for a drill session, sorts by overall rating,
        /// and splits them into two balanced training groups via snake-draft alternation.
        /// </summary>
        Task<TrainingTeamSplitDto> SplitTrainingTeamsAsync(int coachId, int sessionId);

        /// <summary>
        /// Returns a side-by-side comparison of two players' category ratings and match stats.
        /// </summary>
        Task<SquadComparisonDto> GetSquadComparisonAsync(int playerAId, int playerBId);
    }
}
