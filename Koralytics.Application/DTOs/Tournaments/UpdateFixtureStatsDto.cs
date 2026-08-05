using System.Collections.Generic;

namespace Koralytics.Application.DTOs.Tournaments
{
    public class UpdateFixtureStatsDto
    {
        public List<GoalEventDto> Goals { get; set; } = [];
        public int? MotmPlayerId { get; set; }
    }

    public class GoalEventDto
    {
        public int PlayerId { get; set; }
        public int? AssistPlayerId { get; set; }
        public int Minute { get; set; }
        public bool IsHomeSide { get; set; }
    }
}
