namespace Koralytics.Application.DTOs.Match
{
    public class AcademyMatchesResponseDto
    {
        public List<MatchResponseDto> Matches { get; set; } = [];
        public List<int> AcademyTeamIds { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
