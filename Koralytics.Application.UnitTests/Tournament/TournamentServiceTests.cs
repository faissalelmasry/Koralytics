using System.Linq.Expressions;
using AutoMapper;
using Koralytics.Application.DTOs.Tournament;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Services.Tournament;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable;
using MockQueryable.Moq;
using Xunit;
using TournamentEntity = Koralytics.Domain.Entities.Tournamet.Tournament;
using TournamentTeamEntity = Koralytics.Domain.Entities.Tournamet.TournamentTeam;
using TournamentSquadEntity = Koralytics.Domain.Entities.Tournamet.TournamentSquad;
using PlayerTeamEntity = Koralytics.Domain.Entities.Player.PlayerTeam;
namespace Koralytics.Application.UnitTests.Tournament
{
    public class TournamentServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<TournamentService>> _loggerMock;
        private readonly Mock<IRepository<TournamentEntity>> _tournamentRepoMock;
        private readonly Mock<IRepository<AgeGroup>> _ageGroupRepoMock;
        private readonly Mock<IRepository<TournamentTeamEntity>> _tournamentTeamRepoMock;
        private readonly Mock<IRepository<TournamentSquadEntity>> _tournamentSquadRepoMock;
        private readonly Mock<IRepository<PlayerTeamEntity>> _playerTeamRepoMock;
        private readonly Mock<IRepository<Academy>> _academyRepoMock;
        private readonly Mock<IRepository<Team>> _teamRepoMock;
        private readonly TournamentService _service;

        public TournamentServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<TournamentService>>();
            _tournamentRepoMock = new Mock<IRepository<TournamentEntity>>();
            _ageGroupRepoMock = new Mock<IRepository<AgeGroup>>();
            _tournamentTeamRepoMock = new Mock<IRepository<TournamentTeamEntity>>();
            _tournamentSquadRepoMock = new Mock<IRepository<TournamentSquadEntity>>();
            _playerTeamRepoMock = new Mock<IRepository<PlayerTeamEntity>>();
            _academyRepoMock = new Mock<IRepository<Academy>>();
            _teamRepoMock = new Mock<IRepository<Team>>();

            _unitOfWorkMock
                .Setup(u => u.Repository<TournamentEntity>())
                .Returns(_tournamentRepoMock.Object);
            _unitOfWorkMock
                .Setup(u => u.Repository<AgeGroup>())
                .Returns(_ageGroupRepoMock.Object);
            _unitOfWorkMock
                .Setup(u => u.Repository<TournamentTeamEntity>())
                .Returns(_tournamentTeamRepoMock.Object);
            _unitOfWorkMock
                .Setup(u => u.Repository<TournamentSquadEntity>())
                .Returns(_tournamentSquadRepoMock.Object);
            _unitOfWorkMock
                .Setup(u => u.Repository<PlayerTeamEntity>())
                .Returns(_playerTeamRepoMock.Object);
            _unitOfWorkMock
                .Setup(u => u.Repository<Academy>())
                .Returns(_academyRepoMock.Object);
            _unitOfWorkMock
                .Setup(u => u.Repository<Team>())
                .Returns(_teamRepoMock.Object);

            _service = new TournamentService(
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        // ─── CreateTournamentAsync ───────────────────────────────────

        [Fact]
        public async Task CreateTournamentAsync_AgeGroupNotFound_ThrowsNotFoundException()
        {
            _ageGroupRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<AgeGroup, bool>>>()))
                .ReturnsAsync((AgeGroup?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.CreateTournamentAsync(
                    new CreateTournamentDto { AgeGroupId = 99 }, 1));
        }

