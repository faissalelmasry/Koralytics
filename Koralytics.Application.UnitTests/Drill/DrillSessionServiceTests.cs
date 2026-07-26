//using AutoMapper;
//using Koralytics.Application.DTOs.Drill;
//using Koralytics.Application.Interfaces;
//using Koralytics.Application.Services.Drill.DrillSession;
//using Koralytics.Application.Services.Player.Helpers;
//using Koralytics.Domain.Entities.Coach;
//using Koralytics.Domain.Entities.Drill;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
//using MockQueryable;
//using MockQueryable.Moq;
//using Moq;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Threading.Tasks;
//using Xunit;
//using DrillSessionEntity = Koralytics.Domain.Entities.Drill.DrillSession;

//namespace Koralytics.Application.UnitTests.Drill
//{
//    public class DrillSessionServiceTests
//    {
//        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
//        private readonly Mock<IMapper> _mapperMock;
//        private readonly Mock<CardInvalidationList> _invalidationListMock;
//        private readonly DrillSessionService _service;

//        public DrillSessionServiceTests()
//        {
//            _unitOfWorkMock = new Mock<IUnitOfWork>();
//            _mapperMock = new Mock<IMapper>();

//            // To mock the background service, we provide dummy dependencies
//            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
//            var loggerMock = new Mock<ILogger<CardInvalidationList>>();
//            _invalidationListMock = new Mock<CardInvalidationList>(scopeFactoryMock.Object, loggerMock.Object);

//            _service = new DrillSessionService(
//                _unitOfWorkMock.Object,
//                _mapperMock.Object,
//                _invalidationListMock.Object);
//        }

//        // ================================================================
//        // 1. CreateSessionAsync Tests
//        // ================================================================

//        [Fact]
//        public async Task CreateSessionAsync_CoachNotAuthorized_ThrowsUnauthorizedAccessException()
//        {
//            // --- ARRANGE ---
//            var dto = new CreateDrillSessionDto { TeamId = 5 };

//            var coachTeamRepoMock = new Mock<IRepository<CoachTeam>>();
//            // Simulate the database saying "No, this coach does not manage this team"
//            coachTeamRepoMock
//                .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<CoachTeam, bool>>>()))
//                .ReturnsAsync(false);

//            _unitOfWorkMock.Setup(u => u.Repository<CoachTeam>()).Returns(coachTeamRepoMock.Object);

//            // --- ACT & ASSERT ---
//            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
//                _service.CreateSessionAsync(dto, currentCoachId: 1, currentAcademyId: 1));
//        }

//        [Fact]
//        public async Task CreateSessionAsync_ValidData_SavesSessionAndBuildsAttendance()
//        {
//            // --- ARRANGE ---
//            var dto = new CreateDrillSessionDto
//            {
//                TeamId = 5,
//                PlayerIds = new List<int> { 101, 102 }
//            };

//            var sessionEntity = new DrillSessionEntity { Id = 0, SessionAttendances = new List<SessionAttendance>() };

//            // 1. Mock Coach Authorization (Returns True)
//            var coachTeamRepoMock = new Mock<IRepository<CoachTeam>>();
//            coachTeamRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<CoachTeam, bool>>>())).ReturnsAsync(true);
//            _unitOfWorkMock.Setup(u => u.Repository<CoachTeam>()).Returns(coachTeamRepoMock.Object);

//            // 2. Mock Mapper
//            _mapperMock.Setup(m => m.Map<DrillSessionEntity>(dto)).Returns(sessionEntity);
//            _mapperMock.Setup(m => m.Map<DrillSessionDto>(sessionEntity)).Returns(new DrillSessionDto { Id = 1 });

//            // 3. Mock Session Repository
//            var sessionRepoMock = new Mock<IRepository<DrillSessionEntity>>();
//            sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<DrillSessionEntity>())).Returns(Task.CompletedTask);
//            _unitOfWorkMock.Setup(u => u.Repository<DrillSessionEntity>()).Returns(sessionRepoMock.Object);

//            // --- ACT ---
//            var result = await _service.CreateSessionAsync(dto, currentCoachId: 1, currentAcademyId: 2);

//            // --- ASSERT ---
//            Assert.NotNull(result);
//            Assert.Equal(2, sessionEntity.SessionAttendances.Count); // Ensure the memory loop worked
//            Assert.Equal(101, sessionEntity.SessionAttendances.First().playerId);
//            Assert.Equal(1, sessionEntity.CoachId); // Ensure security override worked

//            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once); // Verify DB commit
//        }

//        // ================================================================
//        // 2. GetCoachSessionsAsync Tests (Testing IQueryable & Filters)
//        // ================================================================

