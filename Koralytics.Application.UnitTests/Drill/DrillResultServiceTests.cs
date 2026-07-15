using AutoMapper;
using Koralytics.Application.DTOs.Drill;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Services.Drill.DrillResult;
using Koralytics.Domain.Entities.Drill;
using Koralytics.Domain.Entities.Player;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;
using DrillSessionEntity = Koralytics.Domain.Entities.Drill.DrillSession;

namespace Koralytics.Application.UnitTests.Drill
{
    public class DrillResultServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly DrillResultService _service;

        public DrillResultServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();

            _service = new DrillResultService(
                _unitOfWorkMock.Object,
                _mapperMock.Object);
        }

        // ================================================================
        // SubmitResultsAsync Tests
        // ================================================================

        [Fact]
        public async Task SubmitResultsAsync_PlayerIsAbsent_ThrowsInvalidOperationException()
        {
            // --- ARRANGE ---
            // 1. Mock Session (Owned by Coach 1)
            var session = new DrillSessionEntity { Id = 1, CoachId = 1 };
            var sessionRepoMock = new Mock<IRepository<DrillSessionEntity>>();
            sessionRepoMock.Setup(r => r.GetByIdAsNoTrackingAsync(1)).ReturnsAsync(session);
            _unitOfWorkMock.Setup(u => u.Repository<DrillSessionEntity>()).Returns(sessionRepoMock.Object);

            // 2. Mock Drill (Belongs to Session 1)
            var drill = new Koralytics.Domain.Entities.Drill.Drill { Id = 10, SessionId = 1 };
            var drillRepoMock = new Mock<IRepository<Koralytics.Domain.Entities.Drill.Drill>>();
            drillRepoMock.Setup(r => r.GetByIdAsNoTrackingAsync(10)).ReturnsAsync(drill);
            _unitOfWorkMock.Setup(u => u.Repository<Koralytics.Domain.Entities.Drill.Drill>()).Returns(drillRepoMock.Object);

            // 3. Mock Attendance (Player 99 is ABSENT)
            var attendanceList = new List<SessionAttendance>
            {
                new SessionAttendance { playerId = 99, IsPresent = false } // ABSENT!
            };
            var attendanceRepoMock = new Mock<IRepository<SessionAttendance>>();
            attendanceRepoMock.Setup(r => r.FindAllAsNoTrackingAsync(It.IsAny<Expression<Func<SessionAttendance, bool>>>()))
                              .ReturnsAsync(attendanceList);
            _unitOfWorkMock.Setup(u => u.Repository<SessionAttendance>()).Returns(attendanceRepoMock.Object);

            // Add this right here!
            var resultRepoMock = new Mock<IRepository<Domain.Entities.Drill.DrillResult>>();
            resultRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Domain.Entities.Drill.DrillResult>().BuildMock());
            _unitOfWorkMock.Setup(u => u.Repository<Domain.Entities.Drill.DrillResult>()).Returns(resultRepoMock.Object);

            // 4. Create the payload trying to score the absent player
            var dto = new SubmitDrillResultsDto
            {
                Results = new List<PlayerDrillResultDto>
                {
                    new PlayerDrillResultDto { PlayerId = 99, ManualScore = 85 }
                }
            };

            // --- ACT & ASSERT ---
            // Verify the "Ghost Rule" triggers
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.SubmitResultsAsync(sessionId: 1, drillId: 10, dto, currentCoachId: 1));

            Assert.Contains("is not present", exception.Message);
        }

        [Fact]
        public async Task SubmitResultsAsync_SuccessOrMissedMath_CalculatesCorrectly()
        {
            // --- ARRANGE ---
            var session = new DrillSessionEntity { Id = 1, CoachId = 1 };
            var sessionRepoMock = new Mock<IRepository<DrillSessionEntity>>();
            sessionRepoMock.Setup(r => r.GetByIdAsNoTrackingAsync(1)).ReturnsAsync(session);
            _unitOfWorkMock.Setup(u => u.Repository<DrillSessionEntity>()).Returns(sessionRepoMock.Object);

            // Mock Drill as 'SuccessOrMissed' Mode
            var drill = new Koralytics.Domain.Entities.Drill.Drill
            {
                Id = 10,
                SessionId = 1,
                Mode = Koralytics.Domain.Enums.DrillMode.SuccessOrMissed
            };
            var drillRepoMock = new Mock<IRepository<Koralytics.Domain.Entities.Drill.Drill>>();
            drillRepoMock.Setup(r => r.GetByIdAsNoTrackingAsync(10)).ReturnsAsync(drill);
            _unitOfWorkMock.Setup(u => u.Repository<Koralytics.Domain.Entities.Drill.Drill>()).Returns(drillRepoMock.Object);

            // Mock Attendance (Player 99 is PRESENT)
            var attendanceList = new List<SessionAttendance> { new SessionAttendance { playerId = 99, IsPresent = true } };
            var attendanceRepoMock = new Mock<IRepository<SessionAttendance>>();
            attendanceRepoMock.Setup(r => r.FindAllAsNoTrackingAsync(It.IsAny<Expression<Func<SessionAttendance, bool>>>()))
                              .ReturnsAsync(attendanceList);
            _unitOfWorkMock.Setup(u => u.Repository<SessionAttendance>()).Returns(attendanceRepoMock.Object);

            // Mock Results Repo (Empty, so it triggers an INSERT)
            var resultRepoMock = new Mock<IRepository<Domain.Entities.Drill.DrillResult>>();
            var emptyResults = new List<Domain.Entities.Drill.DrillResult>().BuildMock();
            resultRepoMock.Setup(r => r.GetQueryable()).Returns(emptyResults);

            // Capture the inserted result to verify the math
            Domain.Entities.Drill.DrillResult capturedResult = null;
            resultRepoMock.Setup(r => r.AddAsync(It.IsAny<Domain.Entities.Drill.DrillResult>()))
                          .Callback<Domain.Entities.Drill.DrillResult>(r => capturedResult = r)
                          .Returns(Task.CompletedTask);

            _unitOfWorkMock.Setup(u => u.Repository<Domain.Entities.Drill.DrillResult>()).Returns(resultRepoMock.Object);

            // 8 Done, 2 Missed = 80% Success Rate = 8.0 Score out of 10
            var dto = new SubmitDrillResultsDto
            {
                Results = new List<PlayerDrillResultDto>
                {
                    new PlayerDrillResultDto { PlayerId = 99, DoneCount = 8, MissedCount = 2 }
                }
            };

            // --- ACT ---
            await _service.SubmitResultsAsync(sessionId: 1, drillId: 10, dto, currentCoachId: 1);

            // --- ASSERT ---
            Assert.NotNull(capturedResult);
            Assert.Equal(8.0m, capturedResult.FinalScore); // The math works!
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // ================================================================
        // GetPlayerDrillProgressionAsync Tests
        // ================================================================

        [Fact]
        public async Task GetPlayerDrillProgressionAsync_OrdersByDateCorrectly()
        {
            // --- ARRANGE ---
            var playerRepoMock = new Mock<IRepository<Koralytics.Domain.Entities.Player.Player>>();
            playerRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Koralytics.Domain.Entities.Player.Player, bool>>>())).ReturnsAsync(true);
            _unitOfWorkMock.Setup(u => u.Repository<Koralytics.Domain.Entities.Player.Player>()).Returns(playerRepoMock.Object);

            // Create scrambled dates
            var rawResults = new List<Domain.Entities.Drill.DrillResult>
            {
                new Domain.Entities.Drill.DrillResult { PlayerId = 1, FinalScore = 6, Drill = new Koralytics.Domain.Entities.Drill.Drill { DrillSession = new DrillSessionEntity { SessionDate = new DateTime(2026, 3, 1) }, DrillTemplate = new DrillTemplate { CategoryId = 5, Name = "Pass", DrillCategory = new DrillCategory { Name = "Passing" } } } },
                new Domain.Entities.Drill.DrillResult { PlayerId = 1, FinalScore = 9, Drill = new Koralytics.Domain.Entities.Drill.Drill { DrillSession = new DrillSessionEntity { SessionDate = new DateTime(2026, 1, 1) }, DrillTemplate = new DrillTemplate { CategoryId = 5, Name = "Pass", DrillCategory = new DrillCategory { Name = "Passing" } } } },
                new Domain.Entities.Drill.DrillResult { PlayerId = 1, FinalScore = 7, Drill = new Koralytics.Domain.Entities.Drill.Drill { DrillSession = new DrillSessionEntity { SessionDate = new DateTime(2026, 2, 1) }, DrillTemplate = new DrillTemplate { CategoryId = 5, Name = "Pass", DrillCategory = new DrillCategory { Name = "Passing" } } } }
            };

            var resultRepoMock = new Mock<IRepository<Domain.Entities.Drill.DrillResult>>();
            resultRepoMock.Setup(r => r.GetQueryableAsNoTracking()).Returns(rawResults.BuildMock());
            _unitOfWorkMock.Setup(u => u.Repository<Domain.Entities.Drill.DrillResult>()).Returns(resultRepoMock.Object);

            // --- ACT ---
            var result = await _service.GetPlayerDrillProgressionAsync(playerId: 1, categoryId: 5, currentAcademyId: 1);

            // --- ASSERT ---
            Assert.Equal(3, result.ProgressionChart.Count);
            // Verify the chart data goes chronologically (Jan, Feb, Mar)
            Assert.Equal(new DateTime(2026, 1, 1), result.ProgressionChart[0].SessionDate);
            Assert.Equal(new DateTime(2026, 2, 1), result.ProgressionChart[1].SessionDate);
            Assert.Equal(new DateTime(2026, 3, 1), result.ProgressionChart[2].SessionDate);
        }
    }
}