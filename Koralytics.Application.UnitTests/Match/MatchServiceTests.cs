using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using Koralytics.Application.DTOs.Match;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Match;
using Koralytics.Application.Services.Match;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Drill;
using Koralytics.Domain.Entities.Tournamet;
using Koralytics.Domain.Exceptions;
using DomainEnums = Koralytics.Domain.Enums;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using MatchEntity = Koralytics.Domain.Entities.Match.Match;
using MatchLineupEntity = Koralytics.Domain.Entities.Match.MatchLineup;

namespace Koralytics.Application.UnitTests.Match
{
    public class MatchServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<MatchService>> _loggerMock;
        private readonly MatchService _service;

        public MatchServiceTests()
        {
            _unitOfWorkMock = new();
            _mapperMock = new();
            _loggerMock = new();
            _service = new MatchService(_unitOfWorkMock.Object, _mapperMock.Object, _loggerMock.Object);
        }

        private void SetupRepository<T>(Mock<IRepository<T>> repo) where T : class, Koralytics.Domain.Interfaces.ISoftDelete
        {
            _unitOfWorkMock.Setup(u => u.Repository<T>()).Returns(repo.Object);
        }

        #region CreateFriendlyMatchAsync

        [Fact]
        public async Task CreateFriendlyMatchAsync_SameTeam_ThrowsBadRequestException()
        {
            var dto = new CreateFriendlyMatchDto { HomeTeamId = 1, AwayTeamId = 1, Format = DomainEnums.MatchFormat.ElevenSide };

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.CreateFriendlyMatchAsync(dto));
        }

        [Fact]
        public async Task CreateFriendlyMatchAsync_HomeTeamNotFound_ThrowsNotFoundException()
        {
            var dto = new CreateFriendlyMatchDto { HomeTeamId = 1, AwayTeamId = 2, Format = DomainEnums.MatchFormat.ElevenSide };
            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync((Team?)null);
            SetupRepository(teamRepo);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.CreateFriendlyMatchAsync(dto));
        }

        [Fact]
        public async Task CreateFriendlyMatchAsync_AwayTeamNotFound_ThrowsNotFoundException()
        {
            var dto = new CreateFriendlyMatchDto { HomeTeamId = 1, AwayTeamId = 2, Format = DomainEnums.MatchFormat.ElevenSide };
            var homeTeam = new Team { Id = 1, Name = "Home" };
            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.Is<Expression<Func<Team, bool>>>(e =>
                e.Compile()(homeTeam))))
                .ReturnsAsync(homeTeam);
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.Is<Expression<Func<Team, bool>>>(e =>
                !e.Compile()(homeTeam))))
                .ReturnsAsync((Team?)null);
            SetupRepository(teamRepo);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.CreateFriendlyMatchAsync(dto));
        }

        [Fact]
        public async Task CreateFriendlyMatchAsync_ValidRequest_CreatesMatchAndReturnsDto()
        {
            var dto = new CreateFriendlyMatchDto
            {
                HomeTeamId = 1, AwayTeamId = 2, Format = DomainEnums.MatchFormat.ElevenSide,
                MatchDate = DateTime.UtcNow, Location = "Stadium"
            };
            var homeTeam = new Team { Id = 1, Name = "Home" };
            var awayTeam = new Team { Id = 2, Name = "Away" };

            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync((Expression<Func<Team, bool>> expr) =>
                {
                    var compiled = expr.Compile();
                    if (compiled(homeTeam)) return homeTeam;
                    if (compiled(awayTeam)) return awayTeam;
                    return null;
                });
            SetupRepository(teamRepo);

            var matchEntity = new MatchEntity { Id = 100 };
            _mapperMock.Setup(m => m.Map<MatchEntity>(dto)).Returns(matchEntity);

            var matchRepo = new Mock<IRepository<MatchEntity>>();
            var matches = new List<MatchEntity>
            {
                new MatchEntity
                {
                    Id = 100, HomeTeamId = 1, AwayTeamId = 2, HomeTeam = homeTeam, AwayTeam = awayTeam
                }
            };
            var matchQueryable = matches.BuildMock();
            matchRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(matchQueryable);
            SetupRepository(matchRepo);

            var responseDto = new MatchResponseDto { Id = 100, HomeTeamId = 1, AwayTeamId = 2 };
            _mapperMock.Setup(m => m.Map<MatchResponseDto>(It.IsAny<MatchEntity>())).Returns(responseDto);

            var result = await _service.CreateFriendlyMatchAsync(dto);

            Assert.NotNull(result);
            Assert.Equal(100, result.Id);
            matchRepo.Verify(r => r.AddAsync(It.IsAny<MatchEntity>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(1));
        }

        #endregion

        #region CreateTournamentMatchAsync

        [Fact]
        public async Task CreateTournamentMatchAsync_FixtureNotFound_ThrowsNotFoundException()
        {
            var dto = new CreateTournamentMatchDto
            {
                TournamentFixtureId = 1, HomeTeamId = 1, AwayTeamId = 2, Format = DomainEnums.MatchFormat.ElevenSide
            };

            var fixtureRepo = new Mock<IRepository<TournamentFixture>>();
            var fixtures = new List<TournamentFixture>();
            var fixtureQueryable = fixtures.BuildMock();
            fixtureRepo.Setup(r => r.GetQueryable()).Returns(fixtureQueryable);
            SetupRepository(fixtureRepo);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.CreateTournamentMatchAsync(dto));
        }

        [Fact]
        public async Task CreateTournamentMatchAsync_FixtureAlreadyHasMatch_ThrowsBadRequestException()
        {
            var dto = new CreateTournamentMatchDto
            {
                TournamentFixtureId = 1, HomeTeamId = 1, AwayTeamId = 2, Format = DomainEnums.MatchFormat.ElevenSide
            };
            var fixture = new TournamentFixture
            {
                Id = 1, MatchId = 99, HomeTeamId = 10, AwayTeamId = 20,
                Group = new TournamentGroup { TournamentId = 5 },
                HomeTeam = new TournamentTeam { TeamId = 1, Team = new Team { Id = 1, Name = "Home" } },
                AwayTeam = new TournamentTeam { TeamId = 2, Team = new Team { Id = 2, Name = "Away" } }
            };

            var fixtures = new List<TournamentFixture> { fixture };
            var fixtureQueryable = fixtures.BuildMock();
            var fixtureRepo = new Mock<IRepository<TournamentFixture>>();
            fixtureRepo.Setup(r => r.GetQueryable()).Returns(fixtureQueryable);
            SetupRepository(fixtureRepo);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.CreateTournamentMatchAsync(dto));
        }

        [Fact]
        public async Task CreateTournamentMatchAsync_HomeTeamIdMismatch_ThrowsBadRequestException()
        {
            var dto = new CreateTournamentMatchDto
            {
                TournamentFixtureId = 1, HomeTeamId = 999, AwayTeamId = 2, Format = DomainEnums.MatchFormat.ElevenSide
            };
            var fixture = new TournamentFixture
            {
                Id = 1, MatchId = null, HomeTeamId = 10, AwayTeamId = 20,
                Group = new TournamentGroup { TournamentId = 5 },
                HomeTeam = new TournamentTeam { TeamId = 1, Team = new Team { Id = 1, Name = "Home" } },
                AwayTeam = new TournamentTeam { TeamId = 2, Team = new Team { Id = 2, Name = "Away" } }
            };

            var fixtures = new List<TournamentFixture> { fixture };
            var fixtureQueryable = fixtures.BuildMock();
            var fixtureRepo = new Mock<IRepository<TournamentFixture>>();
            fixtureRepo.Setup(r => r.GetQueryable()).Returns(fixtureQueryable);
            SetupRepository(fixtureRepo);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.CreateTournamentMatchAsync(dto));
        }

        [Fact]
        public async Task CreateTournamentMatchAsync_ValidRequest_CreatesMatchAndUpdatesFixture()
        {
            var dto = new CreateTournamentMatchDto
            {
                TournamentFixtureId = 1, HomeTeamId = 10, AwayTeamId = 20,
                Format = DomainEnums.MatchFormat.ElevenSide, MatchDate = DateTime.UtcNow, Location = "Stadium"
            };
            var homeTeam = new Team { Id = 1, Name = "Home" };
            var awayTeam = new Team { Id = 2, Name = "Away" };
            var fixture = new TournamentFixture
            {
                Id = 1, MatchId = null, HomeTeamId = 10, AwayTeamId = 20,
                Group = new TournamentGroup { TournamentId = 5 },
                HomeTeam = new TournamentTeam { TeamId = 1, Team = homeTeam },
                AwayTeam = new TournamentTeam { TeamId = 2, Team = awayTeam }
            };

            var fixtures = new List<TournamentFixture> { fixture };
            var fixtureQueryable = fixtures.BuildMock();
            var fixtureRepo = new Mock<IRepository<TournamentFixture>>();
            fixtureRepo.Setup(r => r.GetQueryable()).Returns(fixtureQueryable);
            SetupRepository(fixtureRepo);

            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync((Expression<Func<Team, bool>> expr) =>
                {
                    var compiled = expr.Compile();
                    if (compiled(homeTeam)) return homeTeam;
                    if (compiled(awayTeam)) return awayTeam;
                    return null;
                });
            SetupRepository(teamRepo);

            var matchEntity = new MatchEntity { Id = 100 };
            _mapperMock.Setup(m => m.Map<MatchEntity>(dto)).Returns(matchEntity);

            var matchRepo = new Mock<IRepository<MatchEntity>>();
            var matches = new List<MatchEntity>
            {
                new MatchEntity { Id = 100, HomeTeamId = 1, AwayTeamId = 2, HomeTeam = homeTeam, AwayTeam = awayTeam }
            };
            var matchQueryable = matches.BuildMock();
            matchRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(matchQueryable);
            SetupRepository(matchRepo);

            var responseDto = new MatchResponseDto { Id = 100 };
            _mapperMock.Setup(m => m.Map<MatchResponseDto>(It.IsAny<MatchEntity>())).Returns(responseDto);

            var result = await _service.CreateTournamentMatchAsync(dto);

            Assert.NotNull(result);
            Assert.Equal(100, result.Id);
            Assert.NotNull(fixture.MatchId);
            Assert.Equal(100, fixture.MatchId.Value);
            Assert.Equal(DomainEnums.MatchStatus.Scheduled, fixture.Status);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
        }

        #endregion

        #region CreateSessionMatchAsync

        [Fact]
        public async Task CreateSessionMatchAsync_SessionNotFound_ThrowsNotFoundException()
        {
            var dto = new CreateSessionMatchDto
            {
                SessionId = 1, Format = DomainEnums.MatchFormat.FiveSide,
                HomePlayers = new List<SessionSidePlayerDto>(),
                AwayPlayers = new List<SessionSidePlayerDto>()
            };

            var sessions = new List<DrillSession>();
            var sessionQueryable = sessions.BuildMock();
            var sessionRepo = new Mock<IRepository<DrillSession>>();
            sessionRepo.Setup(r => r.GetQueryable()).Returns(sessionQueryable);
            SetupRepository(sessionRepo);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.CreateSessionMatchAsync(dto));
        }

        [Fact]
        public async Task CreateSessionMatchAsync_NoPresentPlayers_ThrowsBadRequestException()
        {
            var dto = new CreateSessionMatchDto
            {
                SessionId = 1, Format = DomainEnums.MatchFormat.FiveSide,
                HomePlayers = new List<SessionSidePlayerDto>(),
                AwayPlayers = new List<SessionSidePlayerDto>()
            };
            var session = new DrillSession
            {
                Id = 1, TeamId = 10,
                SessionAttendances = new List<SessionAttendance>
                {
                    new SessionAttendance { playerId = 1, IsPresent = false }
                }
            };

            var sessions = new List<DrillSession> { session };
            var sessionQueryable = sessions.BuildMock();
            var sessionRepo = new Mock<IRepository<DrillSession>>();
            sessionRepo.Setup(r => r.GetQueryable()).Returns(sessionQueryable);
            SetupRepository(sessionRepo);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.CreateSessionMatchAsync(dto));
        }

        [Fact]
        public async Task CreateSessionMatchAsync_InvalidFormat_ThrowsBadRequestException()
        {
            var dto = new CreateSessionMatchDto
            {
                SessionId = 1, Format = (DomainEnums.MatchFormat)999,
                HomePlayers = new List<SessionSidePlayerDto>(),
                AwayPlayers = new List<SessionSidePlayerDto>()
            };
            var session = new DrillSession
            {
                Id = 1, TeamId = 10,
                SessionAttendances = new List<SessionAttendance>
                {
                    new SessionAttendance { playerId = 1, IsPresent = true }
                }
            };

            var sessions = new List<DrillSession> { session };
            var sessionQueryable = sessions.BuildMock();
            var sessionRepo = new Mock<IRepository<DrillSession>>();
            sessionRepo.Setup(r => r.GetQueryable()).Returns(sessionQueryable);
            SetupRepository(sessionRepo);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.CreateSessionMatchAsync(dto));
        }

        [Fact]
        public async Task CreateSessionMatchAsync_WrongStartingPlayerCount_ThrowsBadRequestException()
        {
            var dto = new CreateSessionMatchDto
            {
                SessionId = 1, Format = DomainEnums.MatchFormat.FiveSide,
                HomePlayers = new List<SessionSidePlayerDto>
                {
                    new SessionSidePlayerDto { PlayerId = 1, IsStarting = true },
                    new SessionSidePlayerDto { PlayerId = 2, IsStarting = true }
                },
                AwayPlayers = new List<SessionSidePlayerDto>
                {
                    new SessionSidePlayerDto { PlayerId = 3, IsStarting = true },
                    new SessionSidePlayerDto { PlayerId = 4, IsStarting = true },
                    new SessionSidePlayerDto { PlayerId = 5, IsStarting = true },
                    new SessionSidePlayerDto { PlayerId = 6, IsStarting = true },
                    new SessionSidePlayerDto { PlayerId = 7, IsStarting = true }
                }
            };
            var session = new DrillSession
            {
                Id = 1, TeamId = 10,
                SessionAttendances = new List<SessionAttendance>
                {
                    new SessionAttendance { playerId = 1, IsPresent = true },
                    new SessionAttendance { playerId = 2, IsPresent = true },
                    new SessionAttendance { playerId = 3, IsPresent = true },
                    new SessionAttendance { playerId = 4, IsPresent = true },
                    new SessionAttendance { playerId = 5, IsPresent = true },
                    new SessionAttendance { playerId = 6, IsPresent = true },
                    new SessionAttendance { playerId = 7, IsPresent = true }
                }
            };

            var sessions = new List<DrillSession> { session };
            var sessionQueryable = sessions.BuildMock();
            var sessionRepo = new Mock<IRepository<DrillSession>>();
            sessionRepo.Setup(r => r.GetQueryable()).Returns(sessionQueryable);
            SetupRepository(sessionRepo);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.CreateSessionMatchAsync(dto));
        }

        [Fact]
        public async Task CreateSessionMatchAsync_DuplicatedPlayers_ThrowsBadRequestException()
        {
            var dto = new CreateSessionMatchDto
            {
                SessionId = 1, Format = DomainEnums.MatchFormat.FiveSide,
                HomePlayers = Enumerable.Range(0, 5)
                    .Select(i => new SessionSidePlayerDto { PlayerId = 1, IsStarting = true }).ToList(),
                AwayPlayers = Enumerable.Range(0, 5)
                    .Select(i => new SessionSidePlayerDto { PlayerId = 6, IsStarting = true }).ToList()
            };
            var attendances = Enumerable.Range(1, 7).Select(i =>
                new SessionAttendance { playerId = i, IsPresent = true }).ToList();
            var session = new DrillSession { Id = 1, TeamId = 10, SessionAttendances = attendances };

            var sessions = new List<DrillSession> { session };
            var sessionQueryable = sessions.BuildMock();
            var sessionRepo = new Mock<IRepository<DrillSession>>();
            sessionRepo.Setup(r => r.GetQueryable()).Returns(sessionQueryable);
            SetupRepository(sessionRepo);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.CreateSessionMatchAsync(dto));
        }

        [Fact]
        public async Task CreateSessionMatchAsync_MissingPlayersFromSession_ThrowsBadRequestException()
        {
            var dto = new CreateSessionMatchDto
            {
                SessionId = 1, Format = DomainEnums.MatchFormat.FiveSide,
                HomePlayers = Enumerable.Range(1, 5)
                    .Select(i => new SessionSidePlayerDto { PlayerId = i, IsStarting = true }).ToList(),
                AwayPlayers = Enumerable.Range(6, 5)
                    .Select(i => new SessionSidePlayerDto { PlayerId = i, IsStarting = true }).ToList()
            };
            var session = new DrillSession
            {
                Id = 1, TeamId = 10,
                SessionAttendances = new List<SessionAttendance>
                {
                    new SessionAttendance { playerId = 1, IsPresent = true }
                }
            };

            var sessions = new List<DrillSession> { session };
            var sessionQueryable = sessions.BuildMock();
            var sessionRepo = new Mock<IRepository<DrillSession>>();
            sessionRepo.Setup(r => r.GetQueryable()).Returns(sessionQueryable);
            SetupRepository(sessionRepo);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.CreateSessionMatchAsync(dto));
        }

        [Fact]
        public async Task CreateSessionMatchAsync_ValidRequest_CreatesMatchAndLineups()
        {
            var dto = new CreateSessionMatchDto
            {
                SessionId = 1, Format = DomainEnums.MatchFormat.FiveSide, MatchDate = DateTime.UtcNow, Location = "Field",
                HomePlayers = Enumerable.Range(1, 5)
                    .Select(i => new SessionSidePlayerDto { PlayerId = i, IsStarting = true, JerseyNumber = i }).ToList(),
                AwayPlayers = Enumerable.Range(6, 5)
                    .Select(i => new SessionSidePlayerDto { PlayerId = i, IsStarting = true, JerseyNumber = i }).ToList()
            };

            var attendances = Enumerable.Range(1, 10).Select(i =>
                new SessionAttendance { playerId = i, IsPresent = true }).ToList();
            var session = new DrillSession { Id = 1, TeamId = 10, SessionAttendances = attendances };

            var sessions = new List<DrillSession> { session };
            var sessionQueryable = sessions.BuildMock();
            var sessionRepo = new Mock<IRepository<DrillSession>>();
            sessionRepo.Setup(r => r.GetQueryable()).Returns(sessionQueryable);
            SetupRepository(sessionRepo);

            var transactionMock = new Mock<IDbContextTransaction>();
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(transactionMock.Object);

            var matchEntity = new MatchEntity { Id = 100 };
            _mapperMock.Setup(m => m.Map<MatchEntity>(dto)).Returns(matchEntity);

            var matchRepo = new Mock<IRepository<MatchEntity>>();
            var matches = new List<MatchEntity>
            {
                new MatchEntity { Id = 100, HomeTeamId = 10, AwayTeamId = 10, HomeTeam = new Team { Id = 10, Name = "Team" }, AwayTeam = new Team { Id = 10, Name = "Team" } }
            };
            var matchQueryable = matches.BuildMock();
            matchRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(matchQueryable);
            SetupRepository(matchRepo);

            var lineupRepo = new Mock<IRepository<MatchLineupEntity>>();
            SetupRepository(lineupRepo);

            var responseDto = new MatchResponseDto { Id = 100 };
            _mapperMock.Setup(m => m.Map<MatchResponseDto>(It.IsAny<MatchEntity>())).Returns(responseDto);

            var result = await _service.CreateSessionMatchAsync(dto);

            Assert.NotNull(result);
            Assert.Equal(100, result.Id);
            matchRepo.Verify(r => r.AddAsync(It.IsAny<MatchEntity>()), Times.Once);
            lineupRepo.Verify(r => r.AddAsync(It.IsAny<MatchLineupEntity>()), Times.Exactly(10));
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
        }

        #endregion

        #region GetMatchAsync

        [Fact]
        public async Task GetMatchAsync_NotFound_ThrowsNotFoundException()
        {
            var matches = new List<MatchEntity>();
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            SetupRepository(matchRepo);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.GetMatchAsync(1));
        }

        [Fact]
        public async Task GetMatchAsync_Found_ReturnsDto()
        {
            var match = new MatchEntity { Id = 1, HomeTeam = new Team { Name = "H" }, AwayTeam = new Team { Name = "A" } };
            var matches = new List<MatchEntity> { match };
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            SetupRepository(matchRepo);

            var responseDto = new MatchResponseDto { Id = 1, HomeTeamName = "H" };
            _mapperMock.Setup(m => m.Map<MatchResponseDto>(match)).Returns(responseDto);

            var result = await _service.GetMatchAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
        }

        #endregion

        #region CancelMatchAsync

        [Fact]
        public async Task CancelMatchAsync_NotFound_ThrowsNotFoundException()
        {
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .ReturnsAsync((MatchEntity?)null);
            SetupRepository(matchRepo);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.CancelMatchAsync(1));
        }

        [Fact]
        public async Task CancelMatchAsync_NotScheduled_ThrowsNotFoundException()
        {
            var match = new MatchEntity { Id = 1, Status = DomainEnums.MatchStatus.Completed };
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .ReturnsAsync(match);
            SetupRepository(matchRepo);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.CancelMatchAsync(1));
        }

        [Fact]
        public async Task CancelMatchAsync_Scheduled_SetsCancelledAndSaves()
        {
            var match = new MatchEntity { Id = 1, Status = DomainEnums.MatchStatus.Scheduled };
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .ReturnsAsync(match);
            SetupRepository(matchRepo);

            await _service.CancelMatchAsync(1);

            Assert.Equal(DomainEnums.MatchStatus.Cancelled, match.Status);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region StartMatchAsync

        [Fact]
        public async Task StartMatchAsync_NotFound_ThrowsNotFoundException()
        {
            var matches = new List<MatchEntity>();
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(matchRepo);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.StartMatchAsync(1));
        }

        [Fact]
        public async Task StartMatchAsync_NotScheduled_ThrowsBadRequestException()
        {
            var match = new MatchEntity { Id = 1, Status = DomainEnums.MatchStatus.Live };
            var matches = new List<MatchEntity> { match };
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(matchRepo);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.StartMatchAsync(1));
        }

        [Fact]
        public async Task StartMatchAsync_Scheduled_SetsLiveAndSaves()
        {
            var match = new MatchEntity { Id = 1, Status = DomainEnums.MatchStatus.Scheduled };
            var matches = new List<MatchEntity> { match };
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(matchRepo);

            await _service.StartMatchAsync(1);

            Assert.Equal(DomainEnums.MatchStatus.Live, match.Status);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region EndMatchAsync

        [Fact]
        public async Task EndMatchAsync_NotFound_ThrowsNotFoundException()
        {
            var matches = new List<MatchEntity>();
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(matchRepo);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.EndMatchAsync(1));
        }

        [Fact]
        public async Task EndMatchAsync_NotLive_ThrowsBadRequestException()
        {
            var match = new MatchEntity { Id = 1, Status = DomainEnums.MatchStatus.Scheduled };
            var matches = new List<MatchEntity> { match };
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(matchRepo);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.EndMatchAsync(1));
        }

        [Fact]
        public async Task EndMatchAsync_HomeWin_SetsHomeAsWinner()
        {
            var homeTeam = new Team { Id = 1, Name = "Home" };
            var awayTeam = new Team { Id = 2, Name = "Away" };
            var match = new MatchEntity
            {
                Id = 1, Status = DomainEnums.MatchStatus.Live, HomeTeamId = 1, AwayTeamId = 2,
                HomeScore = 3, AwayScore = 1, HomeTeam = homeTeam, AwayTeam = awayTeam
            };
            var matches = new List<MatchEntity> { match };
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(matchRepo);

            var responseDto = new MatchResponseDto { Id = 1, WinningTeamId = 1 };
            _mapperMock.Setup(m => m.Map<MatchResponseDto>(match)).Returns(responseDto);

            var result = await _service.EndMatchAsync(1);

            Assert.Equal(DomainEnums.MatchStatus.Completed, match.Status);
            Assert.Equal(1, match.WinningTeamId);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task EndMatchAsync_AwayWin_SetsAwayAsWinner()
        {
            var homeTeam = new Team { Id = 1, Name = "Home" };
            var awayTeam = new Team { Id = 2, Name = "Away" };
            var match = new MatchEntity
            {
                Id = 1, Status = DomainEnums.MatchStatus.Live, HomeTeamId = 1, AwayTeamId = 2,
                HomeScore = 1, AwayScore = 3, HomeTeam = homeTeam, AwayTeam = awayTeam
            };
            var matches = new List<MatchEntity> { match };
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(matchRepo);

            var responseDto = new MatchResponseDto { Id = 1, WinningTeamId = 2 };
            _mapperMock.Setup(m => m.Map<MatchResponseDto>(match)).Returns(responseDto);

            var result = await _service.EndMatchAsync(1);

            Assert.Equal(DomainEnums.MatchStatus.Completed, match.Status);
            Assert.Equal(2, match.WinningTeamId);
        }

        [Fact]
        public async Task EndMatchAsync_DrawWithHomePenaltyWin_SetsHomeAsWinner()
        {
            var homeTeam = new Team { Id = 1, Name = "Home" };
            var awayTeam = new Team { Id = 2, Name = "Away" };
            var match = new MatchEntity
            {
                Id = 1, Status = DomainEnums.MatchStatus.Live, HomeTeamId = 1, AwayTeamId = 2,
                HomeScore = 2, AwayScore = 2, HomePenaltyScore = 5, AwayPenaltyScore = 4,
                HomeTeam = homeTeam, AwayTeam = awayTeam
            };
            var matches = new List<MatchEntity> { match };
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(matchRepo);

            var responseDto = new MatchResponseDto { Id = 1, WinningTeamId = 1 };
            _mapperMock.Setup(m => m.Map<MatchResponseDto>(match)).Returns(responseDto);

            var result = await _service.EndMatchAsync(1);

            Assert.Equal(1, match.WinningTeamId);
        }

        [Fact]
        public async Task EndMatchAsync_DrawWithNoPenalties_SetsNullWinner()
        {
            var homeTeam = new Team { Id = 1, Name = "Home" };
            var awayTeam = new Team { Id = 2, Name = "Away" };
            var match = new MatchEntity
            {
                Id = 1, Status = DomainEnums.MatchStatus.Live, HomeTeamId = 1, AwayTeamId = 2,
                HomeScore = 2, AwayScore = 2, HomeTeam = homeTeam, AwayTeam = awayTeam
            };
            var matches = new List<MatchEntity> { match };
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(matchRepo);

            var responseDto = new MatchResponseDto { Id = 1 };
            _mapperMock.Setup(m => m.Map<MatchResponseDto>(match)).Returns(responseDto);

            var result = await _service.EndMatchAsync(1);

            Assert.Null(match.WinningTeamId);
        }

        #endregion

        #region GetFormGuideAsync

        [Fact]
        public async Task GetFormGuideAsync_TeamNotFound_ThrowsNotFoundException()
        {
            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync((Team?)null);
            SetupRepository(teamRepo);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.GetFormGuideAsync(1, DomainEnums.MatchFormat.ElevenSide));
        }

        [Fact]
        public async Task GetFormGuideAsync_ReturnsFormGuideWithResults()
        {
            var team = new Team { Id = 1, Name = "Reds" };
            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync(team);
            SetupRepository(teamRepo);

            var matches = new List<MatchEntity>
            {
                new MatchEntity { Id = 1, HomeTeamId = 1, AwayTeamId = 2, HomeScore = 3, AwayScore = 1, Status = DomainEnums.MatchStatus.Completed, Format = DomainEnums.MatchFormat.ElevenSide, Type = DomainEnums.MatchType.Friendly, MatchDate = DateTime.UtcNow.AddDays(-1) },
                new MatchEntity { Id = 2, HomeTeamId = 3, AwayTeamId = 1, HomeScore = 2, AwayScore = 2, HomePenaltyScore = 4, AwayPenaltyScore = 5, Status = DomainEnums.MatchStatus.Completed, Format = DomainEnums.MatchFormat.ElevenSide, Type = DomainEnums.MatchType.Friendly, MatchDate = DateTime.UtcNow.AddDays(-2) },
                new MatchEntity { Id = 3, HomeTeamId = 2, AwayTeamId = 1, HomeScore = 2, AwayScore = 0, Status = DomainEnums.MatchStatus.Completed, Format = DomainEnums.MatchFormat.ElevenSide, Type = DomainEnums.MatchType.Friendly, MatchDate = DateTime.UtcNow.AddDays(-3) },
                new MatchEntity { Id = 4, HomeTeamId = 1, AwayTeamId = 2, HomeScore = 2, AwayScore = 0, Status = DomainEnums.MatchStatus.Completed, Format = DomainEnums.MatchFormat.ElevenSide, Type = DomainEnums.MatchType.Friendly, MatchDate = DateTime.UtcNow.AddDays(-4) },
                new MatchEntity { Id = 5, HomeTeamId = 1, AwayTeamId = 2, HomeScore = 2, AwayScore = 2, Status = DomainEnums.MatchStatus.Completed, Format = DomainEnums.MatchFormat.ElevenSide, Type = DomainEnums.MatchType.Friendly, MatchDate = DateTime.UtcNow.AddDays(-5) }
            };
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            SetupRepository(matchRepo);

            var result = await _service.GetFormGuideAsync(1, DomainEnums.MatchFormat.ElevenSide);

            Assert.NotNull(result);
            Assert.Equal(1, result.TeamId);
            Assert.Equal("Reds", result.TeamName);
            Assert.Equal(5, result.Results.Count);
            Assert.Equal("W", result.Results[0]);
            Assert.Equal("W", result.Results[1]);
            Assert.Equal("L", result.Results[2]);
            Assert.Equal("W", result.Results[3]);
            Assert.Equal("D", result.Results[4]);
        }

        [Fact]
        public async Task GetFormGuideAsync_OnlyTakesLastFiveMatches()
        {
            var team = new Team { Id = 1, Name = "Reds" };
            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync(team);
            SetupRepository(teamRepo);

            var matches = new List<MatchEntity>();
            for (int i = 1; i <= 10; i++)
            {
                matches.Add(new MatchEntity
                {
                    Id = i, HomeTeamId = 1, AwayTeamId = 2, HomeScore = 1, AwayScore = 0,
                    Status = DomainEnums.MatchStatus.Completed, Format = DomainEnums.MatchFormat.ElevenSide,
                    Type = DomainEnums.MatchType.Friendly, MatchDate = DateTime.UtcNow.AddDays(-i)
                });
            }
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            SetupRepository(matchRepo);

            var result = await _service.GetFormGuideAsync(1, DomainEnums.MatchFormat.ElevenSide);

            Assert.Equal(5, result.Results.Count);
        }

        #endregion

        #region GetMatchesByDateAsync

        [Fact]
        public async Task GetMatchesByDateAsync_ReturnsPaginatedResults()
        {
            var date = new DateTime(2025, 1, 15);
            var matches = new List<MatchEntity>
            {
                new MatchEntity { Id = 1, MatchDate = date.AddHours(10), HomeTeam = new Team(), AwayTeam = new Team() },
                new MatchEntity { Id = 2, MatchDate = date.AddHours(14), HomeTeam = new Team(), AwayTeam = new Team() },
                new MatchEntity { Id = 3, MatchDate = date.AddHours(16), HomeTeam = new Team(), AwayTeam = new Team() }
            };
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            SetupRepository(matchRepo);

            var dtoList = matches.Select(m => new MatchResponseDto { Id = m.Id }).ToList();
            _mapperMock.Setup(m => m.Map<List<MatchResponseDto>>(It.IsAny<List<MatchEntity>>())).Returns(dtoList);

            var result = await _service.GetMatchesByDateAsync(date, 1, 10);

            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(1, result.Page);
            Assert.Equal(10, result.PageSize);
            Assert.Equal(3, result.Matches.Count);
        }

        #endregion

        #region GetTeamMatchesByStatusAsync

        [Fact]
        public async Task GetTeamMatchesByStatusAsync_TeamNotFound_ThrowsNotFoundException()
        {
            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync((Team?)null);
            SetupRepository(teamRepo);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.GetTeamMatchesByStatusAsync(1, null, 1, 10));
        }

        [Fact]
        public async Task GetTeamMatchesByStatusAsync_WithStatusFilter_ReturnsFilteredResults()
        {
            var team = new Team { Id = 1, Name = "Team1" };
            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync(team);
            SetupRepository(teamRepo);

            var matches = new List<MatchEntity>
            {
                new MatchEntity { Id = 1, HomeTeamId = 1, AwayTeamId = 2, Status = DomainEnums.MatchStatus.Scheduled, MatchDate = DateTime.UtcNow, HomeTeam = new Team(), AwayTeam = new Team() },
                new MatchEntity { Id = 2, HomeTeamId = 3, AwayTeamId = 1, Status = DomainEnums.MatchStatus.Completed, MatchDate = DateTime.UtcNow, HomeTeam = new Team(), AwayTeam = new Team() }
            };
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            SetupRepository(matchRepo);

            var dtoList = new List<MatchResponseDto> { new MatchResponseDto { Id = 1 } };
            _mapperMock.Setup(m => m.Map<List<MatchResponseDto>>(It.IsAny<List<MatchEntity>>())).Returns(dtoList);

            var result = await _service.GetTeamMatchesByStatusAsync(1, DomainEnums.MatchStatus.Scheduled, 1, 10);

            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Matches);
        }

        [Fact]
        public async Task GetTeamMatchesByStatusAsync_EmptyResult_ReturnsZeroCount()
        {
            var team = new Team { Id = 1, Name = "Team1" };
            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync(team);
            SetupRepository(teamRepo);

            var matches = new List<MatchEntity>();
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            SetupRepository(matchRepo);

            var dtoList = new List<MatchResponseDto>();
            _mapperMock.Setup(m => m.Map<List<MatchResponseDto>>(It.IsAny<List<MatchEntity>>())).Returns(dtoList);

            var result = await _service.GetTeamMatchesByStatusAsync(1, null, 1, 10);

            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Matches);
        }

        #endregion
    }
}
