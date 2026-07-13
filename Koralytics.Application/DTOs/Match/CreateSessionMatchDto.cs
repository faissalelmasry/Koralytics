using Koralytics.Domain.Enums;

namespace Koralytics.Application.DTOs.Match
{
    public class CreateSessionMatchDto
    {
        public int SessionId { get; set; }
        public List<SessionSidePlayerDto> HomePlayers { get; set; } = [];
        public List<SessionSidePlayerDto> AwayPlayers { get; set; } = [];
        public MatchFormat Format { get; set; }
        public DateTime MatchDate { get; set; }
        public string? Location { get; set; }
    }

    public class SessionSidePlayerDto
    {
        public int PlayerId { get; set; }
        public bool IsStarting { get; set; }
        public int? JerseyNumber { get; set; }
    }
}
