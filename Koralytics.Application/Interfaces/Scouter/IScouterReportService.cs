using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Entities.Scouter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.Interfaces.Scouter
{
    public interface IScouterReportService
    {
        Task<String> GenerateScoutingReportAsync(int scouterId, int playerId);
        Task<ScouterReport> GetScoutingReportAsync(int scouterId, int playerId);
        Task<bool> VerifyScouterAsync(int scouterId);
    }
}
