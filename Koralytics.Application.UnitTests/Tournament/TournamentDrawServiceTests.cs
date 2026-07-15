using System.Linq.Expressions;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Services.Player.PlayerCardService;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable;
using MockQueryable.Moq;
using Xunit;
using TournamentEntity = Koralytics.Domain.Entities.Tournamet.Tournament;
using TournamentTeamEntity = Koralytics.Domain.Entities.Tournamet.TournamentTeam;
using TournamentRoundEntity = Koralytics.Domain.Entities.Tournamet.TournamentRound;
using TournamentGroupEntity = Koralytics.Domain.Entities.Tournamet.TournamentGroup;
using TournamentFixtureEntity = Koralytics.Domain.Entities.Tournamet.TournamentFixture;
using Koralytics.Application.Services.Tournaments;

namespace Koralytics.Application.UnitTests.Tournament
{
    public class TournamentDrawServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<TournamentDrawService>> _loggerMock;
        private readonly Mock<IPlayerCardService> _playerCardServiceMock;
        private readonly Mock<IRepository<TournamentEntity>> _tournamentRepoMock;
        private readonly Mock<IRepository<TournamentTeamEntity>> _tournamentTeamRepoMock;
        private readonly Mock<IRepository<TournamentRoundEntity>> _roundRepoMock;
        private readonly Mock<IRepository<TournamentGroupEntity>> _groupRepoMock;
        private readonly Mock<IRepository<TournamentFixtureEntity>> _fixtureRepoMock;
        private readonly TournamentDrawService _service;

        public TournamentDrawServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<TournamentDrawService>>();
            _playerCardServiceMock = new Mock<IPlayerCardService>();
            _tournamentRepoMock = new Mock<IRepository<TournamentEntity>>();
            _tournamentTeamRepoMock = new Mock<IRepository<TournamentTeamEntity>>();
            _roundRepoMock = new Mock<IRepository<TournamentRoundEntity>>();
            _groupRepoMock = new Mock<IRepository<TournamentGroupEntity>>();
            _fixtureRepoMock = new Mock<IRepository<TournamentFixtureEntity>>();

            _unitOfWorkMock
                .Setup(u => u.Repository<TournamentEntity>())
                .Returns(_tournamentRepoMock.Object);
            _unitOfWorkMock
                .Setup(u => u.Repository<TournamentTeamEntity>())
                .Returns(_tournamentTeamRepoMock.Object);
            _unitOfWorkMock
                .Setup(u => u.Repository<TournamentRoundEntity>())
                .Returns(_roundRepoMock.Object);
            _unitOfWorkMock
                .Setup(u => u.Repository<TournamentGroupEntity>())
                .Returns(_groupRepoMock.Object);
            _unitOfWorkMock
                .Setup(u => u.Repository<TournamentFixtureEntity>())
                .Returns(_fixtureRepoMock.Object);

