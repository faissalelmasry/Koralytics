using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Scouter;
using ScouterEntity = Koralytics.Domain.Entities.Scouter.Scouter;
using ScouterReport = Koralytics.Domain.Entities.Scouter.ScouterReport;
using Koralytics.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.Services.Scouter.ScouterReportService
{
    public class ScouterReportService : IScouterReportService
    {
        private readonly IUnitOfWork _unitOfWork;
       
        public ScouterReportService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
           
        }
        //need aiservice
        public Task<string> GenerateScoutingReportAsync(int scouterId, int playerId)
        {
            throw new NotImplementedException();
        }

        public async Task<ScouterReport> GetScoutingReportAsync(int scouterId, int playerId)
        {
            var existingReport = await _unitOfWork.Repository<ScouterReport>()
                .FindAsync(r => r.ScouterUserId == scouterId && r.PlayerId == playerId);

            if (existingReport != null)
            {
                return existingReport;
            }

            await GenerateScoutingReportAsync(scouterId, playerId);

            var newlyGeneratedReport = await _unitOfWork.Repository<ScouterReport>()
                .FindAsync(r => r.ScouterUserId == scouterId && r.PlayerId == playerId);

            if (newlyGeneratedReport == null)
            {
                throw new InvalidOperationException("Failed to retrieve the scouting report after generation.");
            }

            return newlyGeneratedReport;
        }

        public async Task<bool> VerifyScouterAsync(int scouterId)
        {
            var scouter = await _unitOfWork.Repository<ScouterEntity>()
                .FindAsync(s => s.Id == scouterId);

            if (scouter == null)
            {
                throw new NotFoundException($"Scouter with User ID {scouterId} not found.");
            }
            scouter.IsVerified = true;
            scouter.VerifiedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
