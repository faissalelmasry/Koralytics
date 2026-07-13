using AutoMapper;
using TournamentEntity = Koralytics.Domain.Entities.Tournamet.Tournament;
using TournamentGroupEntity = Koralytics.Domain.Entities.Tournamet.TournamentGroup;
using TournamentTeamEntity = Koralytics.Domain.Entities.Tournamet.TournamentTeam;
using TournamentSquadEntity = Koralytics.Domain.Entities.Tournamet.TournamentSquad;
using PlayerTeamEntity = Koralytics.Domain.Entities.Player.PlayerTeam;
using AcademyEntity= Koralytics.Domain.Entities.Academy.Academy;
using Koralytics.Application.DTOs.Tournament;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Tournament;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Koralytics.Application.Services.Tournament
{
    public class TournamentService : ITournamentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<TournamentService> _logger;

        public TournamentService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<TournamentService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<TournamentDto> CreateTournamentAsync(
            CreateTournamentDto dto, int requestingUserId)
        {
            _logger.LogInformation(
                "User {UserId} attempting to create tournament {Name}",
                requestingUserId, dto.Name);

            // Validate AgeGroup exists
            var ageGroup = await _unitOfWork.Repository<AgeGroup>()
                .FindAsync(a => a.Id == dto.AgeGroupId);

            if (ageGroup is null)
                throw new NotFoundException(
                    $"AgeGroup with Id {dto.AgeGroupId} not found");

            // Validate dates
            if (dto.EndDate <= dto.StartDate)
                throw new BadRequestException(
                    "EndDate must be after StartDate");

            // Validate tournament name is unique
            var nameExists = await _unitOfWork.Repository<TournamentEntity>()
                .ExistsAsync(t => t.Name == dto.Name);

            if (nameExists)
                throw new ConflictException(
                    $"Tournament with name '{dto.Name}' already exists");

            // Wrap in transaction — tournament + dummy group must be atomic
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Map and create tournament
                var tournament = _mapper.Map<TournamentEntity>(dto);
                tournament.Status = TournamentStatus.Draft;

                await _unitOfWork.Repository<TournamentEntity>()
                    .AddAsync(tournament);
                await _unitOfWork.SaveChangesAsync();

                // If structure is League → auto create dummy group
                if (dto.Structure == TournamentStructure.League)
                {
                    var dummyGroup = new TournamentGroupEntity
                    {
                        TournamentId = tournament.Id,
                        Name = "League",
                        IsDummy = true
                    };
                    await _unitOfWork.Repository<TournamentGroupEntity>()
                        .AddAsync(dummyGroup);
                    await _unitOfWork.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                // Reload with AgeGroup for mapping
                var created = await _unitOfWork.Repository<TournamentEntity>()
                    .GetQueryable()
                    .Include(t => t.AgeGroup)
                    .FirstOrDefaultAsync(t => t.Id == tournament.Id);

                _logger.LogInformation(
                    "Tournament {Name} created successfully with Id {Id}",
                    tournament.Name, tournament.Id);

                return _mapper.Map<TournamentDto>(created!);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task InviteAcademyAsync(int tournamentId, int academyId)
        {
            _logger.LogInformation(
                "Inviting academy {AcademyId} to tournament {TournamentId}",
                academyId, tournamentId);

            // Validate tournament exists and is in Registration status
            var tournament = await _unitOfWork.Repository<TournamentEntity>()
                .FindAsync(t => t.Id == tournamentId);

            if (tournament is null)
                throw new NotFoundException(
                    $"Tournament with Id {tournamentId} not found");

            if (tournament.Status != TournamentStatus.Registration)
                throw new BadRequestException(
                    "Tournament must be in Registration status to invite teams");

            // Validate academy exists
            var academy = await _unitOfWork.Repository<AcademyEntity>()
                .FindAsync(a => a.Id == academyId);

            if (academy is null)
                throw new NotFoundException(
                    $"Academy with Id {academyId} not found");

            // Validate academy not already invited
            var alreadyInvited = await _unitOfWork.Repository<TournamentTeamEntity>()
                .ExistsAsync(tt =>
                    tt.TournamentId == tournamentId &&
                    tt.Team.AcademyId == academyId);

            if (alreadyInvited)
                throw new ConflictException(
                    "This academy is already invited to the tournament");

            // Get the academy's team for this tournament's age group
            var team = await _unitOfWork.Repository<Team>()
                .FindAsync(t =>
                    t.AcademyId == academyId &&
                    t.AgeGroupId == tournament.AgeGroupId);

            if (team is null)
                throw new NotFoundException(
                    $"Academy {academyId} has no team in the tournament's age group");

            var tournamentTeam = new TournamentTeamEntity
            {
                TournamentId = tournamentId,
                TeamId = team.Id,
                Status = TournamentTeamStatus.Invited,
                RegisteredAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<TournamentTeamEntity>()
                .AddAsync(tournamentTeam);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Academy {AcademyId} successfully invited to tournament {TournamentId}",
                academyId, tournamentId);
        }

        public async Task AcceptInvitationAsync(int tournamentId, int academyId)
        {
            _logger.LogInformation(
                "Academy {AcademyId} accepting invitation for tournament {TournamentId}",
                academyId, tournamentId);

            // Find the tournament team record for this academy
            var tournamentTeam = await _unitOfWork.Repository<TournamentTeamEntity>()
                .GetQueryable()
                .Include(tt => tt.Team)
                .FirstOrDefaultAsync(tt =>
                    tt.TournamentId == tournamentId &&
                    tt.Team.AcademyId == academyId);

            if (tournamentTeam is null)
                throw new NotFoundException(
                    "Tournament invitation not found for this academy");

            if (tournamentTeam.Status != TournamentTeamStatus.Invited)
                throw new BadRequestException(
                    "Academy has already responded to this invitation");

            tournamentTeam.Status = TournamentTeamStatus.Accepted;
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Academy {AcademyId} accepted invitation for tournament {TournamentId}",
                academyId, tournamentId);
        }

        public async Task RegisterSquadAsync(
            int tournamentId, int teamId, List<int> playerIds)
        {
            _logger.LogInformation(
                "Registering squad for team {TeamId} in tournament {TournamentId}",
                teamId, tournamentId);

            // Validate tournament exists and is in Registration status
            var tournament = await _unitOfWork.Repository<TournamentEntity>()
                .FindAsync(t => t.Id == tournamentId);

            if (tournament is null)
                throw new NotFoundException(
                    $"Tournament with Id {tournamentId} not found");

            if (tournament.Status != TournamentStatus.Registration)
                throw new BadRequestException(
                    "Tournament must be in Registration status to register squad");

            // Validate team is accepted in this tournament
            var tournamentTeam = await _unitOfWork.Repository<TournamentTeamEntity>()
                .FindAsync(tt =>
                    tt.TournamentId == tournamentId &&
                    tt.TeamId == teamId &&
                    tt.Status == TournamentTeamStatus.Accepted);

            if (tournamentTeam is null)
                throw new BadRequestException(
                    "Team must accept the tournament invitation before registering a squad");

            // Validate no duplicate playerIds in request
            if (playerIds.Count != playerIds.Distinct().Count())
                throw new BadRequestException(
                    "Duplicate players found in squad registration");

            // Validate minimum player count matches format
            var minPlayers = (int)tournament.Format;
            if (playerIds.Count < minPlayers)
                throw new BadRequestException(
                    $"Squad must have at least {minPlayers} players " +
                    $"for {tournament.Format} format");

            // Validate maximum player count
            var maxPlayers = (int)tournament.Format + 5;
            if (playerIds.Count > maxPlayers)
                throw new BadRequestException(
                    $"Squad exceeds maximum allowed players " +
                    $"for {tournament.Format} format");

            // Validate each player belongs to the team
            // and is not already registered in this tournament
            foreach (var playerId in playerIds)
            {
                // Check player belongs to this team
                var belongsToTeam = await _unitOfWork.Repository<PlayerTeamEntity>()
                    .ExistsAsync(pt =>
                        pt.PlayerId == playerId &&
                        pt.TeamId == teamId &&
                        pt.LeftAt == null);

                if (!belongsToTeam)
                    throw new BadRequestException(
                        $"Player {playerId} does not belong to team {teamId}");

                // Check player not already registered in this tournament
                var alreadyRegistered = await _unitOfWork
                    .Repository<TournamentSquadEntity>()
                    .ExistsAsync(ts =>
                        ts.TournamentId == tournamentId &&
                        ts.PlayerId == playerId);

                if (alreadyRegistered)
                    throw new ConflictException(
                        $"Player {playerId} is already registered " +
                        $"in this tournament");
            }

            // Create squad records
            var squadRecords = playerIds.Select(playerId =>
                new TournamentSquadEntity
                {
                    TournamentId = tournamentId,
                    TeamId = teamId,
                    PlayerId = playerId,
                    RegisteredAt = DateTime.UtcNow
                }).ToList();

            await _unitOfWork.Repository<TournamentSquadEntity>()
                .AddRangeAsync(squadRecords);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Successfully registered {Count} players for team {TeamId} " +
                "in tournament {TournamentId}",
                playerIds.Count, teamId, tournamentId);
        }

        public async Task UpdateStatusAsync(
            int tournamentId, TournamentStatus status)
        {
            var tournament = await _unitOfWork.Repository<TournamentEntity>()
                .FindAsync(t => t.Id == tournamentId);

            if (tournament is null)
                throw new NotFoundException(
                    $"Tournament with Id {tournamentId} not found");

            tournament.Status = status;
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Tournament {Id} status updated to {Status}",
                tournamentId, status);
        }
    }
}