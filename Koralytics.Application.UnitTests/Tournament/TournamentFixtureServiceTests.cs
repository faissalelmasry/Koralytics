using System.Linq.Expressions;
using Koralytics.Application.Interfaces;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable;
using MockQueryable.Moq;
using Xunit;
using TournamentEntity = Koralytics.Domain.Entities.Tournamet.Tournament;
using TournamentRoundEntity = Koralytics.Domain.Entities.Tournamet.TournamentRound;
using TournamentFixtureEntity = Koralytics.Domain.Entities.Tournamet.TournamentFixture;
using TournamentStandingEntity = Koralytics.Domain.Entities.Tournamet.TournamentStanding;
using MatchEntity = Koralytics.Domain.Entities.Match.Match;
using Koralytics.Application.Services.Tournaments;

namespace Koralytics.Application.UnitTests.Tournament
{
    public class TournamentFixtureServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<TournamentFixtureService>> _loggerMock;
        private readonly Mock<IRepository<MatchEntity>> _matchRepoMock;
        private readonly Mock<IRepository<TournamentFixtureEntity>> _fixtureRepoMock;
        private readonly Mock<IRepository<TournamentStandingEntity>> _standingRepoMock;
        private readonly Mock<IRepository<TournamentEntity>> _tournamentRepoMock;
        private readonly Mock<IRepository<TournamentRoundEntity>> _roundRepoMock;
        private readonly TournamentFixtureService _service;

        public TournamentFixtureServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<TournamentFixtureService>>();
            _matchRepoMock = new Mock<IRepository<MatchEntity>>();
            _fixtureRepoMock = new Mock<IRepository<TournamentFixtureEntity>>();
            _standingRepoMock = new Mock<IRepository<TournamentStandingEntity>>();
            _tournamentRepoMock = new Mock<IRepository<TournamentEntity>>();
            _roundRepoMock = new Mock<IRepository<TournamentRoundEntity>>();

            _unitOfWorkMock
                .Setup(u => u.Repository<MatchEntity>())
                .Returns(_matchRepoMock.Object);
            _unitOfWorkMock
                .Setup(u => u.Repository<TournamentFixtureEntity>())
                .Returns(_fixtureRepoMock.Object);
            _unitOfWorkMock
                .Setup(u => u.Repository<TournamentStandingEntity>())
                .Returns(_standingRepoMock.Object);
            _unitOfWorkMock
                .Setup(u => u.Repository<TournamentEntity>())
                .Returns(_tournamentRepoMock.Object);
            _unitOfWorkMock
                .Setup(u => u.Repository<TournamentRoundEntity>())
                .Returns(_roundRepoMock.Object);

