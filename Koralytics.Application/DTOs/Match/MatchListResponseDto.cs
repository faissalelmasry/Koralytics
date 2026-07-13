namespace Koralytics.Application.DTOs.Match
{
    public class MatchListResponseDto
    {
        public List<MatchResponseDto> Matches { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
