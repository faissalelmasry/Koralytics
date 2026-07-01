using Koralytics.Domain.Models.BaseModels;


namespace Koralytics.Domain.Entities.Tournamet
{
    public class TournamentRound : BaseEntity
    {
        public int TournamentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int RoundNumber { get; set; }

        public Tournament Tournament { get; set; } = null!;

        public ICollection<TournamentFixture> TournamentFixtures { get; set; } = [];
    }
}
