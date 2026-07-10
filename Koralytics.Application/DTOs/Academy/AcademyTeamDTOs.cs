namespace Koralytics.Application.DTOs.Academies
{
    // ─── Request DTOs ────────────────────────────────────────────────────────

    public class CreateAgeGroupDto
    {
        public string Name { get; set; } = string.Empty;
        public int MinAge { get; set; }
        public int MaxAge { get; set; }
    }

    public class CreateTeamDto
    {
        public string Name { get; set; } = string.Empty;
        public int AgeGroupId { get; set; }
        public int LocationId { get; set; }
    }

    // ─── Response DTOs ───────────────────────────────────────────────────────

    public class AgeGroupResponseDto
    {
        public int Id { get; set; }
        public int AcademyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MinAge { get; set; }
        public int MaxAge { get; set; }
    }

    public class TeamResponseDto
    {
        public int Id { get; set; }
        public int AcademyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int AgeGroupId { get; set; }
        public string AgeGroupName { get; set; } = string.Empty;
        public int LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public int CoachId { get; set; }
        public string CoachName { get; set; } = string.Empty;

        //list of players in the team
    }

    public class CoachTeamAssignmentDto
    {
        public int CoachId { get; set; }
        public string CoachFullName { get; set; } = string.Empty;
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
    }
}
