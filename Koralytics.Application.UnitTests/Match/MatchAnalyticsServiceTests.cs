using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Koralytics.Application.DTOs.Match;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Match;
using Koralytics.Application.Services.Match;
using Koralytics.Domain.Entities.Academy;
using DomainEnums = Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using MatchEntity = Koralytics.Domain.Entities.Match.Match;

namespace Koralytics.Application.UnitTests.Match
{
    public class MatchAnalyticsServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<MatchAnalyticsService>> _loggerMock;
        private readonly MatchAnalyticsService _service;

        public MatchAnalyticsServiceTests()
        {
            _unitOfWorkMock = new();
            _loggerMock = new();
            _service = new MatchAnalyticsService(_unitOfWorkMock.Object, _loggerMock.Object);
        }

        private void SetupRepository<T>(Mock<IRepository<T>> repo) where T : class, Koralytics.Domain.Interfaces.ISoftDelete
        {
            _unitOfWorkMock.Setup(u => u.Repository<T>()).Returns(repo.Object);
        }

        #region GetHeadToHeadAsync

        [Fact]
        public async Task GetHeadToHeadAsync_TeamANotFound_ThrowsNotFoundException()
        {
            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync((Team?)null);
            SetupRepository(teamRepo);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.GetHeadToHeadAsync(1, 2));
        }

        [Fact]
        public async Task GetHeadToHeadAsync_TeamBNotFound_ThrowsNotFoundException()
        {
            var teamA = new Team { Id = 1, Name = "Team A" };
            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.Is<Expression<Func<Team, bool>>>(e =>
                e.Compile()(teamA))))
                .ReturnsAsync(teamA);
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.Is<Expression<Func<Team, bool>>>(e =>
                !e.Compile()(teamA))))
                .ReturnsAsync((Team?)null);
            SetupRepository(teamRepo);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.GetHeadToHeadAsync(1, 2));
        }

        [Fact]
        public async Task GetHeadToHeadAsync_NoMatches_ReturnsZeroStats()
        {
            var teamA = new Team { Id = 1, Name = "Team A" };
            var teamB = new Team { Id = 2, Name = "Team B" };

            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync((Team?)null);
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.Is<Expression<Func<Team, bool>>>(e =>
                e.Compile()(teamA))))
                .ReturnsAsync(teamA);
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.Is<Expression<Func<Team, bool>>>(e =>
                e.Compile()(teamB))))
                .ReturnsAsync(teamB);
            SetupRepository(teamRepo);

            var matches = new List<MatchEntity>();
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            SetupRepository(matchRepo);

            var result = await _service.GetHeadToHeadAsync(1, 2);

            Assert.NotNull(result);
            Assert.Equal(1, result.TeamAId);
            Assert.Equal(2, result.TeamBId);
            Assert.Equal("Team A", result.TeamAName);
            Assert.Equal("Team B", result.TeamBName);
            Assert.Equal(0, result.TotalMatches);
            Assert.Equal(0, result.TeamAWins);
            Assert.Equal(0, result.TeamBWins);
            Assert.Equal(0, result.Draws);
            Assert.Empty(result.Matches);
        }

        [Fact]
        public async Task GetHeadToHeadAsync_WithMatches_ReturnsCorrectStats()
        {
            var teamA = new Team { Id = 1, Name = "Team A" };
            var teamB = new Team { Id = 2, Name = "Team B" };
            var homeTeam = new Team { Id = 1, Name = "Team A" };
            var awayTeam = new Team { Id = 2, Name = "Team B" };

            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync((Team?)null);
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.Is<Expression<Func<Team, bool>>>(e =>
                e.Compile()(teamA))))
                .ReturnsAsync(teamA);
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.Is<Expression<Func<Team, bool>>>(e =>
                e.Compile()(teamB))))
                .ReturnsAsync(teamB);
            SetupRepository(teamRepo);

            var matches = new List<MatchEntity>
            {
                new MatchEntity { Id = 1, HomeTeamId = 1, AwayTeamId = 2, HomeScore = 2, AwayScore = 1,
                    Status = DomainEnums.MatchStatus.Completed, Type = DomainEnums.MatchType.Friendly, MatchDate = DateTime.UtcNow.AddDays(-1),
                    HomeTeam = homeTeam, AwayTeam = awayTeam },
                new MatchEntity { Id = 2, HomeTeamId = 1, AwayTeamId = 2, HomeScore = 1, AwayScore = 1,
                    Status = DomainEnums.MatchStatus.Completed, Type = DomainEnums.MatchType.Friendly, MatchDate = DateTime.UtcNow.AddDays(-2),
                    HomeTeam = homeTeam, AwayTeam = awayTeam },
                new MatchEntity { Id = 3, HomeTeamId = 2, AwayTeamId = 1, HomeScore = 3, AwayScore = 0,
                    Status = DomainEnums.MatchStatus.Completed, Type = DomainEnums.MatchType.Friendly, MatchDate = DateTime.UtcNow.AddDays(-3),
                    HomeTeam = awayTeam, AwayTeam = homeTeam }
            };
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            SetupRepository(matchRepo);

            var result = await _service.GetHeadToHeadAsync(1, 2);

            Assert.Equal(3, result.TotalMatches);
            Assert.Equal(1, result.TeamAWins);
            Assert.Equal(1, result.TeamBWins);
            Assert.Equal(1, result.Draws);
            Assert.Equal(3, result.Matches.Count);
        }

        [Fact]
        public async Task GetHeadToHeadAsync_WithPenaltyShootoutResult_HandlesCorrectly()
        {
            var teamA = new Team { Id = 1, Name = "Team A" };
            var teamB = new Team { Id = 2, Name = "Team B" };

            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync((Team?)null);
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.Is<Expression<Func<Team, bool>>>(e =>
                e.Compile()(teamA))))
                .ReturnsAsync(teamA);
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.Is<Expression<Func<Team, bool>>>(e =>
                e.Compile()(teamB))))
                .ReturnsAsync(teamB);
            SetupRepository(teamRepo);

            var matches = new List<MatchEntity>
            {
                new MatchEntity
                {
                    Id = 1, HomeTeamId = 1, AwayTeamId = 2, HomeScore = 1, AwayScore = 1,
                    HomePenaltyScore = 5, AwayPenaltyScore = 4,
                    Status = DomainEnums.MatchStatus.Completed, Type = DomainEnums.MatchType.Tournament, MatchDate = DateTime.UtcNow,
                    HomeTeam = teamA, AwayTeam = teamB
                }
            };
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            SetupRepository(matchRepo);

            var result = await _service.GetHeadToHeadAsync(1, 2);

            Assert.Equal(1, result.TeamAWins);
            Assert.Equal(0, result.TeamBWins);
            Assert.Equal(0, result.Draws);
        }

        [Fact]
        public async Task GetHeadToHeadAsync_ExcludesSessionMatches()
        {
            var teamA = new Team { Id = 1, Name = "Team A" };
            var teamB = new Team { Id = 2, Name = "Team B" };

            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync((Team?)null);
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.Is<Expression<Func<Team, bool>>>(e =>
                e.Compile()(teamA))))
                .ReturnsAsync(teamA);
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.Is<Expression<Func<Team, bool>>>(e =>
                e.Compile()(teamB))))
                .ReturnsAsync(teamB);
            SetupRepository(teamRepo);

            var matches = new List<MatchEntity>
            {
                new MatchEntity { Id = 1, HomeTeamId = 1, AwayTeamId = 2, HomeScore = 3, AwayScore = 0,
                    Status = DomainEnums.MatchStatus.Completed, Type = DomainEnums.MatchType.Session, MatchDate = DateTime.UtcNow,
                    HomeTeam = teamA, AwayTeam = teamB }
            };
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            SetupRepository(matchRepo);

            var result = await _service.GetHeadToHeadAsync(1, 2);

            Assert.Equal(0, result.TotalMatches);
        }

        #endregion

        #region GetPostMatchAnalysisAsync

        [Fact]
        public async Task GetPostMatchAnalysisAsync_TeamNotFound_ThrowsNotFoundException()
        {
            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync((Team?)null);
            SetupRepository(teamRepo);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.GetPostMatchAnalysisAsync(1));
        }

        [Fact]
        public async Task GetPostMatchAnalysisAsync_NoMatches_ReturnsZeroStats()
        {
            var team = new Team { Id = 1, Name = "Reds" };
            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync(team);
            SetupRepository(teamRepo);

            var matches = new List<MatchEntity>();
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            SetupRepository(matchRepo);

            var result = await _service.GetPostMatchAnalysisAsync(1);

            Assert.Equal(1, result.TeamId);
            Assert.Equal("Reds", result.TeamName);
            Assert.Equal(0, result.Wins);
            Assert.Equal(0, result.Losses);
            Assert.Equal(0, result.Draws);
            Assert.Equal(0, result.GoalsFor);
            Assert.Equal(0, result.GoalsAgainst);
            Assert.Empty(result.RecentMatches);
        }

        [Fact]
        public async Task GetPostMatchAnalysisAsync_WithMatches_ReturnsCorrectAnalysis()
        {
            var team = new Team { Id = 1, Name = "Reds" };
            var opponentHome = new Team { Id = 2, Name = "Blues" };
            var opponentAway = new Team { Id = 3, Name = "Greens" };

            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync(team);
            SetupRepository(teamRepo);

            var matches = new List<MatchEntity>
            {
                new MatchEntity { Id = 1, HomeTeamId = 1, AwayTeamId = 2, HomeScore = 2, AwayScore = 0,
                    Status = DomainEnums.MatchStatus.Completed, Type = DomainEnums.MatchType.Friendly,
                    MatchDate = DateTime.UtcNow.AddDays(-1), HomeTeam = team, AwayTeam = opponentHome },
                new MatchEntity { Id = 2, HomeTeamId = 3, AwayTeamId = 1, HomeScore = 1, AwayScore = 1,
                    Status = DomainEnums.MatchStatus.Completed, Type = DomainEnums.MatchType.Friendly,
                    MatchDate = DateTime.UtcNow.AddDays(-2), HomeTeam = opponentAway, AwayTeam = team },
                new MatchEntity { Id = 3, HomeTeamId = 1, AwayTeamId = 2, HomeScore = 0, AwayScore = 2,
                    Status = DomainEnums.MatchStatus.Completed, Type = DomainEnums.MatchType.Friendly,
                    MatchDate = DateTime.UtcNow.AddDays(-3), HomeTeam = team, AwayTeam = opponentHome }
            };
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            SetupRepository(matchRepo);

            var result = await _service.GetPostMatchAnalysisAsync(1);

            Assert.Equal(1, result.Wins);
            Assert.Equal(1, result.Losses);
            Assert.Equal(1, result.Draws);
            Assert.Equal(3, result.GoalsFor);
            Assert.Equal(3, result.GoalsAgainst);
            Assert.Equal(3, result.RecentMatches.Count);
            Assert.Equal("W", result.RecentMatches[0].Result);
            Assert.Equal("D", result.RecentMatches[1].Result);
            Assert.Equal("L", result.RecentMatches[2].Result);
        }

        [Fact]
        public async Task GetPostMatchAnalysisAsync_OnlyTakesLastTenMatches()
        {
            var team = new Team { Id = 1, Name = "Reds" };
            var opponent = new Team { Id = 2, Name = "Blues" };

            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync(team);
            SetupRepository(teamRepo);

            var matches = new List<MatchEntity>();
            for (int i = 1; i <= 15; i++)
            {
                matches.Add(new MatchEntity
                {
                    Id = i, HomeTeamId = 1, AwayTeamId = 2, HomeScore = 1, AwayScore = 0,
                    Status = DomainEnums.MatchStatus.Completed, Type = DomainEnums.MatchType.Friendly,
                    MatchDate = DateTime.UtcNow.AddDays(-i), HomeTeam = team, AwayTeam = opponent
                });
            }
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            SetupRepository(matchRepo);

            var result = await _service.GetPostMatchAnalysisAsync(1);

            Assert.Equal(10, result.RecentMatches.Count);
        }

        [Fact]
        public async Task GetPostMatchAnalysisAsync_WithPenaltyShootoutWin_CountsCorrectly()
        {
            var team = new Team { Id = 1, Name = "Reds" };
            var opponent = new Team { Id = 2, Name = "Blues" };

            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync(team);
            SetupRepository(teamRepo);

            var matches = new List<MatchEntity>
            {
                new MatchEntity { Id = 1, HomeTeamId = 1, AwayTeamId = 2, HomeScore = 1, AwayScore = 1,
                    HomePenaltyScore = 4, AwayPenaltyScore = 3,
                    Status = DomainEnums.MatchStatus.Completed, Type = DomainEnums.MatchType.Tournament,
                    MatchDate = DateTime.UtcNow, HomeTeam = team, AwayTeam = opponent }
            };
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            SetupRepository(matchRepo);

            var result = await _service.GetPostMatchAnalysisAsync(1);

            Assert.Equal(1, result.Wins);
            Assert.Equal(0, result.Losses);
            Assert.Equal(0, result.Draws);
            Assert.Equal("W", result.RecentMatches[0].Result);
        }

        [Fact]
        public async Task GetPostMatchAnalysisAsync_AwayPenaltyShootoutWin_CountsCorrectly()
        {
            var team = new Team { Id = 1, Name = "Reds" };
            var opponent = new Team { Id = 2, Name = "Blues" };

            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync(team);
            SetupRepository(teamRepo);

            var matches = new List<MatchEntity>
            {
                new MatchEntity { Id = 1, HomeTeamId = 2, AwayTeamId = 1, HomeScore = 2, AwayScore = 2,
                    HomePenaltyScore = 3, AwayPenaltyScore = 5,
                    Status = DomainEnums.MatchStatus.Completed, Type = DomainEnums.MatchType.Tournament,
                    MatchDate = DateTime.UtcNow, HomeTeam = opponent, AwayTeam = team }
            };
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            SetupRepository(matchRepo);

            var result = await _service.GetPostMatchAnalysisAsync(1);

            Assert.Equal(1, result.Wins);
            Assert.Equal(0, result.Losses);
            Assert.Equal("W", result.RecentMatches[0].Result);
        }

        #endregion

        #region GetPlayerReadinessAsync

        [Fact]
        public async Task GetPlayerReadinessAsync_PlayerNotFound_ThrowsNotFoundException()
        {
            var playerRepo = new Mock<IRepository<Koralytics.Domain.Entities.Player.Player>>();
            playerRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Koralytics.Domain.Entities.Player.Player, bool>>>()))
                .ReturnsAsync((Koralytics.Domain.Entities.Player.Player?)null);
            SetupRepository(playerRepo);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.GetPlayerReadinessAsync(1));
        }

        [Fact]
        public async Task GetPlayerReadinessAsync_InjuredPlayer_ReturnsZeroScore()
        {
            var player = new Koralytics.Domain.Entities.Player.Player { Id = 1, FirstName = "Test", LastName = "Player", AvailabilityStatus = DomainEnums.AvailabilityStatus.Injured };
            var playerRepo = new Mock<IRepository<Koralytics.Domain.Entities.Player.Player>>();
            playerRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Koralytics.Domain.Entities.Player.Player, bool>>>()))
                .ReturnsAsync(player);
            SetupRepository(playerRepo);

            var lineups = new List<Koralytics.Domain.Entities.Match.MatchLineup>();
            var queryable = lineups.BuildMock();
            var lineupRepo = new Mock<IRepository<Koralytics.Domain.Entities.Match.MatchLineup>>();
            lineupRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            SetupRepository(lineupRepo);

            var result = await _service.GetPlayerReadinessAsync(1);

            Assert.Equal(0, result.ReadinessScore);
            Assert.Equal("Injured", result.Status);
        }

        [Fact]
        public async Task GetPlayerReadinessAsync_AvailablePlayer_NoRecentMatches_ReturnsFullyRested()
        {
            var player = new Koralytics.Domain.Entities.Player.Player { Id = 1, FirstName = "Test", LastName = "Player", AvailabilityStatus = DomainEnums.AvailabilityStatus.Available };
            var playerRepo = new Mock<IRepository<Koralytics.Domain.Entities.Player.Player>>();
            playerRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Koralytics.Domain.Entities.Player.Player, bool>>>()))
                .ReturnsAsync(player);
            SetupRepository(playerRepo);

            var lineups = new List<Koralytics.Domain.Entities.Match.MatchLineup>();
            var queryable = lineups.BuildMock();
            var lineupRepo = new Mock<IRepository<Koralytics.Domain.Entities.Match.MatchLineup>>();
            lineupRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            SetupRepository(lineupRepo);

            var result = await _service.GetPlayerReadinessAsync(1);

            Assert.Equal(100, result.ReadinessScore);
            Assert.Equal("Fully Rested", result.Status);
            Assert.Equal(0, result.MatchesPlayedLast7Days);
        }

        [Fact]
        public async Task GetPlayerReadinessAsync_AvailablePlayer_ManyRecentMatches_ReturnsHighlyFatigued()
        {
            var player = new Koralytics.Domain.Entities.Player.Player { Id = 1, FirstName = "Test", LastName = "Player", AvailabilityStatus = DomainEnums.AvailabilityStatus.Available };
            var playerRepo = new Mock<IRepository<Koralytics.Domain.Entities.Player.Player>>();
            playerRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Koralytics.Domain.Entities.Player.Player, bool>>>()))
                .ReturnsAsync(player);
            SetupRepository(playerRepo);

            var lineups = new List<Koralytics.Domain.Entities.Match.MatchLineup>
            {
                new Koralytics.Domain.Entities.Match.MatchLineup { PlayerId = 1, Match = new MatchEntity { MatchDate = DateTime.UtcNow.AddDays(-1), Status = DomainEnums.MatchStatus.Completed } },
                new Koralytics.Domain.Entities.Match.MatchLineup { PlayerId = 1, Match = new MatchEntity { MatchDate = DateTime.UtcNow.AddDays(-2), Status = DomainEnums.MatchStatus.Completed } },
                new Koralytics.Domain.Entities.Match.MatchLineup { PlayerId = 1, Match = new MatchEntity { MatchDate = DateTime.UtcNow.AddDays(-3), Status = DomainEnums.MatchStatus.Completed } }
            };
            var queryable = lineups.BuildMock();
            var lineupRepo = new Mock<IRepository<Koralytics.Domain.Entities.Match.MatchLineup>>();
            lineupRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            SetupRepository(lineupRepo);

            var result = await _service.GetPlayerReadinessAsync(1);

            Assert.Equal(30, result.ReadinessScore);
            Assert.Equal("Highly Fatigued", result.Status);
            Assert.Equal(3, result.MatchesPlayedLast7Days);
        }

        #endregion
    }
}