            _service = new TournamentFixtureService(
                _unitOfWorkMock.Object,
                _loggerMock.Object);
        }

        // ─── UpdateStandingsAsync ────────────────────────────────────

        [Fact]
        public async Task UpdateStandingsAsync_AlreadyUpdated_ThrowsConflictException()
        {
            _fixtureRepoMock
                .Setup(r => r.ExistsAsync(
                    It.IsAny<Expression<Func<TournamentFixtureEntity, bool>>>()))
                .ReturnsAsync(true);

            await Assert.ThrowsAsync<ConflictException>(() =>
                _service.UpdateStandingsAsync(1, 1));
        }

        [Fact]
        public async Task UpdateStandingsAsync_MatchNotFound_ThrowsNotFoundException()
        {
            _fixtureRepoMock
                .Setup(r => r.ExistsAsync(
                    It.IsAny<Expression<Func<TournamentFixtureEntity, bool>>>()))
                .ReturnsAsync(false);

            _matchRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .ReturnsAsync((MatchEntity?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.UpdateStandingsAsync(1, 1));
        }

        [Fact]
        public async Task UpdateStandingsAsync_MatchNotCompleted_ThrowsBadRequestException()
        {
            _fixtureRepoMock
                .Setup(r => r.ExistsAsync(
                    It.IsAny<Expression<Func<TournamentFixtureEntity, bool>>>()))
                .ReturnsAsync(false);

            _matchRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .ReturnsAsync(new MatchEntity { Status = MatchStatus.Live });

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.UpdateStandingsAsync(1, 1));
        }

        [Fact]
        public async Task UpdateStandingsAsync_FixtureNotFound_ThrowsNotFoundException()
        {
            _fixtureRepoMock
                .Setup(r => r.ExistsAsync(
                    It.IsAny<Expression<Func<TournamentFixtureEntity, bool>>>()))
                .ReturnsAsync(false);

            _matchRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .ReturnsAsync(new MatchEntity { Status = MatchStatus.Completed });

            _fixtureRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentFixtureEntity, bool>>>()))
                .ReturnsAsync((TournamentFixtureEntity?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.UpdateStandingsAsync(1, 1));
        }

        [Fact]
        public async Task UpdateStandingsAsync_HomeWin_UpdatesStandingsCorrectly()
        {
            var match = new MatchEntity
            {
                Id = 1,
                Status = MatchStatus.Completed,
                HomeScore = 3,
                AwayScore = 1,
                WinningTeamId = 10
            };

            var fixture = new TournamentFixtureEntity
            {
                MatchId = 1,
                GroupId = 1,
                HomeTeamId = 10,
                AwayTeamId = 20
            };

            var homeStanding = new TournamentStandingEntity
            {
                GroupId = 1,
                TournamentTeamId = 10,
                Played = 0,
                Won = 0,
                Drawn = 0,
                Lost = 0,
                GoalsFor = 0,
                GoalsAgainst = 0,
                Points = 0
            };

            var awayStanding = new TournamentStandingEntity
            {
                GroupId = 1,
                TournamentTeamId = 20,
                Played = 0,
                Won = 0,
                Drawn = 0,
                Lost = 0,
                GoalsFor = 0,
                GoalsAgainst = 0,
                Points = 0
            };

            _fixtureRepoMock
                .Setup(r => r.ExistsAsync(
                    It.IsAny<Expression<Func<TournamentFixtureEntity, bool>>>()))
                .ReturnsAsync(false);

            _matchRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .ReturnsAsync(match);

            _fixtureRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentFixtureEntity, bool>>>()))
                .ReturnsAsync(fixture);

            _standingRepoMock
                .SetupSequence(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentStandingEntity, bool>>>()))
                .ReturnsAsync(homeStanding)
                .ReturnsAsync(awayStanding);

            await _service.UpdateStandingsAsync(1, 1);

            Assert.Equal(1, homeStanding.Played);
            Assert.Equal(1, homeStanding.Won);
            Assert.Equal(3, homeStanding.Points);
            Assert.Equal(3, homeStanding.GoalsFor);
            Assert.Equal(1, homeStanding.GoalsAgainst);
            Assert.Equal(1, awayStanding.Played);
            Assert.Equal(1, awayStanding.Lost);
            Assert.Equal(0, awayStanding.Points);
            Assert.Equal(1, awayStanding.GoalsFor);
            Assert.Equal(3, awayStanding.GoalsAgainst);
        }

        [Fact]
        public async Task UpdateStandingsAsync_Draw_GivesBothTeamsOnePoint()
        {
            var match = new MatchEntity
            {
                Id = 1,
                Status = MatchStatus.Completed,
                HomeScore = 2,
                AwayScore = 2,
                WinningTeamId = null
            };

            var fixture = new TournamentFixtureEntity
            {
                MatchId = 1,
                GroupId = 1,
                HomeTeamId = 10,
                AwayTeamId = 20
            };

            var homeStanding = new TournamentStandingEntity
            {
                GroupId = 1,
                TournamentTeamId = 10,
                Played = 0,
                Won = 0,
                Drawn = 0,
                Lost = 0,
                GoalsFor = 0,
                GoalsAgainst = 0,
                Points = 0
            };

            var awayStanding = new TournamentStandingEntity
            {
                GroupId = 1,
                TournamentTeamId = 20,
                Played = 0,
                Won = 0,
                Drawn = 0,
                Lost = 0,
                GoalsFor = 0,
                GoalsAgainst = 0,
                Points = 0
            };

            _fixtureRepoMock
                .Setup(r => r.ExistsAsync(
                    It.IsAny<Expression<Func<TournamentFixtureEntity, bool>>>()))
                .ReturnsAsync(false);

            _matchRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .ReturnsAsync(match);

            _fixtureRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentFixtureEntity, bool>>>()))
                .ReturnsAsync(fixture);

            _standingRepoMock
                .SetupSequence(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentStandingEntity, bool>>>()))
                .ReturnsAsync(homeStanding)
                .ReturnsAsync(awayStanding);

            await _service.UpdateStandingsAsync(1, 1);

            Assert.Equal(1, homeStanding.Drawn);
            Assert.Equal(1, homeStanding.Points);
            Assert.Equal(1, awayStanding.Drawn);
            Assert.Equal(1, awayStanding.Points);
        }

        // ─── AdvanceKnockoutAsync ────────────────────────────────────

        [Fact]
        public async Task AdvanceKnockoutAsync_TournamentNotFound_ThrowsNotFoundException()
        {
            _tournamentRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync((TournamentEntity?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.AdvanceKnockoutAsync(1, 1));
        }

        [Fact]
        public async Task AdvanceKnockoutAsync_NotInProgress_ThrowsBadRequestException()
        {
            _tournamentRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync(new TournamentEntity
                {
                    Status = TournamentStatus.Registration
                });

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.AdvanceKnockoutAsync(1, 1));
        }

        [Fact]
        public async Task AdvanceKnockoutAsync_RoundNotFound_ThrowsNotFoundException()
        {
            _tournamentRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync(new TournamentEntity
                {
                    Status = TournamentStatus.InProgress
                });

            _roundRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentRoundEntity, bool>>>()))
                .ReturnsAsync((TournamentRoundEntity?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.AdvanceKnockoutAsync(1, 1));
        }

        [Fact]
        public async Task AdvanceKnockoutAsync_NextRoundAlreadyExists_ThrowsConflictException()
        {
            _tournamentRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync(new TournamentEntity
                {
                    Status = TournamentStatus.InProgress
                });

            _roundRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentRoundEntity, bool>>>()))
                .ReturnsAsync(new TournamentRoundEntity
                {
                    Id = 1,
                    RoundNumber = 1
                });

            _roundRepoMock
                .Setup(r => r.ExistsAsync(
                    It.IsAny<Expression<Func<TournamentRoundEntity, bool>>>()))
                .ReturnsAsync(true);

            await Assert.ThrowsAsync<ConflictException>(() =>
                _service.AdvanceKnockoutAsync(1, 1));
        }

        [Fact]
        public async Task AdvanceKnockoutAsync_IncompleteFixtures_ThrowsBadRequestException()
        {
            _tournamentRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentEntity, bool>>>()))
                .ReturnsAsync(new TournamentEntity
                {
                    Status = TournamentStatus.InProgress,
                    HasTwoLegs = false
                });

            _roundRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<TournamentRoundEntity, bool>>>()))
                .ReturnsAsync(new TournamentRoundEntity
                {
                    Id = 1,
                    RoundNumber = 1
                });

            _roundRepoMock
                .Setup(r => r.ExistsAsync(
                    It.IsAny<Expression<Func<TournamentRoundEntity, bool>>>()))
                .ReturnsAsync(false);

            // One live fixture — not all completed
            var fixtures = new List<TournamentFixtureEntity>
            {
                new() { Id = 1, RoundId = 1, Status = MatchStatus.Live,
                        WinnerTeamId = null },
                new() { Id = 2, RoundId = 1, Status = MatchStatus.Completed,
                        WinnerTeamId = 10 }
            };

            _fixtureRepoMock
                .Setup(r => r.GetQueryable())
                .Returns(fixtures.BuildMock());

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.AdvanceKnockoutAsync(1, 1));
        }
    }
}