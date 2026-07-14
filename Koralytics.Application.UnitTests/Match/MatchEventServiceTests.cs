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
using DomainEnums = Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using MatchEntity = Koralytics.Domain.Entities.Match.Match;
using MatchEventEntity = Koralytics.Domain.Entities.Match.MatchEvent;
using MatchLineupEntity = Koralytics.Domain.Entities.Match.MatchLineup;

namespace Koralytics.Application.UnitTests.Match
{
    public class MatchEventServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<MatchEventService>> _loggerMock;
        private readonly MatchEventService _service;

        public MatchEventServiceTests()
        {
            _unitOfWorkMock = new();
            _mapperMock = new();
            _loggerMock = new();
            _service = new MatchEventService(_unitOfWorkMock.Object, _mapperMock.Object, _loggerMock.Object);
        }

        private void SetupRepository<T>(Mock<IRepository<T>> repo) where T : class, Koralytics.Domain.Interfaces.ISoftDelete
        {
            _unitOfWorkMock.Setup(u => u.Repository<T>()).Returns(repo.Object);
        }

        #region LogMatchEventAsync

        [Fact]
        public async Task LogMatchEventAsync_MatchNotFound_ThrowsNotFoundException()
        {
            var matches = new List<MatchEntity>();
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(matchRepo);

            var dto = new LogMatchEventDto { TeamId = 1, PlayerId = 1, EventType = DomainEnums.MatchEventType.Goal, Minute = 45 };

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.LogMatchEventAsync(1, dto));
        }

        [Fact]
        public async Task LogMatchEventAsync_MatchNotLive_ThrowsBadRequestException()
        {
            var match = new MatchEntity { Id = 1, Status = DomainEnums.MatchStatus.Scheduled, HomeTeamId = 1, AwayTeamId = 2 };
            var matches = new List<MatchEntity> { match };
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(matchRepo);

            var dto = new LogMatchEventDto { TeamId = 1, PlayerId = 1, EventType = DomainEnums.MatchEventType.Goal, Minute = 45 };

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.LogMatchEventAsync(1, dto));
        }

        [Fact]
        public async Task LogMatchEventAsync_InvalidTeam_ThrowsBadRequestException()
        {
            var match = new MatchEntity { Id = 1, Status = DomainEnums.MatchStatus.Live, HomeTeamId = 1, AwayTeamId = 2 };
            var matches = new List<MatchEntity> { match };
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(matchRepo);

            var dto = new LogMatchEventDto { TeamId = 99, PlayerId = 1, EventType = DomainEnums.MatchEventType.Goal, Minute = 45 };

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.LogMatchEventAsync(1, dto));
        }

        [Fact]
        public async Task LogMatchEventAsync_PlayerNotInLineup_ThrowsBadRequestException()
        {
            var match = new MatchEntity { Id = 1, Status = DomainEnums.MatchStatus.Live, HomeTeamId = 1, AwayTeamId = 2 };
            var matches = new List<MatchEntity> { match };
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(matchRepo);

            var lineupRepo = new Mock<IRepository<MatchLineupEntity>>();
            lineupRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<MatchLineupEntity, bool>>>()))
                .ReturnsAsync(false);
            SetupRepository(lineupRepo);

            var dto = new LogMatchEventDto { TeamId = 1, PlayerId = 999, EventType = DomainEnums.MatchEventType.Goal, Minute = 45 };

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.LogMatchEventAsync(1, dto));
        }

        [Fact]
        public async Task LogMatchEventAsync_HomeGoal_IncrementsHomeScore()
        {
            var match = new MatchEntity { Id = 1, Status = DomainEnums.MatchStatus.Live, HomeTeamId = 1, AwayTeamId = 2, HomeScore = 0, AwayScore = 0 };
            var matches = new List<MatchEntity> { match };
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(matchRepo);

            var lineupRepo = new Mock<IRepository<MatchLineupEntity>>();
            lineupRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<MatchLineupEntity, bool>>>()))
                .ReturnsAsync(true);
            SetupRepository(lineupRepo);

            var createdEvent = new MatchEventEntity
            {
                Id = 1, MatchId = 1, TeamId = 1, PlayerId = 1, EventType = DomainEnums.MatchEventType.Goal, Minute = 45,
                Team = new Team { Name = "Home" }, Player = new Koralytics.Domain.Entities.Player.Player { FirstName = "A", LastName = "B" }
            };
            var events = new List<MatchEventEntity> { createdEvent };
            var eventQueryable = events.BuildMock();
            var eventRepo = new Mock<IRepository<MatchEventEntity>>();
            eventRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(eventQueryable);
            SetupRepository(eventRepo);

            _mapperMock.Setup(m => m.Map<MatchEventEntity>(It.IsAny<LogMatchEventDto>())).Returns(new MatchEventEntity());
            _mapperMock.Setup(m => m.Map<MatchEventResponseDto>(It.IsAny<MatchEventEntity>()))
                .Returns(new MatchEventResponseDto { Id = 1 });

            var dto = new LogMatchEventDto { TeamId = 1, PlayerId = 1, EventType = DomainEnums.MatchEventType.Goal, Minute = 45 };

            var result = await _service.LogMatchEventAsync(1, dto);

            Assert.NotNull(result);
            Assert.Equal(1, match.HomeScore);
            Assert.Equal(0, match.AwayScore);
        }

        [Fact]
        public async Task LogMatchEventAsync_AwayGoal_IncrementsAwayScore()
        {
            var match = new MatchEntity { Id = 1, Status = DomainEnums.MatchStatus.Live, HomeTeamId = 1, AwayTeamId = 2, HomeScore = 0, AwayScore = 0 };
            var matches = new List<MatchEntity> { match };
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(matchRepo);

            var lineupRepo = new Mock<IRepository<MatchLineupEntity>>();
            lineupRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<MatchLineupEntity, bool>>>()))
                .ReturnsAsync(true);
            SetupRepository(lineupRepo);

            var createdEvent = new MatchEventEntity
            {
                Id = 1, MatchId = 1, TeamId = 2, PlayerId = 1, EventType = DomainEnums.MatchEventType.Goal, Minute = 45,
                Team = new Team { Name = "Away" }, Player = new Koralytics.Domain.Entities.Player.Player { FirstName = "A", LastName = "B" }
            };
            var events = new List<MatchEventEntity> { createdEvent };
            var eventQueryable = events.BuildMock();
            var eventRepo = new Mock<IRepository<MatchEventEntity>>();
            eventRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(eventQueryable);
            SetupRepository(eventRepo);

            _mapperMock.Setup(m => m.Map<MatchEventEntity>(It.IsAny<LogMatchEventDto>())).Returns(new MatchEventEntity());
            _mapperMock.Setup(m => m.Map<MatchEventResponseDto>(It.IsAny<MatchEventEntity>()))
                .Returns(new MatchEventResponseDto { Id = 1 });

            var dto = new LogMatchEventDto { TeamId = 2, PlayerId = 1, EventType = DomainEnums.MatchEventType.Goal, Minute = 45 };

            var result = await _service.LogMatchEventAsync(1, dto);

            Assert.Equal(0, match.HomeScore);
            Assert.Equal(1, match.AwayScore);
        }

        [Fact]
        public async Task LogMatchEventAsync_HomeOwnGoal_IncrementsAwayScore()
        {
            var match = new MatchEntity { Id = 1, Status = DomainEnums.MatchStatus.Live, HomeTeamId = 1, AwayTeamId = 2, HomeScore = 0, AwayScore = 0 };
            var matches = new List<MatchEntity> { match };
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(matchRepo);

            var lineupRepo = new Mock<IRepository<MatchLineupEntity>>();
            lineupRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<MatchLineupEntity, bool>>>()))
                .ReturnsAsync(true);
            SetupRepository(lineupRepo);

            var createdEvent = new MatchEventEntity
            {
                Id = 1, MatchId = 1, TeamId = 1, PlayerId = 1, EventType = DomainEnums.MatchEventType.OwnGoal, Minute = 45,
                Team = new Team { Name = "Home" }, Player = new Koralytics.Domain.Entities.Player.Player { FirstName = "A", LastName = "B" }
            };
            var events = new List<MatchEventEntity> { createdEvent };
            var eventQueryable = events.BuildMock();
            var eventRepo = new Mock<IRepository<MatchEventEntity>>();
            eventRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(eventQueryable);
            SetupRepository(eventRepo);

            _mapperMock.Setup(m => m.Map<MatchEventEntity>(It.IsAny<LogMatchEventDto>())).Returns(new MatchEventEntity());
            _mapperMock.Setup(m => m.Map<MatchEventResponseDto>(It.IsAny<MatchEventEntity>()))
                .Returns(new MatchEventResponseDto { Id = 1 });

            var dto = new LogMatchEventDto { TeamId = 1, PlayerId = 1, EventType = DomainEnums.MatchEventType.OwnGoal, Minute = 45 };

            await _service.LogMatchEventAsync(1, dto);

            Assert.Equal(0, match.HomeScore);
            Assert.Equal(1, match.AwayScore);
        }

        [Fact]
        public async Task LogMatchEventAsync_HomePenalty_IncrementsHomePenaltyScore()
        {
            var match = new MatchEntity { Id = 1, Status = DomainEnums.MatchStatus.Live, HomeTeamId = 1, AwayTeamId = 2, HomeScore = 0, AwayScore = 0 };
            var matches = new List<MatchEntity> { match };
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(matchRepo);

            var lineupRepo = new Mock<IRepository<MatchLineupEntity>>();
            lineupRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<MatchLineupEntity, bool>>>()))
                .ReturnsAsync(true);
            SetupRepository(lineupRepo);

            var createdEvent = new MatchEventEntity
            {
                Id = 1, MatchId = 1, TeamId = 1, PlayerId = 1, EventType = DomainEnums.MatchEventType.PenaltyScored, Minute = 90,
                Team = new Team { Name = "Home" }, Player = new Koralytics.Domain.Entities.Player.Player { FirstName = "A", LastName = "B" }
            };
            var events = new List<MatchEventEntity> { createdEvent };
            var eventQueryable = events.BuildMock();
            var eventRepo = new Mock<IRepository<MatchEventEntity>>();
            eventRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(eventQueryable);
            SetupRepository(eventRepo);

            _mapperMock.Setup(m => m.Map<MatchEventEntity>(It.IsAny<LogMatchEventDto>())).Returns(new MatchEventEntity());
            _mapperMock.Setup(m => m.Map<MatchEventResponseDto>(It.IsAny<MatchEventEntity>()))
                .Returns(new MatchEventResponseDto { Id = 1 });

            var dto = new LogMatchEventDto { TeamId = 1, PlayerId = 1, EventType = DomainEnums.MatchEventType.PenaltyScored, Minute = 90 };

            await _service.LogMatchEventAsync(1, dto);

            Assert.Equal(1, match.HomePenaltyScore);
            Assert.Null(match.AwayPenaltyScore);
        }

        #endregion

        #region LogSessionMatchEventAsync

        [Fact]
        public async Task LogSessionMatchEventAsync_MatchNotFound_ThrowsNotFoundException()
        {
            var matches = new List<MatchEntity>();
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(matchRepo);

            var dto = new LogSessionMatchEventDto { PlayerId = 1, IsHomeSide = true, EventType = DomainEnums.MatchEventType.Goal, Minute = 30 };

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.LogSessionMatchEventAsync(1, dto));
        }

        [Fact]
        public async Task LogSessionMatchEventAsync_NotSessionMatch_ThrowsBadRequestException()
        {
            var match = new MatchEntity { Id = 1, Status = DomainEnums.MatchStatus.Live, Type = DomainEnums.MatchType.Friendly, HomeTeamId = 1, AwayTeamId = 2 };
            var matches = new List<MatchEntity> { match };
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(matchRepo);

            var dto = new LogSessionMatchEventDto { PlayerId = 1, IsHomeSide = true, EventType = DomainEnums.MatchEventType.Goal, Minute = 30 };

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.LogSessionMatchEventAsync(1, dto));
        }

        [Fact]
        public async Task LogSessionMatchEventAsync_MatchNotLive_ThrowsBadRequestException()
        {
            var match = new MatchEntity { Id = 1, Status = DomainEnums.MatchStatus.Scheduled, Type = DomainEnums.MatchType.Session, HomeTeamId = 1, AwayTeamId = 1 };
            var matches = new List<MatchEntity> { match };
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(matchRepo);

            var dto = new LogSessionMatchEventDto { PlayerId = 1, IsHomeSide = true, EventType = DomainEnums.MatchEventType.Goal, Minute = 30 };

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.LogSessionMatchEventAsync(1, dto));
        }

        [Fact]
        public async Task LogSessionMatchEventAsync_PlayerNotInLineup_ThrowsBadRequestException()
        {
            var match = new MatchEntity { Id = 1, Status = DomainEnums.MatchStatus.Live, Type = DomainEnums.MatchType.Session, HomeTeamId = 1, AwayTeamId = 1 };
            var matches = new List<MatchEntity> { match };
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(matchRepo);

            var lineupRepo = new Mock<IRepository<MatchLineupEntity>>();
            lineupRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<MatchLineupEntity, bool>>>()))
                .ReturnsAsync(false);
            SetupRepository(lineupRepo);

            var dto = new LogSessionMatchEventDto { PlayerId = 1, IsHomeSide = true, EventType = DomainEnums.MatchEventType.Goal, Minute = 30 };

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.LogSessionMatchEventAsync(1, dto));
        }

        [Fact]
        public async Task LogSessionMatchEventAsync_HomeGoal_IncrementsHomeScore()
        {
            var match = new MatchEntity { Id = 1, Status = DomainEnums.MatchStatus.Live, Type = DomainEnums.MatchType.Session, HomeTeamId = 1, AwayTeamId = 1, HomeScore = 0, AwayScore = 0 };
            var matches = new List<MatchEntity> { match };
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(matchRepo);

            var lineupRepo = new Mock<IRepository<MatchLineupEntity>>();
            lineupRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<MatchLineupEntity, bool>>>()))
                .ReturnsAsync(true);
            SetupRepository(lineupRepo);

            var createdEvent = new MatchEventEntity
            {
                Id = 1, MatchId = 1, TeamId = 1, PlayerId = 1, EventType = DomainEnums.MatchEventType.Goal, Minute = 30,
                Team = new Team { Name = "T" }, Player = new Koralytics.Domain.Entities.Player.Player { FirstName = "A", LastName = "B" }
            };
            var events = new List<MatchEventEntity> { createdEvent };
            var eventQueryable = events.BuildMock();
            var eventRepo = new Mock<IRepository<MatchEventEntity>>();
            eventRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(eventQueryable);
            SetupRepository(eventRepo);

            _mapperMock.Setup(m => m.Map<MatchEventResponseDto>(It.IsAny<MatchEventEntity>()))
                .Returns(new MatchEventResponseDto { Id = 1 });

            var dto = new LogSessionMatchEventDto { PlayerId = 1, IsHomeSide = true, EventType = DomainEnums.MatchEventType.Goal, Minute = 30 };

            var result = await _service.LogSessionMatchEventAsync(1, dto);

            Assert.NotNull(result);
            Assert.Equal(1, match.HomeScore);
            Assert.Equal(0, match.AwayScore);
        }

        [Fact]
        public async Task LogSessionMatchEventAsync_AwayGoal_IncrementsAwayScore()
        {
            var match = new MatchEntity { Id = 1, Status = DomainEnums.MatchStatus.Live, Type = DomainEnums.MatchType.Session, HomeTeamId = 1, AwayTeamId = 1, HomeScore = 0, AwayScore = 0 };
            var matches = new List<MatchEntity> { match };
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(matchRepo);

            var lineupRepo = new Mock<IRepository<MatchLineupEntity>>();
            lineupRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<MatchLineupEntity, bool>>>()))
                .ReturnsAsync(true);
            SetupRepository(lineupRepo);

            var createdEvent = new MatchEventEntity
            {
                Id = 1, MatchId = 1, TeamId = 1, PlayerId = 1, EventType = DomainEnums.MatchEventType.Goal, Minute = 30,
                Team = new Team { Name = "T" }, Player = new Koralytics.Domain.Entities.Player.Player { FirstName = "A", LastName = "B" }
            };
            var events = new List<MatchEventEntity> { createdEvent };
            var eventQueryable = events.BuildMock();
            var eventRepo = new Mock<IRepository<MatchEventEntity>>();
            eventRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(eventQueryable);
            SetupRepository(eventRepo);

            _mapperMock.Setup(m => m.Map<MatchEventResponseDto>(It.IsAny<MatchEventEntity>()))
                .Returns(new MatchEventResponseDto { Id = 1 });

            var dto = new LogSessionMatchEventDto { PlayerId = 1, IsHomeSide = false, EventType = DomainEnums.MatchEventType.Goal, Minute = 30 };

            var result = await _service.LogSessionMatchEventAsync(1, dto);

            Assert.Equal(0, match.HomeScore);
            Assert.Equal(1, match.AwayScore);
        }

        [Fact]
        public async Task LogSessionMatchEventAsync_HomePenalty_IncrementsHomePenaltyScore()
        {
            var match = new MatchEntity { Id = 1, Status = DomainEnums.MatchStatus.Live, Type = DomainEnums.MatchType.Session, HomeTeamId = 1, AwayTeamId = 1, HomeScore = 0, AwayScore = 0 };
            var matches = new List<MatchEntity> { match };
            var queryable = matches.BuildMock();
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(matchRepo);

            var lineupRepo = new Mock<IRepository<MatchLineupEntity>>();
            lineupRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<MatchLineupEntity, bool>>>()))
                .ReturnsAsync(true);
            SetupRepository(lineupRepo);

            var createdEvent = new MatchEventEntity
            {
                Id = 1, MatchId = 1, TeamId = 1, PlayerId = 1, EventType = DomainEnums.MatchEventType.PenaltyScored, Minute = 90,
                Team = new Team { Name = "T" }, Player = new Koralytics.Domain.Entities.Player.Player { FirstName = "A", LastName = "B" }
            };
            var events = new List<MatchEventEntity> { createdEvent };
            var eventQueryable = events.BuildMock();
            var eventRepo = new Mock<IRepository<MatchEventEntity>>();
            eventRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(eventQueryable);
            SetupRepository(eventRepo);

            _mapperMock.Setup(m => m.Map<MatchEventResponseDto>(It.IsAny<MatchEventEntity>()))
                .Returns(new MatchEventResponseDto { Id = 1 });

            var dto = new LogSessionMatchEventDto { PlayerId = 1, IsHomeSide = true, EventType = DomainEnums.MatchEventType.PenaltyScored, Minute = 90 };

            await _service.LogSessionMatchEventAsync(1, dto);

            Assert.Equal(1, match.HomePenaltyScore);
            Assert.Null(match.AwayPenaltyScore);
        }

        #endregion

        #region GetMatchTimelineAsync

        [Fact]
        public async Task GetMatchTimelineAsync_MatchNotFound_ThrowsNotFoundException()
        {
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .ReturnsAsync(false);
            SetupRepository(matchRepo);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.GetMatchTimelineAsync(1));
        }

        [Fact]
        public async Task GetMatchTimelineAsync_ReturnsOrderedEvents()
        {
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .ReturnsAsync(true);
            SetupRepository(matchRepo);

            var events = new List<MatchEventEntity>
            {
                new MatchEventEntity { Id = 1, MatchId = 1, Minute = 30, EventType = DomainEnums.MatchEventType.Goal,
                    Team = new Team { Name = "H" }, Player = new Koralytics.Domain.Entities.Player.Player { FirstName = "A", LastName = "B" } },
                new MatchEventEntity { Id = 2, MatchId = 1, Minute = 15, EventType = DomainEnums.MatchEventType.YellowCard,
                    Team = new Team { Name = "A" }, Player = new Koralytics.Domain.Entities.Player.Player { FirstName = "C", LastName = "D" } }
            };
            var eventQueryable = events.BuildMock();
            var eventRepo = new Mock<IRepository<MatchEventEntity>>();
            eventRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(eventQueryable);
            SetupRepository(eventRepo);

            var dtoList = new List<MatchEventResponseDto>
            {
                new MatchEventResponseDto { Id = 2 },
                new MatchEventResponseDto { Id = 1 }
            };
            _mapperMock.Setup(m => m.Map<List<MatchEventResponseDto>>(It.IsAny<List<MatchEventEntity>>())).Returns(dtoList);

            var result = await _service.GetMatchTimelineAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.MatchId);
            Assert.Equal(2, result.Events.Count);
        }

        [Fact]
        public async Task GetMatchTimelineAsync_EmptyTimeline_ReturnsEmptyEvents()
        {
            var matchRepo = new Mock<IRepository<MatchEntity>>();
            matchRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .ReturnsAsync(true);
            SetupRepository(matchRepo);

            var events = new List<MatchEventEntity>();
            var eventQueryable = events.BuildMock();
            var eventRepo = new Mock<IRepository<MatchEventEntity>>();
            eventRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(eventQueryable);
            SetupRepository(eventRepo);

            _mapperMock.Setup(m => m.Map<List<MatchEventResponseDto>>(It.IsAny<List<MatchEventEntity>>()))
                .Returns(new List<MatchEventResponseDto>());

            var result = await _service.GetMatchTimelineAsync(1);

            Assert.Empty(result.Events);
        }

        #endregion
    }
}
