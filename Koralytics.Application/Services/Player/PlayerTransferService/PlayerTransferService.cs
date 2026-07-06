using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Koralytics.Application.Interfaces;
using PlayerEntity = Koralytics.Domain.Entities.Player.Player;
using Koralytics.Domain.Enums;
using Microsoft.Extensions.Logging;
using Koralytics.Domain.Exceptions;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Player;

namespace Koralytics.Application.Services.Player.PlayerTransferService
{
    public class PlayerTransferService:IPlayerTransferService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<PlayerTransferService> _logger;
        private readonly IMapper _mapper;

        public PlayerTransferService(
            IUnitOfWork uow,
            ILogger<PlayerTransferService> logger,
            IMapper mapper)
        {
            _uow = uow;
            _logger = logger;
            _mapper = mapper;
        }
        public async Task LoanPlayerAsync(int playerId, int academyId, int requesterAcademyId)
        {
            _logger.LogInformation("Loaning player {PlayerId} to academy {AcademyId}", playerId, academyId);

            var player = await _uow.Repository<PlayerEntity>().GetByIdAsync(playerId);
            if (player is null)
                throw new NotFoundException($"Player with id {playerId} was not found");

            var newAcademy = await _uow.Repository<Academy>().GetByIdAsync(academyId);
            if (newAcademy is null)
                throw new NotFoundException($"Academy with id {academyId} was not found");

            var currentAcademy = await _uow.Repository<PlayerAcademy>()
                .FindAsync(pa => pa.PlayerId == playerId && pa.LeftAt == null);

            if (requesterAcademyId != academyId && requesterAcademyId != currentAcademy?.AcademyId)
                throw new ForbiddenException("You are not authorized to transfer players to this academy");


            var alreadyInAcademy = await _uow.Repository<PlayerAcademy>()
                   .ExistsAsync(pa => pa.PlayerId == playerId
                                   && pa.AcademyId == academyId
                                   && pa.LeftAt == null);
            if (alreadyInAcademy)
                throw new BadRequestException($"Player is already active in this academy");

            if (currentAcademy is not null)
            {
                currentAcademy.LeftAt = DateTime.UtcNow;
                currentAcademy.Status = PlayerAcademyStatus.Loaned;
            }

            var currentTeams = await _uow.Repository<PlayerTeam>()
                .FindAllAsync(pt => pt.PlayerId == playerId && pt.LeftAt == null);
            foreach (var team in currentTeams)
                team.LeftAt = DateTime.UtcNow;

            var newPlayerAcademy = new PlayerAcademy
            {
                PlayerId = playerId,
                AcademyId = academyId,
                JoinedAt = DateTime.UtcNow,
                Status = PlayerAcademyStatus.Loaned
            };

            await _uow.Repository<PlayerSubscription>().AddAsync(new PlayerSubscription
            {
                PlayerId = playerId,
                AcademyId = academyId,
                Status = SubscriptionStatus.Unpaid,
                PaidByUserId = playerId,
                CreatedAt = DateTime.UtcNow,
                
            });

            await _uow.SaveChangesAsync();

            _logger.LogInformation("Player {PlayerId} successfully loaned to academy {AcademyId}",
                playerId, academyId);
        }

        public async Task TransferPlayerAsync(int playerId, int newAcademyId, int requesterAcademyId)
        {
            _logger.LogInformation("Transferring player {PlayerId} to academy {AcademyId}", playerId, newAcademyId);

            var player = await _uow.Repository<PlayerEntity>().GetByIdAsync(playerId);
            if (player is null)
                throw new NotFoundException($"Player with id {playerId} was not found");
            var currentAcademy = await _uow.Repository<PlayerAcademy>()
                            .FindAsync(pa => pa.PlayerId == playerId && pa.LeftAt == null);

            if (requesterAcademyId != newAcademyId && requesterAcademyId != currentAcademy?.AcademyId)
                throw new ForbiddenException("You are not authorized to transfer players to this academy");

            var newAcademy = await _uow.Repository<Academy>().GetByIdAsync(newAcademyId);
            if (newAcademy is null)
                throw new NotFoundException($"Academy with id {newAcademyId} was not found");

            var alreadyInAcademy = await _uow.Repository<PlayerAcademy>()
                   .ExistsAsync(pa => pa.PlayerId == playerId
                                   && pa.AcademyId == newAcademyId
                                   && pa.LeftAt == null);
            if (alreadyInAcademy)
                throw new BadRequestException($"Player is already active in this academy");

            if (currentAcademy is not null)
            {
                currentAcademy.LeftAt = DateTime.UtcNow;
                currentAcademy.Status = PlayerAcademyStatus.Transferred;
            }

            var currentTeams = await _uow.Repository<PlayerTeam>()
                .FindAllAsync(pt => pt.PlayerId == playerId && pt.LeftAt == null);
            foreach (var team in currentTeams)
                team.LeftAt = DateTime.UtcNow;

            var newPlayerAcademy = new PlayerAcademy
            {
                PlayerId = playerId,
                AcademyId = newAcademyId,
                JoinedAt = DateTime.UtcNow,
                Status = PlayerAcademyStatus.Active
            };

            await _uow.Repository<PlayerSubscription>().AddAsync(new PlayerSubscription
            {
                PlayerId = playerId,
                AcademyId = newAcademyId,
                Status = SubscriptionStatus.Unpaid,
                PaidByUserId=playerId
            });

            await _uow.SaveChangesAsync();

            _logger.LogInformation("Player {PlayerId} successfully transferred to academy {AcademyId}",
                playerId, newAcademyId);
        }

        public async Task UpdateAvailabilityAsync(int playerId, AvailabilityStatus status, int requesterAcademyId, string requesterRole)
        {
            _logger.LogInformation($"Updating availability for player {playerId} to {status}");
            var player = await _uow.Repository<PlayerEntity>().GetByIdAsync(playerId);
            if (player is null)
            {
                _logger.LogWarning("Player {PlayerId} not found", playerId);
                throw new NotFoundException($"Player with id {playerId} was not found");
            }
            if (!Enum.IsDefined(typeof(AvailabilityStatus), status))
                throw new BadRequestException($"Invalid availability status: {status}");
            if (requesterRole == "Coach" || requesterRole == "AcademyAdmin")
            {
                var playerAcademy = await _uow.Repository<PlayerAcademy>()
                    .FindAsNoTrackingAsync(pa => pa.PlayerId == playerId && pa.LeftAt == null);

                if (playerAcademy is null)
                    throw new NotFoundException($"Player with id {playerId} has no active academy");

                if (requesterAcademyId != playerAcademy.AcademyId)
                    throw new ForbiddenException($"You are not authorized to update this player's availability");
            }
            player.AvailabilityStatus = status;
            await _uow.SaveChangesAsync();
            _logger.LogInformation("Player {PlayerId} availability updated to {Status}",
            playerId, status);
        }
    }
}
