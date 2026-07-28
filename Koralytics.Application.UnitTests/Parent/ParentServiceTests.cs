using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Services.Parent;
using Koralytics.Domain.Entities.Parents;
using Koralytics.Domain.Entities.Player;
using Koralytics.Infrastructure.Services.Parents;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Xunit;
using PlayerEntity = Koralytics.Domain.Entities.Player.Player;

namespace Koralytics.Application.UnitTests.Parent
{
    public class ParentServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly ParentService _service;

        public ParentServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _service = new ParentService(_unitOfWorkMock.Object);
        }

        [Fact]
        public async Task GetMyChildrenAsync_WhenParentHasChildren_ReturnsChildrenDtos()
        {
            // Arrange
            int parentId = 10;
            var parentPlayers = new List<ParentPlayer>
            {
                new ParentPlayer
                {
                    ParentId = parentId,
                    PlayerId = 100,
                    Player = new PlayerEntity
                    {
                        Id = 100,
                        FirstName = "John",
                        LastName = "Doe",
                        ProfileImageUrl = "http://example.com/john.jpg",
                        PlayerPositions = new List<PlayerPosition>
                        {
                            new PlayerPosition { Position = "Midfielder", IsPrimary = true }
                        },
                        PlayerTeams = new List<PlayerTeam>
                        {
                            new PlayerTeam { Team = new Domain.Entities.Academy.Team { Name = "Eagles FC" } }
                        }
                    }
                }
            };

            var mockRepo = new Mock<IRepository<ParentPlayer>>();
            mockRepo.Setup(r => r.GetQueryable()).Returns(parentPlayers.BuildMock());
            _unitOfWorkMock.Setup(u => u.Repository<ParentPlayer>()).Returns(mockRepo.Object);

            // Act
            var result = await _service.GetMyChildrenAsync(parentId);

            // Assert
            Assert.NotNull(result);
            var childrenList = result.ToList();
            Assert.Single(childrenList);
            Assert.Equal(100, childrenList[0].PlayerId);
            Assert.Equal("John Doe", childrenList[0].FullName);
            Assert.Equal("http://example.com/john.jpg", childrenList[0].PhotoUrl);
            Assert.Equal("Midfielder", childrenList[0].Position);
            Assert.Equal("Eagles FC", childrenList[0].TeamName);
        }

        [Fact]
        public async Task GetMyChildrenAsync_WhenParentHasNoChildren_ReturnsEmpty()
        {
            // Arrange
            int parentId = 99;
            var parentPlayers = new List<ParentPlayer>();

            var mockRepo = new Mock<IRepository<ParentPlayer>>();
            mockRepo.Setup(r => r.GetQueryable()).Returns(parentPlayers.BuildMock());
            _unitOfWorkMock.Setup(u => u.Repository<ParentPlayer>()).Returns(mockRepo.Object);

            // Act
            var result = await _service.GetMyChildrenAsync(parentId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}