        [Fact]
        public async Task CreateTournamentAsync_EndDateBeforeStartDate_ThrowsBadRequestException()
        {
            _ageGroupRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<AgeGroup, bool>>>()))
                .ReturnsAsync(new AgeGroup { Id = 1 });

            var dto = new CreateTournamentDto
            {
                AgeGroupId = 1,
                StartDate = DateTime.UtcNow.AddDays(10),
                EndDate = DateTime.UtcNow.AddDays(5)
            };

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.CreateTournamentAsync(dto, 1));
        }

        [Fact]
        public async Task CreateTournamentAsync_DuplicateName_ThrowsConflictException()
        {
            _ageGroupRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<AgeGroup, bool>>>()))
                .ReturnsAsync(new AgeGroup { Id = 1 });

            _tournamentRepoMock
                .Setup(r => r.ExistsAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync(true);

            var dto = new CreateTournamentDto
            {
                Name = "Summer Cup",
                AgeGroupId = 1,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            await Assert.ThrowsAsync<ConflictException>(() =>
                _service.CreateTournamentAsync(dto, 1));
        }

        // ─── InviteAcademyAsync ──────────────────────────────────────

        [Fact]
        public async Task InviteAcademyAsync_TournamentNotFound_ThrowsNotFoundException()
        {
            _tournamentRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync((TournamentEntity?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.InviteAcademyAsync(1, 1));
        }

        [Fact]
        public async Task InviteAcademyAsync_TournamentNotInRegistration_ThrowsBadRequestException()
        {
            _tournamentRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync(new TournamentEntity
                {
                    Status = TournamentStatus.InProgress
                });

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.InviteAcademyAsync(1, 1));
        }

        [Fact]
        public async Task InviteAcademyAsync_AcademyNotFound_ThrowsNotFoundException()
        {
            _tournamentRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync(new TournamentEntity
                {
                    Status = TournamentStatus.Registration
                });

            _academyRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Academy, bool>>>()))
                .ReturnsAsync((Academy?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.InviteAcademyAsync(1, 1));
        }

        [Fact]
        public async Task InviteAcademyAsync_AcademyAlreadyInvited_ThrowsConflictException()
        {
            _tournamentRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync(new TournamentEntity
                {
                    Status = TournamentStatus.Registration
                });

            _academyRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Academy, bool>>>()))
                .ReturnsAsync(new Academy { Id = 1 });

            _tournamentTeamRepoMock
                .Setup(r => r.ExistsAsync(
                    It.IsAny<Expression<Func<TournamentTeamEntity, bool>>>()))
                .ReturnsAsync(true);

            await Assert.ThrowsAsync<ConflictException>(() =>
                _service.InviteAcademyAsync(1, 1));
        }

        [Fact]
        public async Task InviteAcademyAsync_NoTeamInAgeGroup_ThrowsNotFoundException()
        {
            _tournamentRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync(new TournamentEntity
                {
                    Status = TournamentStatus.Registration,
                    AgeGroupId = 1
                });

            _academyRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Academy, bool>>>()))
                .ReturnsAsync(new Academy { Id = 1 });

            _tournamentTeamRepoMock
                .Setup(r => r.ExistsAsync(
                    It.IsAny<Expression<Func<TournamentTeamEntity, bool>>>()))
                .ReturnsAsync(false);

            _teamRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync((Team?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.InviteAcademyAsync(1, 1));
        }

        [Fact]
        public async Task AcceptInvitationAsync_InvitationNotFound_ThrowsNotFoundException()
        {
            // Use DbContextOptions to create a real in-memory queryable
            // OR mock GetQueryable to return data with navigation properties populated
            _tournamentTeamRepoMock
                .Setup(r => r.GetQueryable())
                .Returns(new List<TournamentTeamEntity>().BuildMock());

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.AcceptInvitationAsync(1, 1));
        }

        [Fact]
        public async Task AcceptInvitationAsync_AlreadyAccepted_ThrowsBadRequestException()
        {
            // Navigation property Team must be populated manually
            // since Include() doesn't work on in-memory lists
            var team = new Team { Id = 1, AcademyId = 1 };

            var tournamentTeam = new TournamentTeamEntity
            {
                TournamentId = 1,
                TeamId = 1,
                Status = TournamentTeamStatus.Accepted,
                Team = team  // ← manually set navigation property
            };

            _tournamentTeamRepoMock
                .Setup(r => r.GetQueryable())
                .Returns(new List<TournamentTeamEntity>
                {
            tournamentTeam
                }.BuildMock());

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.AcceptInvitationAsync(1, 1));
        }
        // ─── RegisterSquadAsync ──────────────────────────────────────

        [Fact]
        public async Task RegisterSquadAsync_TournamentNotFound_ThrowsNotFoundException()
        {
            _tournamentRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync((TournamentEntity?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.RegisterSquadAsync(1, 1, [1, 2, 3, 4, 5]));
        }

        [Fact]
        public async Task RegisterSquadAsync_DuplicatePlayers_ThrowsBadRequestException()
        {
            _tournamentRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync(new TournamentEntity
                {
                    Status = TournamentStatus.Registration,
                    Format = MatchFormat.FiveSide
                });

            _tournamentTeamRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentTeamEntity, bool>>>()))
                .ReturnsAsync(new TournamentTeamEntity
                {
                    Status = TournamentTeamStatus.Accepted
                });

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.RegisterSquadAsync(1, 1, [1, 1, 2, 3, 4]));
        }

        [Fact]
        public async Task RegisterSquadAsync_BelowMinimumPlayers_ThrowsBadRequestException()
        {
            _tournamentRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync(new TournamentEntity
                {
                    Status = TournamentStatus.Registration,
                    Format = MatchFormat.ElevenSide // minimum 11
                });

            _tournamentTeamRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentTeamEntity, bool>>>()))
                .ReturnsAsync(new TournamentTeamEntity
                {
                    Status = TournamentTeamStatus.Accepted
                });

            // Only 5 players for 11-a-side — below minimum
            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.RegisterSquadAsync(1, 1, [1, 2, 3, 4, 5]));
        }

        [Fact]
        public async Task RegisterSquadAsync_ExceedsMaximumPlayers_ThrowsBadRequestException()
        {
            _tournamentRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync(new TournamentEntity
                {
                    Status = TournamentStatus.Registration,
                    Format = MatchFormat.FiveSide // max 10
                });

            _tournamentTeamRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentTeamEntity, bool>>>()))
                .ReturnsAsync(new TournamentTeamEntity
                {
                    Status = TournamentTeamStatus.Accepted
                });

            // 11 players for 5-a-side — exceeds maximum of 10
            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.RegisterSquadAsync(1, 1,
                    [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11]));
        }

        [Fact]
        public async Task RegisterSquadAsync_PlayerNotInTeam_ThrowsBadRequestException()
        {
            _tournamentRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync(new TournamentEntity
                {
                    Status = TournamentStatus.Registration,
                    Format = MatchFormat.FiveSide
                });

            _tournamentTeamRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentTeamEntity, bool>>>()))
                .ReturnsAsync(new TournamentTeamEntity
                {
                    Status = TournamentTeamStatus.Accepted
                });

            // Return false — no players belong to this team
            _playerTeamRepoMock
                .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<PlayerTeamEntity, bool>>>()))
                .ReturnsAsync(false);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.RegisterSquadAsync(1, 1, [1, 2, 3, 4, 5]));
        }

        [Fact]
        public async Task RegisterSquadAsync_PlayerAlreadyRegistered_ThrowsConflictException()
        {
            _tournamentRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync(new TournamentEntity
                {
                    Status = TournamentStatus.Registration,
                    Format = MatchFormat.FiveSide
                });

            _tournamentTeamRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentTeamEntity, bool>>>()))
                .ReturnsAsync(new TournamentTeamEntity
                {
                    Status = TournamentTeamStatus.Accepted
                });

            // All 5 players belong to the team
            _playerTeamRepoMock
                .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<PlayerTeamEntity, bool>>>()))
                .ReturnsAsync(true);

            // Player 1 already registered in tournament
            // First call (player 1) returns true — already registered
            _tournamentSquadRepoMock
                .SetupSequence(r => r.ExistsAsync(It.IsAny<Expression<Func<TournamentSquadEntity, bool>>>()))
                .ReturnsAsync(true);

            await Assert.ThrowsAsync<ConflictException>(() =>
                _service.RegisterSquadAsync(1, 1, [1, 2, 3, 4, 5]));
        }
        [Fact]
        public async Task RegisterSquadAsync_TeamNotAccepted_ThrowsBadRequestException()
        {
            _tournamentRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync(new TournamentEntity
                {
                    Status = TournamentStatus.Registration,
                    Format = MatchFormat.FiveSide
                });

            _tournamentTeamRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentTeamEntity, bool>>>()))
                .ReturnsAsync((TournamentTeamEntity?)null);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.RegisterSquadAsync(1, 1, [1, 2, 3, 4, 5]));
        }
    }
}