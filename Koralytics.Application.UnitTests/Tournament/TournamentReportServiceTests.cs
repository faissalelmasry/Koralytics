using System.Linq.Expressions;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Services.Tournament;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using TournamentEntity = Koralytics.Domain.Entities.Tournamet.Tournament;
using TournamentFixtureEntity = Koralytics.Domain.Entities.Tournamet.TournamentFixture;
using TournamentGroupEntity = Koralytics.Domain.Entities.Tournamet.TournamentGroup;
using TournamentRoundEntity = Koralytics.Domain.Entities.Tournamet.TournamentRound;
using TournamentStandingEntity = Koralytics.Domain.Entities.Tournamet.TournamentStanding;

namespace Koralytics.Application.UnitTests.Tournament
{
    public class TournamentReportServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<TournamentReportService>> _loggerMock;
        private readonly Mock<IRepository<TournamentEntity>> _tournamentRepoMock;
        private readonly Mock<IRepository<TournamentFixtureEntity>> _fixtureRepoMock;
        private readonly Mock<IRepository<TournamentGroupEntity>> _groupRepoMock;
        private readonly Mock<IRepository<TournamentRoundEntity>> _roundRepoMock;
        private readonly TournamentReportService _service;

        public TournamentReportServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<TournamentReportService>>();
            _tournamentRepoMock = new Mock<IRepository<TournamentEntity>>();
            _fixtureRepoMock = new Mock<IRepository<TournamentFixtureEntity>>();
            _groupRepoMock = new Mock<IRepository<TournamentGroupEntity>>();
            _roundRepoMock = new Mock<IRepository<TournamentRoundEntity>>();

            _unitOfWorkMock
                .Setup(u => u.Repository<TournamentEntity>())
                .Returns(_tournamentRepoMock.Object);
            _unitOfWorkMock
                .Setup(u => u.Repository<TournamentFixtureEntity>())
                .Returns(_fixtureRepoMock.Object);
            _unitOfWorkMock
                .Setup(u => u.Repository<TournamentGroupEntity>())
                .Returns(_groupRepoMock.Object);
            _unitOfWorkMock
                .Setup(u => u.Repository<TournamentRoundEntity>())
                .Returns(_roundRepoMock.Object);

            _service = new TournamentReportService(
                _unitOfWorkMock.Object,
                _loggerMock.Object);
        }

        // ─── CompleteTournamentAsync ─────────────────────────────────

        [Fact]
        public async Task CompleteTournamentAsync_TournamentNotFound_ThrowsNotFoundException()
        {
            _tournamentRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync((TournamentEntity?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.CompleteTournamentAsync(1));
        }

        [Fact]
        public async Task CompleteTournamentAsync_AlreadyCompleted_ThrowsConflictException()
        {
            _tournamentRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync(new TournamentEntity
                {
                    Status = TournamentStatus.Completed
                });

            await Assert.ThrowsAsync<ConflictException>(() =>
                _service.CompleteTournamentAsync(1));
        }

        [Fact]
        public async Task CompleteTournamentAsync_NotInProgress_ThrowsBadRequestException()
        {
            _tournamentRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync(new TournamentEntity
                {
                    Status = TournamentStatus.Registration
                });

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.CompleteTournamentAsync(1));
        }

        [Fact]
        public async Task CompleteTournamentAsync_HasIncompleteFixtures_ThrowsBadRequestException()
        {
            _tournamentRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync(new TournamentEntity
                {
                    Status = TournamentStatus.InProgress
                });

            _fixtureRepoMock
                .Setup(r => r.ExistsAsync(
                    It.IsAny<Expression<Func<TournamentFixtureEntity, bool>>>()))
                .ReturnsAsync(true);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.CompleteTournamentAsync(1));
        }

        // ─── GetBracketAsync ─────────────────────────────────────────

        [Fact]
        public async Task GetBracketAsync_TournamentNotFound_ThrowsNotFoundException()
        {
            _tournamentRepoMock
                .Setup(r => r.GetQueryable())
                .Returns(new List<TournamentEntity>().AsQueryable());

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.GetBracketAsync(1));
        }

        [Fact]
        public async Task GetBracketAsync_ValidTournament_ReturnsCorrectIds()
        {
            var tournament = new TournamentEntity
            {
                Id = 1,
                Name = "Summer Cup",
                Status = TournamentStatus.InProgress
            };

            _tournamentRepoMock
                .Setup(r => r.GetQueryable())
                .Returns(new List<TournamentEntity>
                {
                    tournament
                }.AsQueryable());

            _groupRepoMock
                .Setup(r => r.GetQueryable())
                .Returns(new List<TournamentGroupEntity>().AsQueryable());

            _roundRepoMock
                .Setup(r => r.GetQueryable())
                .Returns(new List<TournamentRoundEntity>().AsQueryable());

            var result = await _service.GetBracketAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.TournamentId);
            Assert.Equal("Summer Cup", result.TournamentName);
            Assert.Equal(TournamentStatus.InProgress, result.Status);
            Assert.Empty(result.Groups);
            Assert.Empty(result.Rounds);
        }

        [Fact]
        public async Task GetBracketAsync_ValidTournament_ReturnsEmptyGroupsAndRoundsWhenNoneExist()
        {
            var tournament = new TournamentEntity
            {
                Id = 5,
                Name = "Winter League",
                Status = TournamentStatus.Draft
            };

            _tournamentRepoMock
                .Setup(r => r.GetQueryable())
                .Returns(new List<TournamentEntity>
                {
                    tournament
                }.AsQueryable());

            _groupRepoMock
                .Setup(r => r.GetQueryable())
                .Returns(new List<TournamentGroupEntity>().AsQueryable());

            _roundRepoMock
                .Setup(r => r.GetQueryable())
                .Returns(new List<TournamentRoundEntity>().AsQueryable());

            var result = await _service.GetBracketAsync(5);

            Assert.Empty(result.Groups);
            Assert.Empty(result.Rounds);
        }
    }
}