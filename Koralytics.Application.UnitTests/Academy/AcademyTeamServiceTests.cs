using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using Koralytics.Application.DTOs.Academies;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Services.Academy.AcademyTeamService;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Coach;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Xunit;
using AcademyEntity = Koralytics.Domain.Entities.Academy.Academy;

namespace Koralytics.Application.UnitTests.Academies
{
    public class AcademyTeamServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<AcademyTeamService>> _loggerMock;
        private readonly AcademyTeamService _service;

        public AcademyTeamServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<AcademyTeamService>>();

            _service = new AcademyTeamService(
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        // --- CreateAgeGroupAsync ---

        [Fact]
        public async Task CreateAgeGroupAsync_AcademyNotFound_ThrowsNotFoundException()
        {
            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            academyRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AcademyEntity, bool>>>()))
                .ReturnsAsync((AcademyEntity?)null);
            _unitOfWorkMock.Setup(u => u.Repository<AcademyEntity>()).Returns(academyRepo.Object);

            var dto = new CreateAgeGroupDto { Name = "U10", MinAge = 8, MaxAge = 10 };
            
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.CreateAgeGroupAsync(1, dto, 100));
        }

        [Fact]
        public async Task CreateAgeGroupAsync_MinAgeGreaterThanMax_ThrowsBadRequestException()
        {
            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            academyRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AcademyEntity, bool>>>()))
                .ReturnsAsync(new AcademyEntity { Id = 1 });
            _unitOfWorkMock.Setup(u => u.Repository<AcademyEntity>()).Returns(academyRepo.Object);

            var dto = new CreateAgeGroupDto { Name = "U10", MinAge = 10, MaxAge = 8 };
            
            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.CreateAgeGroupAsync(1, dto, 100));
        }

        [Fact]
        public async Task CreateAgeGroupAsync_OverlappingAgeRange_ThrowsConflictException()
        {
            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            academyRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AcademyEntity, bool>>>()))
                .ReturnsAsync(new AcademyEntity { Id = 1 });
            _unitOfWorkMock.Setup(u => u.Repository<AcademyEntity>()).Returns(academyRepo.Object);

            var ageGroupRepo = new Mock<IRepository<AgeGroup>>();
            ageGroupRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<AgeGroup, bool>>>()))
                .ReturnsAsync(true); // overlap exists
            _unitOfWorkMock.Setup(u => u.Repository<AgeGroup>()).Returns(ageGroupRepo.Object);

            var dto = new CreateAgeGroupDto { Name = "U10", MinAge = 8, MaxAge = 10 };
            
            await Assert.ThrowsAsync<ConflictException>(() =>
                _service.CreateAgeGroupAsync(1, dto, 100));
        }

        [Fact]
        public async Task CreateAgeGroupAsync_Success_ReturnsDto()
        {
            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            academyRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AcademyEntity, bool>>>()))
                .ReturnsAsync(new AcademyEntity { Id = 1 });
            _unitOfWorkMock.Setup(u => u.Repository<AcademyEntity>()).Returns(academyRepo.Object);

            var ageGroupRepo = new Mock<IRepository<AgeGroup>>();
            ageGroupRepo.SetupSequence(r => r.ExistsAsync(It.IsAny<Expression<Func<AgeGroup, bool>>>()))
                .ReturnsAsync(false) // overlap
                .ReturnsAsync(false); // name exists
            _unitOfWorkMock.Setup(u => u.Repository<AgeGroup>()).Returns(ageGroupRepo.Object);

            var dto = new CreateAgeGroupDto { Name = "U10", MinAge = 8, MaxAge = 10 };
            var ageGroup = new AgeGroup { Id = 5, Name = "U10", AcademyId = 1 };
            
            _mapperMock.Setup(m => m.Map<AgeGroup>(dto)).Returns(ageGroup);
            _mapperMock.Setup(m => m.Map<AgeGroupResponseDto>(It.IsAny<AgeGroup>()))
                .Returns(new AgeGroupResponseDto { Id = 5, Name = "U10" });

            var result = await _service.CreateAgeGroupAsync(1, dto, 100);

            Assert.NotNull(result);
            Assert.Equal(5, result.Id);
            Assert.Equal("U10", result.Name);
            ageGroupRepo.Verify(r => r.AddAsync(It.IsAny<AgeGroup>()), Times.Once);
        }

        // --- CreateTeamAsync ---

        [Fact]
        public async Task CreateTeamAsync_AcademyNotFound_ThrowsNotFoundException()
        {
            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            academyRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AcademyEntity, bool>>>()))
                .ReturnsAsync((AcademyEntity?)null);
            _unitOfWorkMock.Setup(u => u.Repository<AcademyEntity>()).Returns(academyRepo.Object);

            var dto = new CreateTeamDto { Name = "Tigers", AgeGroupId = 1, LocationId = 1 };
            
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.CreateTeamAsync(1, dto, 100));
        }

        [Fact]
        public async Task CreateTeamAsync_Success_ReturnsDto()
        {
            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            academyRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AcademyEntity, bool>>>()))
                .ReturnsAsync(new AcademyEntity { Id = 1 });
            _unitOfWorkMock.Setup(u => u.Repository<AcademyEntity>()).Returns(academyRepo.Object);

            var ageGroupRepo = new Mock<IRepository<AgeGroup>>();
            ageGroupRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AgeGroup, bool>>>()))
                .ReturnsAsync(new AgeGroup { Id = 1, AcademyId = 1 });
            _unitOfWorkMock.Setup(u => u.Repository<AgeGroup>()).Returns(ageGroupRepo.Object);

            var locationRepo = new Mock<IRepository<AcademyLocation>>();
            locationRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AcademyLocation, bool>>>()))
                .ReturnsAsync(new AcademyLocation { Id = 1, AcademyId = 1 });
            _unitOfWorkMock.Setup(u => u.Repository<AcademyLocation>()).Returns(locationRepo.Object);

            var teamRepo = new Mock<IRepository<Team>>();
            teamRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Team, bool>>>())).ReturnsAsync(false);
            
            var teams = new List<Team> { new Team { Id = 10, Name = "Tigers", AgeGroup = new AgeGroup(), Location = new AcademyLocation() } };
            teamRepo.Setup(r => r.GetQueryableAsNoTracking())
                .Returns(teams.BuildMock());
            
            _unitOfWorkMock.Setup(u => u.Repository<Team>()).Returns(teamRepo.Object);

            var dto = new CreateTeamDto { Name = "Tigers", AgeGroupId = 1, LocationId = 1 };
            var team = new Team { Id = 10, Name = "Tigers" };

            _mapperMock.Setup(m => m.Map<Team>(dto)).Returns(team);
            _mapperMock.Setup(m => m.Map<TeamResponseDto>(It.IsAny<Team>()))
                .Returns(new TeamResponseDto { Id = 10, Name = "Tigers" });

            var result = await _service.CreateTeamAsync(1, dto, 100);

            Assert.NotNull(result);
            Assert.Equal(10, result.Id);
            teamRepo.Verify(r => r.AddAsync(It.IsAny<Team>()), Times.Once);
        }

        // --- AssignCoachToTeamAsync ---

        [Fact]
        public async Task AssignCoachToTeamAsync_TeamNotFound_ThrowsNotFoundException()
        {
            var teamRepo = new Mock<IRepository<Team>>();
            var teams = new List<Team>(); // Empty list so FirstOrDefaultAsync returns null
            teamRepo.Setup(r => r.GetQueryable())
                .Returns(teams.BuildMock());
            _unitOfWorkMock.Setup(u => u.Repository<Team>()).Returns(teamRepo.Object);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.AssignCoachToTeamAsync(1, 1, 100));
        }

        // --- GetTeamsByAcademyAsync ---

        [Fact]
        public async Task GetTeamsByAcademyAsync_AcademyNotFound_ThrowsNotFoundException()
        {
            var academyRepo = new Mock<IRepository<AcademyEntity>>();
            academyRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<AcademyEntity, bool>>>()))
                .ReturnsAsync(false);
            _unitOfWorkMock.Setup(u => u.Repository<AcademyEntity>()).Returns(academyRepo.Object);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.GetTeamsByAcademyAsync(1));
        }
    }
}