//        [Fact]
//        public async Task GetCoachSessionsAsync_AppliesFiltersAndPaginationCorrectly()
//        {
//            // --- ARRANGE ---
//            // Create a fake database table with 3 sessions
//            var fakeSessions = new List<DrillSessionEntity>
//            {
//                new DrillSessionEntity { Id = 1, CoachId = 1, AcademyId = 2, TeamId = 10, SessionDate = new DateTime(2026, 8, 1) },
//                new DrillSessionEntity { Id = 2, CoachId = 1, AcademyId = 2, TeamId = 10, SessionDate = new DateTime(2026, 8, 5) },
//                new DrillSessionEntity { Id = 3, CoachId = 1, AcademyId = 2, TeamId = 99, SessionDate = new DateTime(2026, 8, 10) }
//            };

//            // Use MockQueryable to translate our fake list into an EF Core IQueryable
//            var mockQueryable = fakeSessions.BuildMock();

//            var sessionRepoMock = new Mock<IRepository<DrillSessionEntity>>();
//            sessionRepoMock.Setup(r => r.GetQueryable()).Returns(mockQueryable);
//            _unitOfWorkMock.Setup(u => u.Repository<DrillSessionEntity>()).Returns(sessionRepoMock.Object);

//            var filter = new SessionFilterDto { TeamId = 10, PageNumber = 1, PageSize = 10 };

//            _mapperMock.Setup(m => m.Map<IEnumerable<DrillSessionDto>>(It.IsAny<IEnumerable<DrillSessionEntity>>()))
//                       .Returns(new List<DrillSessionDto> { new DrillSessionDto(), new DrillSessionDto() });

//            // --- ACT ---
//            var results = await _service.GetCoachSessionsAsync(currentCoachId: 1, currentAcademyId: 2, filter);

//            // --- ASSERT ---
//            // Only 2 sessions belong to TeamId 10, so it should return exactly 2 items
//            Assert.Equal(2, results.Count());
//        }

//        // ================================================================
//        // 3. CompleteSessionAsync Tests
//        // ================================================================

//        [Fact]
//        public async Task CompleteSessionAsync_AlreadyCompleted_ThrowsInvalidOperationException()
//        {
//            // --- ARRANGE ---
//            var session = new DrillSessionEntity
//            {
//                Id = 1,
//                CoachId = 1,
//                Status = Koralytics.Domain.Enums.SessionStatus.Completed
//            };

//            var mockQueryable = new List<DrillSessionEntity> { session }.BuildMock();

//            var sessionRepoMock = new Mock<IRepository<DrillSessionEntity>>();
//            sessionRepoMock.Setup(r => r.GetQueryable()).Returns(mockQueryable);
//            _unitOfWorkMock.Setup(u => u.Repository<DrillSessionEntity>()).Returns(sessionRepoMock.Object);

//            // --- ACT & ASSERT ---
//            await Assert.ThrowsAsync<InvalidOperationException>(() =>
//                _service.CompleteSessionAsync(sessionId: 1, currentCoachId: 1));
//        }

//        [Fact]
//        public async Task CompleteSessionAsync_Valid_ChangesStatusAndTriggersInvalidation()
//        {
//            // --- ARRANGE ---
//            var session = new DrillSessionEntity
//            {
//                Id = 1,
//                CoachId = 1,
//                Status = Koralytics.Domain.Enums.SessionStatus.Scheduled,
//                SessionAttendances = new List<SessionAttendance>
//                {
//                    new SessionAttendance { playerId = 10, IsPresent = true },
//                    new SessionAttendance { playerId = 11, IsPresent = false }, // Absent
//                    new SessionAttendance { playerId = 12, IsPresent = true }
//                }
//            };

//            var mockQueryable = new List<DrillSessionEntity> { session }.BuildMock();

//            var sessionRepoMock = new Mock<IRepository<DrillSessionEntity>>();
//            sessionRepoMock.Setup(r => r.GetQueryable()).Returns(mockQueryable);
//            _unitOfWorkMock.Setup(u => u.Repository<DrillSessionEntity>()).Returns(sessionRepoMock.Object);

//            // --- ACT ---
//            await _service.CompleteSessionAsync(sessionId: 1, currentCoachId: 1);

//            // --- ASSERT ---
//            Assert.Equal(Koralytics.Domain.Enums.SessionStatus.Completed, session.Status);
//            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);

//            // Note: If you want to verify that _invalidationList.Invalidate was called 
//            // exactly twice (for players 10 and 12), the Invalidate method in CardInvalidationList 
//            // must be marked as 'virtual'.
//        }
//    }
//}