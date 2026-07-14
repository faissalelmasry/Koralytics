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
using Koralytics.Domain.Entities.Coach;
using Koralytics.Domain.Entities.Match;
using DomainEnums = Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using AcademyEntity = Koralytics.Domain.Entities.Academy.Academy;

namespace Koralytics.Application.UnitTests.Match
{
    public class MatchRequestServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<MatchRequestService>> _loggerMock;
        private readonly Mock<IMatchService> _matchServiceMock;
        private readonly MatchRequestService _service;

        public MatchRequestServiceTests()
        {
            _unitOfWorkMock = new();
            _mapperMock = new();
            _loggerMock = new();
            _matchServiceMock = new();
            _service = new MatchRequestService(
                _unitOfWorkMock.Object, _mapperMock.Object, _loggerMock.Object, _matchServiceMock.Object);
        }

        private void SetupRepository<T>(Mock<IRepository<T>> repo) where T : class, Koralytics.Domain.Interfaces.ISoftDelete
        {
            _unitOfWorkMock.Setup(u => u.Repository<T>()).Returns(repo.Object);
        }

        #region RequestFriendlyMatchAsync

        [Fact]
        public async Task RequestFriendlyMatchAsync_SameTeam_ThrowsBadRequestException()
        {
            var dto = new CreateMatchRequestDto
            {
                RequesterTeamId = 1, TargetTeamId = 1, Format = DomainEnums.MatchFormat.ElevenSide,
                ProposedDate = DateTime.UtcNow
            };

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.RequestFriendlyMatchAsync(100, dto));
        }

        [Fact]
        public async Task RequestFriendlyMatchAsync_RequesterTeamNotFound_ThrowsNotFoundException()
        {
            var dto = new CreateMatchRequestDto
            {
                RequesterTeamId = 1, TargetTeamId = 2, Format = DomainEnums.MatchFormat.ElevenSide,
                ProposedDate = DateTime.UtcNow
            };
            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync((Team?)null);
            SetupRepository(teamRepo);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.RequestFriendlyMatchAsync(100, dto));
        }

        [Fact]
        public async Task RequestFriendlyMatchAsync_TargetTeamNotFound_ThrowsNotFoundException()
        {
            var dto = new CreateMatchRequestDto
            {
                RequesterTeamId = 1, TargetTeamId = 2, Format = DomainEnums.MatchFormat.ElevenSide,
                ProposedDate = DateTime.UtcNow
            };
            var requesterTeam = new Team { Id = 1, Name = "Req", AcademyId = 10 };
            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.Is<Expression<Func<Team, bool>>>(e =>
                e.Compile()(requesterTeam))))
                .ReturnsAsync(requesterTeam);
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.Is<Expression<Func<Team, bool>>>(e =>
                !e.Compile()(requesterTeam))))
                .ReturnsAsync((Team?)null);
            SetupRepository(teamRepo);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.RequestFriendlyMatchAsync(100, dto));
        }

        [Fact]
        public async Task RequestFriendlyMatchAsync_NotCoachOrAdmin_ThrowsForbiddenException()
        {
            var dto = new CreateMatchRequestDto
            {
                RequesterTeamId = 1, TargetTeamId = 2, Format = DomainEnums.MatchFormat.ElevenSide,
                ProposedDate = DateTime.UtcNow
            };
            var requesterTeam = new Team { Id = 1, Name = "Req", AcademyId = 10 };
            var targetTeam = new Team { Id = 2, Name = "Target", AcademyId = 20 };

            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync((Team?)null);
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.Is<Expression<Func<Team, bool>>>(e =>
                e.Compile()(requesterTeam))))
                .ReturnsAsync(requesterTeam);
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.Is<Expression<Func<Team, bool>>>(e =>
                e.Compile()(targetTeam))))
                .ReturnsAsync(targetTeam);
            SetupRepository(teamRepo);

            var coachTeamRepo = new Mock<IRepository<CoachTeam>>();
            coachTeamRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<CoachTeam, bool>>>()))
                .ReturnsAsync(false);
            SetupRepository(coachTeamRepo);

            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            academyRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<AcademyEntity, bool>>>()))
                .ReturnsAsync(false);
            SetupRepository(academyRepo);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                _service.RequestFriendlyMatchAsync(100, dto));
        }

        [Fact]
        public async Task RequestFriendlyMatchAsync_DuplicatePendingRequest_ThrowsBadRequestException()
        {
            var dto = new CreateMatchRequestDto
            {
                RequesterTeamId = 1, TargetTeamId = 2, Format = DomainEnums.MatchFormat.ElevenSide,
                ProposedDate = DateTime.UtcNow
            };
            var requesterTeam = new Team { Id = 1, Name = "Req", AcademyId = 10 };
            var targetTeam = new Team { Id = 2, Name = "Target", AcademyId = 20 };

            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync((Team?)null);
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.Is<Expression<Func<Team, bool>>>(e =>
                e.Compile()(requesterTeam))))
                .ReturnsAsync(requesterTeam);
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.Is<Expression<Func<Team, bool>>>(e =>
                e.Compile()(targetTeam))))
                .ReturnsAsync(targetTeam);
            SetupRepository(teamRepo);

            var coachTeamRepo = new Mock<IRepository<CoachTeam>>();
            coachTeamRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<CoachTeam, bool>>>()))
                .ReturnsAsync(true);
            SetupRepository(coachTeamRepo);

            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            SetupRepository(academyRepo);

            var matchRequestRepo = new Mock<IRepository<MatchRequest>>();
            matchRequestRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<MatchRequest, bool>>>()))
                .ReturnsAsync(true);
            SetupRepository(matchRequestRepo);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.RequestFriendlyMatchAsync(100, dto));
        }

        [Fact]
        public async Task RequestFriendlyMatchAsync_ValidRequest_CreatesAndReturnsDto()
        {
            var dto = new CreateMatchRequestDto
            {
                RequesterTeamId = 1, TargetTeamId = 2, Format = DomainEnums.MatchFormat.ElevenSide,
                ProposedDate = DateTime.UtcNow, Location = "Stadium"
            };
            var requesterTeam = new Team { Id = 1, Name = "Req", AcademyId = 10 };
            var targetTeam = new Team { Id = 2, Name = "Target", AcademyId = 20 };

            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync((Team?)null);
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.Is<Expression<Func<Team, bool>>>(e =>
                e.Compile()(requesterTeam))))
                .ReturnsAsync(requesterTeam);
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.Is<Expression<Func<Team, bool>>>(e =>
                e.Compile()(targetTeam))))
                .ReturnsAsync(targetTeam);
            SetupRepository(teamRepo);

            var coachTeamRepo = new Mock<IRepository<CoachTeam>>();
            coachTeamRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<CoachTeam, bool>>>()))
                .ReturnsAsync(true);
            SetupRepository(coachTeamRepo);

            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            academyRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<AcademyEntity, bool>>>()))
                .ReturnsAsync(true);
            SetupRepository(academyRepo);

            var matchRequestRepo = new Mock<IRepository<MatchRequest>>();
            matchRequestRepo.Setup(r => r.ExistsAsync(It.Is<Expression<Func<MatchRequest, bool>>>(e =>
                e.Compile()(new MatchRequest { Status = DomainEnums.MatchRequestStatus.Pending }))))
                .ReturnsAsync(false);
            SetupRepository(matchRequestRepo);

            var createdRequest = new MatchRequest
            {
                Id = 1, RequesterTeamId = 1, TargetTeamId = 2, RequesterTeam = requesterTeam,
                TargetTeam = targetTeam, RequesterCoach = new Coach { FirstName = "C", LastName = "C" }
            };
            var requests = new List<MatchRequest> { createdRequest };
            var requestQueryable = requests.BuildMock();
            matchRequestRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(requestQueryable);

            var responseDto = new MatchRequestResponseDto { Id = 1 };
            _mapperMock.Setup(m => m.Map<MatchRequestResponseDto>(It.IsAny<MatchRequest>())).Returns(responseDto);

            var result = await _service.RequestFriendlyMatchAsync(100, dto);

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            matchRequestRepo.Verify(r => r.AddAsync(It.IsAny<MatchRequest>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(1));
        }

        #endregion

        #region AcceptMatchRequestAsync

        [Fact]
        public async Task AcceptMatchRequestAsync_RequestNotFound_ThrowsNotFoundException()
        {
            var requests = new List<MatchRequest>();
            var queryable = requests.BuildMock();
            var requestRepo = new Mock<IRepository<MatchRequest>>();
            requestRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(requestRepo);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.AcceptMatchRequestAsync(1, 100));
        }

        [Fact]
        public async Task AcceptMatchRequestAsync_NotPending_ThrowsBadRequestException()
        {
            var request = new MatchRequest { Id = 1, Status = DomainEnums.MatchRequestStatus.Accepted };
            var requests = new List<MatchRequest> { request };
            var queryable = requests.BuildMock();
            var requestRepo = new Mock<IRepository<MatchRequest>>();
            requestRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(requestRepo);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.AcceptMatchRequestAsync(1, 100));
        }

        [Fact]
        public async Task AcceptMatchRequestAsync_NotCoachOrAdminOfTargetTeam_ThrowsForbiddenException()
        {
            var request = new MatchRequest
            {
                Id = 1, Status = DomainEnums.MatchRequestStatus.Pending,
                RequesterTeamId = 1, TargetTeamId = 2,
                RequesterTeam = new Team { Id = 1, AcademyId = 10 },
                TargetTeam = new Team { Id = 2, AcademyId = 20 }
            };
            var requests = new List<MatchRequest> { request };
            var queryable = requests.BuildMock();
            var requestRepo = new Mock<IRepository<MatchRequest>>();
            requestRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(requestRepo);

            var targetTeam = new Team { Id = 2, Name = "Target", AcademyId = 20 };
            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync(targetTeam);
            SetupRepository(teamRepo);

            var coachTeamRepo = new Mock<IRepository<CoachTeam>>();
            coachTeamRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<CoachTeam, bool>>>()))
                .ReturnsAsync(false);
            SetupRepository(coachTeamRepo);

            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            academyRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<AcademyEntity, bool>>>()))
                .ReturnsAsync(false);
            SetupRepository(academyRepo);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                _service.AcceptMatchRequestAsync(1, 100));
        }

        [Fact]
        public async Task AcceptMatchRequestAsync_ValidRequest_CreatesMatchAndUpdatesRequest()
        {
            var request = new MatchRequest
            {
                Id = 1, Status = DomainEnums.MatchRequestStatus.Pending,
                RequesterTeamId = 1, TargetTeamId = 2,
                Format = DomainEnums.MatchFormat.ElevenSide, ProposedDate = DateTime.UtcNow,
                RequesterTeam = new Team { Id = 1, AcademyId = 10 },
                TargetTeam = new Team { Id = 2, AcademyId = 20, Name = "Target" }
            };
            var requests = new List<MatchRequest> { request };
            var queryable = requests.BuildMock();
            var requestRepo = new Mock<IRepository<MatchRequest>>();
            requestRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(requestRepo);

            var targetTeam = new Team { Id = 2, Name = "Target", AcademyId = 20 };
            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync(targetTeam);
            SetupRepository(teamRepo);

            var coachTeamRepo = new Mock<IRepository<CoachTeam>>();
            coachTeamRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<CoachTeam, bool>>>()))
                .ReturnsAsync(true);
            SetupRepository(coachTeamRepo);

            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            SetupRepository(academyRepo);

            var transactionMock = new Mock<IDbContextTransaction>();
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(transactionMock.Object);

            var matchResponse = new MatchResponseDto { Id = 200 };
            _matchServiceMock.Setup(m => m.CreateFriendlyMatchAsync(It.IsAny<CreateFriendlyMatchDto>()))
                .ReturnsAsync(matchResponse);

            var result = await _service.AcceptMatchRequestAsync(1, 100);

            Assert.NotNull(result);
            Assert.Equal(200, result.Id);
            Assert.Equal(DomainEnums.MatchRequestStatus.Accepted, request.Status);
            Assert.Equal(100, request.ResolvedByCoachId);
            Assert.Equal(200, request.MatchId);
            transactionMock.Verify(t => t.CommitAsync(default), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region DeclineMatchRequestAsync

        [Fact]
        public async Task DeclineMatchRequestAsync_RequestNotFound_ThrowsNotFoundException()
        {
            var requests = new List<MatchRequest>();
            var queryable = requests.BuildMock();
            var requestRepo = new Mock<IRepository<MatchRequest>>();
            requestRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(requestRepo);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.DeclineMatchRequestAsync(1, 100));
        }

        [Fact]
        public async Task DeclineMatchRequestAsync_NotPending_ThrowsBadRequestException()
        {
            var request = new MatchRequest { Id = 1, Status = DomainEnums.MatchRequestStatus.Declined };
            var requests = new List<MatchRequest> { request };
            var queryable = requests.BuildMock();
            var requestRepo = new Mock<IRepository<MatchRequest>>();
            requestRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(requestRepo);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.DeclineMatchRequestAsync(1, 100));
        }

        [Fact]
        public async Task DeclineMatchRequestAsync_NotCoachOrAdminOfTargetTeam_ThrowsForbiddenException()
        {
            var request = new MatchRequest
            {
                Id = 1, Status = DomainEnums.MatchRequestStatus.Pending,
                RequesterTeamId = 1, TargetTeamId = 2
            };
            var requests = new List<MatchRequest> { request };
            var queryable = requests.BuildMock();
            var requestRepo = new Mock<IRepository<MatchRequest>>();
            requestRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(requestRepo);

            var targetTeam = new Team { Id = 2, Name = "Target", AcademyId = 20 };
            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync(targetTeam);
            SetupRepository(teamRepo);

            var coachTeamRepo = new Mock<IRepository<CoachTeam>>();
            coachTeamRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<CoachTeam, bool>>>()))
                .ReturnsAsync(false);
            SetupRepository(coachTeamRepo);

            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            academyRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<AcademyEntity, bool>>>()))
                .ReturnsAsync(false);
            SetupRepository(academyRepo);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                _service.DeclineMatchRequestAsync(1, 100));
        }

        [Fact]
        public async Task DeclineMatchRequestAsync_ValidRequest_SetsDeclinedAndSaves()
        {
            var request = new MatchRequest
            {
                Id = 1, Status = DomainEnums.MatchRequestStatus.Pending,
                RequesterTeamId = 1, TargetTeamId = 2
            };
            var requests = new List<MatchRequest> { request };
            var queryable = requests.BuildMock();
            var requestRepo = new Mock<IRepository<MatchRequest>>();
            requestRepo.Setup(r => r.GetQueryable()).Returns(queryable);
            SetupRepository(requestRepo);

            var targetTeam = new Team { Id = 2, Name = "Target", AcademyId = 20 };
            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.FindAsNoTrackingAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync(targetTeam);
            SetupRepository(teamRepo);

            var coachTeamRepo = new Mock<IRepository<CoachTeam>>();
            coachTeamRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<CoachTeam, bool>>>()))
                .ReturnsAsync(true);
            SetupRepository(coachTeamRepo);

            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            SetupRepository(academyRepo);

            await _service.DeclineMatchRequestAsync(1, 100);

            Assert.Equal(DomainEnums.MatchRequestStatus.Declined, request.Status);
            Assert.Equal(100, request.ResolvedByCoachId);
            Assert.NotNull(request.ResolvedAt);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region GetPendingRequestsAsync

        [Fact]
        public async Task GetPendingRequestsAsync_TeamNotFound_ThrowsNotFoundException()
        {
            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync(false);
            SetupRepository(teamRepo);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.GetPendingRequestsAsync(1));
        }

        [Fact]
        public async Task GetPendingRequestsAsync_ReturnsPendingRequests()
        {
            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync(true);
            SetupRepository(teamRepo);

            var requests = new List<MatchRequest>
            {
                new MatchRequest { Id = 1, TargetTeamId = 1, Status = DomainEnums.MatchRequestStatus.Pending, CreatedAt = DateTime.UtcNow,
                    RequesterTeam = new Team(), TargetTeam = new Team(), RequesterCoach = new Coach() },
                new MatchRequest { Id = 2, TargetTeamId = 1, Status = DomainEnums.MatchRequestStatus.Pending, CreatedAt = DateTime.UtcNow.AddDays(-1),
                    RequesterTeam = new Team(), TargetTeam = new Team(), RequesterCoach = new Coach() }
            };
            var queryable = requests.BuildMock();
            var requestRepo = new Mock<IRepository<MatchRequest>>();
            requestRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            SetupRepository(requestRepo);

            var dtoList = requests.Select(r => new MatchRequestResponseDto { Id = r.Id }).ToList();
            _mapperMock.Setup(m => m.Map<List<MatchRequestResponseDto>>(It.IsAny<List<MatchRequest>>())).Returns(dtoList);

            var result = await _service.GetPendingRequestsAsync(1);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        #endregion

        #region GetSentRequestsAsync

        [Fact]
        public async Task GetSentRequestsAsync_TeamNotFound_ThrowsNotFoundException()
        {
            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync(false);
            SetupRepository(teamRepo);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.GetSentRequestsAsync(1));
        }

        [Fact]
        public async Task GetSentRequestsAsync_ReturnsSentRequests()
        {
            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync(true);
            SetupRepository(teamRepo);

            var requests = new List<MatchRequest>
            {
                new MatchRequest { Id = 1, RequesterTeamId = 1, CreatedAt = DateTime.UtcNow,
                    RequesterTeam = new Team(), TargetTeam = new Team(), RequesterCoach = new Coach() }
            };
            var queryable = requests.BuildMock();
            var requestRepo = new Mock<IRepository<MatchRequest>>();
            requestRepo.Setup(r => r.GetQueryableAsNoTracking()).Returns(queryable);
            SetupRepository(requestRepo);

            var dtoList = requests.Select(r => new MatchRequestResponseDto { Id = r.Id }).ToList();
            _mapperMock.Setup(m => m.Map<List<MatchRequestResponseDto>>(It.IsAny<List<MatchRequest>>())).Returns(dtoList);

            var result = await _service.GetSentRequestsAsync(1);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }

        #endregion
    }
}
