using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Scouter;
using ScouterEntity = Koralytics.Domain.Entities.Scouter.Scouter;
using ScouterReport = Koralytics.Domain.Entities.Scouter.ScouterReport;
using Koralytics.Domain.Exceptions;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<ScouterReportService> _logger;

        public ScouterReportService(IUnitOfWork unitOfWork, ILogger<ScouterReportService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        //need aiservice
        public Task<string> GenerateScoutingReportAsync(int scouterId, int playerId)
        {
            throw new NotImplementedException();
        }

        public async Task<ScouterReport> GetScoutingReportAsync(int scouterId, int playerId)
        {
            _logger.LogInformation("Retrieving scouting report. ScouterId: {ScouterId}, PlayerId: {PlayerId}", scouterId, playerId);
            var existingReport = await _unitOfWork.Repository<ScouterReport>()
                .FindAsync(r => r.ScouterUserId == scouterId && r.PlayerId == playerId);

            if (existingReport != null)
            {
                _logger.LogInformation("Cached scouting report record found in database. ReportId: {ReportId}", existingReport.Id);
                return existingReport;
            }
            
            _logger.LogInformation("No existing scouting report found for Scouter {ScouterId} and Player {PlayerId}. Requesting AI regeneration.", scouterId, playerId);
            await GenerateScoutingReportAsync(scouterId, playerId);

            var newlyGeneratedReport = await _unitOfWork.Repository<ScouterReport>()
                .FindAsync(r => r.ScouterUserId == scouterId && r.PlayerId == playerId);

            if (newlyGeneratedReport == null)
            {
                throw new InvalidOperationException("Failed to retrieve the scouting report after generation.");
            }

            _logger.LogInformation("Successfully loaded newly generated scouting report. ReportId: {ReportId}", newlyGeneratedReport.Id);
            return newlyGeneratedReport;
        }

        public async Task<bool> VerifyScouterAsync(int scouterId)
        {
            _logger.LogInformation("SuperAdmin verification request received for ScouterUserId: {ScouterId}", scouterId);
            var scouter = await _unitOfWork.Repository<ScouterEntity>()
                .FindAsync(s => s.Id == scouterId);

            if (scouter == null)
            {
                throw new NotFoundException($"Scouter with User ID {scouterId} not found.");
            }

            _logger.LogInformation("Flipping Scouter verification flag to True. ScouterUserId: {ScouterId}", scouterId);
            scouter.IsVerified = true;
            scouter.VerifiedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Successfully completed credential verification for ScouterId: {ScouterId}", scouterId);
            return true;
        }
    }
}
