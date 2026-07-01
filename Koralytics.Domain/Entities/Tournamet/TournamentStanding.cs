using Koralytics.Domain.Models.BaseModels;

namespace Koralytics.Domain.Entities.Tournamet
{
    public class TournamentStanding : BaseEntity
    {
        public int GroupId { get; set; }
        public int TournamentTeamId { get; set; }
        public int Played { get; set; }
        public int Won { get; set; }
        public int Drawn { get; set; }
        public int Lost { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public int Points { get; set; }
        public int GoalDifference => GoalsFor - GoalsAgainst;
        public TournamentGroup Group { get; set; } = null!;
        public TournamentTeam TournamentTeam { get; set; } = null!;
    }
}
