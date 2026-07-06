using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Enums;

namespace Koralytics.Application.Services.Player.PlayerTransferService
{
    public interface IPlayerTransferService
    {
        Task TransferPlayerAsync(int playerId, int newAcademyId, int requesterAcademyId);
        Task LoanPlayerAsync(int playerId, int academyId, int requesterAcademyId);
        Task UpdateAvailabilityAsync(int playerId, AvailabilityStatus status, int requesterAcademyId, string requesterRole);
    }
}