            _service = new TournamentDrawService(
                _unitOfWorkMock.Object,
                _loggerMock.Object,
                _playerCardServiceMock.Object);
        }

        // ─── GenerateSeedingAsync ────────────────────────────────────

        [Fact]
        public async Task GenerateSeedingAsync_TournamentNotFound_ThrowsNotFoundException()
        {
            _tournamentRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync((TournamentEntity?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.GenerateSeedingAsync(1));
        }

        [Fact]
        public async Task GenerateSeedingAsync_NotInRegistration_ThrowsBadRequestException()
        {
            _tournamentRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync(new TournamentEntity
                {
                    Status = TournamentStatus.InProgress
                });

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.GenerateSeedingAsync(1));
        }

        [Fact]
        public async Task GenerateSeedingAsync_LessThanTwoTeams_ThrowsBadRequestException()
        {
            _tournamentRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync(new TournamentEntity
                {
                    Status = TournamentStatus.Registration
                });

            _tournamentTeamRepoMock
                .Setup(r => r.GetQueryable())
                .Returns(new List<TournamentTeamEntity>
                {
                    new() { Status = TournamentTeamStatus.Accepted }
                }.BuildMock());

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.GenerateSeedingAsync(1));
        }

        // ─── GenerateDrawAsync ───────────────────────────────────────

        [Fact]
        public async Task GenerateDrawAsync_TournamentNotFound_ThrowsNotFoundException()
        {
            _tournamentRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync((TournamentEntity?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.GenerateDrawAsync(1));
        }

        [Fact]
        public async Task GenerateDrawAsync_NotInRegistration_ThrowsBadRequestException()
        {
            _tournamentRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync(new TournamentEntity
                {
                    Status = TournamentStatus.InProgress
                });

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.GenerateDrawAsync(1));
        }

        [Fact]
        public async Task GenerateDrawAsync_DrawAlreadyExists_ThrowsConflictException()
        {
            _tournamentRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync(new TournamentEntity
                {
                    Status = TournamentStatus.Registration
                });

            // Round already exists
            _roundRepoMock
                .Setup(r => r.ExistsAsync(
                    It.IsAny<Expression<Func<TournamentRoundEntity, bool>>>()))
                .ReturnsAsync(true);

            await Assert.ThrowsAsync<ConflictException>(() =>
                _service.GenerateDrawAsync(1));
        }

        [Fact]
        public async Task GenerateDrawAsync_TeamsNotSeeded_ThrowsBadRequestException()
        {
            _tournamentRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync(new TournamentEntity
                {
                    Status = TournamentStatus.Registration,
                    Structure = TournamentStructure.Knockout
                });

            _roundRepoMock
                .Setup(r => r.ExistsAsync(
                    It.IsAny<Expression<Func<TournamentRoundEntity, bool>>>()))
                .ReturnsAsync(false);

            _groupRepoMock
                .Setup(r => r.ExistsAsync(
                    It.IsAny<Expression<Func<TournamentGroupEntity, bool>>>()))
                .ReturnsAsync(false);

            // Teams have no seed number
            _tournamentTeamRepoMock
                .Setup(r => r.GetQueryable())
                .Returns(new List<TournamentTeamEntity>
                {
                    new() { Status = TournamentTeamStatus.Accepted, SeedNumber = null },
                    new() { Status = TournamentTeamStatus.Accepted, SeedNumber = null }
                }.BuildMock());

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.GenerateDrawAsync(1));
        }

        [Fact]
        public async Task GenerateDrawAsync_KnockoutNonPowerOfTwo_ThrowsBadRequestException()
        {
            _tournamentRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync(new TournamentEntity
                {
                    Status = TournamentStatus.Registration,
                    Structure = TournamentStructure.Knockout
                });

            _roundRepoMock
                .Setup(r => r.ExistsAsync(
                    It.IsAny<Expression<Func<TournamentRoundEntity, bool>>>()))
                .ReturnsAsync(false);

            _groupRepoMock
                .Setup(r => r.ExistsAsync(
                    It.IsAny<Expression<Func<TournamentGroupEntity, bool>>>()))
                .ReturnsAsync(false);

            // 3 teams — not power of 2
            _tournamentTeamRepoMock
                .Setup(r => r.GetQueryable())
                .Returns(new List<TournamentTeamEntity>
                {
                    new() { Status = TournamentTeamStatus.Accepted, SeedNumber = 1 },
                    new() { Status = TournamentTeamStatus.Accepted, SeedNumber = 2 },
                    new() { Status = TournamentTeamStatus.Accepted, SeedNumber = 3 }
                }.BuildMock());

            _unitOfWorkMock
                .Setup(u => u.BeginTransactionAsync())
                .ReturnsAsync(new Mock<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>().Object);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.GenerateDrawAsync(1));
        }

        [Fact]
        public async Task GenerateDrawAsync_LessThanTwoTeams_ThrowsBadRequestException()
        {
            _tournamentRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync(new TournamentEntity
                {
                    Status = TournamentStatus.Registration,
                    Structure = TournamentStructure.Knockout
                });

            _roundRepoMock
                .Setup(r => r.ExistsAsync(
                    It.IsAny<Expression<Func<TournamentRoundEntity, bool>>>()))
                .ReturnsAsync(false);

            _groupRepoMock
                .Setup(r => r.ExistsAsync(
                    It.IsAny<Expression<Func<TournamentGroupEntity, bool>>>()))
                .ReturnsAsync(false);

            // Only 1 team
            _tournamentTeamRepoMock
                .Setup(r => r.GetQueryable())
                .Returns(new List<TournamentTeamEntity>
                {
                    new() { Status = TournamentTeamStatus.Accepted, SeedNumber = 1 }
                }.BuildMock());

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.GenerateDrawAsync(1));
        }
    }
}