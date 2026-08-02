using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Application.DTOs.Match;
using Koralytics.Domain.Enums;

namespace Koralytics.Application.Interfaces.Match
{
    public interface IMatchReportService
    {
        Task<string> GenerateMatchReportAsync(int matchId);
        Task<AIReportResponseDto> GetMatchReportAsync(int referenceId, AIReportType reportType = AIReportType.Match);
    }
}
