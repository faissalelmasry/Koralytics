namespace Koralytics.Application.DTOs.Match
{
    public class SubmitLineupDto
    {
        public List<SubmitLineupPlayerDto> Players { get; set; } = [];
    }

    public class SubmitLineupPlayerDto
    {
        public int PlayerId { get; set; }
        public int TeamId { get; set; }
        public bool IsStarting { get; set; }
        public int? JerseyNumber { get; set; }
    }

    public class LineupResponseDto
    {
        public int Id { get; set; }
        public int MatchId { get; set; }
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public bool IsStarting { get; set; }
        public int? JerseyNumber { get; set; }
        public bool? IsHomeSide { get; set; }
    }
}
