using Koralytics.Domain.Enums;

    namespace Koralytics.Application.DTOs.Tournament
    {
        public class TournamentDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public MatchFormat Format { get; set; }
            public TournamentStructure Structure { get; set; }
            public string AgeGroupName { get; set; } = string.Empty;
            public bool HasTwoLegs { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public TournamentStatus Status { get; set; }
        }
    }

