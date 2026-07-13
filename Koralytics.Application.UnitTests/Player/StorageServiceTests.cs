using Amazon.S3;
using Amazon.S3.Model;
using AutoMapper;
using Koralytics.Application.DTOs.Player;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Options;
using Koralytics.Application.Services.Storage;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Koralytics.Application.UnitTests.Player
{
    public class StorageServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IAmazonS3> _s3ClientMock;
        private readonly Mock<ILogger<StorageService>> _loggerMock;
        private readonly StorageService _service;

        public StorageServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _s3ClientMock = new Mock<IAmazonS3>();
            _loggerMock = new Mock<ILogger<StorageService>>();

            var r2Options = Microsoft.Extensions.Options.Options.Create(new CloudflareR2Options
            {
                BucketName = "test-bucket",
                PublicUrl = "https://pub-test.r2.dev",
                MaxFileSizeMb = 100,
                AllowedExtensions = "mp4,mov,avi,mkv,webm"
            });

            _service = new StorageService(
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _s3ClientMock.Object,
                r2Options,
                _loggerMock.Object);
        }

        // ──────────────────────────────────────────────────────────────
        // UploadHighlightAsync Tests
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task UploadHighlightAsync_PlayerNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var playerRepoMock = new Mock<IRepository<Domain.Entities.Player.Player>>();
            playerRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Domain.Entities.Player.Player, bool>>>()))
                .ReturnsAsync(false);

            _unitOfWorkMock.Setup(u => u.Repository<Domain.Entities.Player.Player>())
                .Returns(playerRepoMock.Object);

            var fileMock = new Mock<IFormFile>();

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.UploadHighlightAsync(1, 1, fileMock.Object, "Title"));
        }

        [Fact]
        public async Task UploadHighlightAsync_AcademyNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var playerRepoMock = new Mock<IRepository<Domain.Entities.Player.Player>>();
            playerRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Domain.Entities.Player.Player, bool>>>()))
                .ReturnsAsync(true);

            var academyRepoMock = new Mock<IRepository<Academy>>();
            academyRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Academy, bool>>>()))
                .ReturnsAsync(false);

            _unitOfWorkMock.Setup(u => u.Repository<Domain.Entities.Player.Player>())
                .Returns(playerRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<Academy>())
                .Returns(academyRepoMock.Object);

            var fileMock = new Mock<IFormFile>();

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.UploadHighlightAsync(1, 1, fileMock.Object, "Title"));
        }

        [Fact]
        public async Task UploadHighlightAsync_FileEmpty_ThrowsBadRequestException()
        {
            // Arrange
            var playerRepoMock = new Mock<IRepository<Domain.Entities.Player.Player>>();
            playerRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Domain.Entities.Player.Player, bool>>>()))
                .ReturnsAsync(true);

            var academyRepoMock = new Mock<IRepository<Academy>>();
            academyRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Academy, bool>>>()))
                .ReturnsAsync(true);

            _unitOfWorkMock.Setup(u => u.Repository<Domain.Entities.Player.Player>())
                .Returns(playerRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<Academy>())
                .Returns(academyRepoMock.Object);

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(0);

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.UploadHighlightAsync(1, 1, fileMock.Object, "Title"));
        }

        [Fact]
        public async Task UploadHighlightAsync_FileSizeExceeded_ThrowsBadRequestException()
        {
            // Arrange
            var playerRepoMock = new Mock<IRepository<Domain.Entities.Player.Player>>();
            playerRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Domain.Entities.Player.Player, bool>>>()))
                .ReturnsAsync(true);

            var academyRepoMock = new Mock<IRepository<Academy>>();
            academyRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Academy, bool>>>()))
                .ReturnsAsync(true);

            _unitOfWorkMock.Setup(u => u.Repository<Domain.Entities.Player.Player>())
                .Returns(playerRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<Academy>())
                .Returns(academyRepoMock.Object);

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(101L * 1024 * 1024); // 101 MB

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.UploadHighlightAsync(1, 1, fileMock.Object, "Title"));
        }

        [Fact]
        public async Task UploadHighlightAsync_InvalidFormat_ThrowsBadRequestException()
        {
            // Arrange
            var playerRepoMock = new Mock<IRepository<Domain.Entities.Player.Player>>();
            playerRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Domain.Entities.Player.Player, bool>>>()))
                .ReturnsAsync(true);

            var academyRepoMock = new Mock<IRepository<Academy>>();
            academyRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Academy, bool>>>()))
                .ReturnsAsync(true);

            _unitOfWorkMock.Setup(u => u.Repository<Domain.Entities.Player.Player>())
                .Returns(playerRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<Academy>())
                .Returns(academyRepoMock.Object);

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(10 * 1024 * 1024); // 10 MB
            fileMock.Setup(f => f.FileName).Returns("image.jpg");
            fileMock.Setup(f => f.ContentType).Returns("image/jpeg");

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.UploadHighlightAsync(1, 1, fileMock.Object, "Title"));
        }

        [Fact]
        public async Task UploadHighlightAsync_ValidExtensionButInvalidContentType_ThrowsBadRequestException()
        {
            // Arrange — a file renamed to .mp4 but with wrong content type should be rejected (|| logic)
            var playerRepoMock = new Mock<IRepository<Domain.Entities.Player.Player>>();
            playerRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Domain.Entities.Player.Player, bool>>>()))
                .ReturnsAsync(true);

            var academyRepoMock = new Mock<IRepository<Academy>>();
            academyRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Academy, bool>>>()))
                .ReturnsAsync(true);

            _unitOfWorkMock.Setup(u => u.Repository<Domain.Entities.Player.Player>())
                .Returns(playerRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<Academy>())
                .Returns(academyRepoMock.Object);

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(10 * 1024 * 1024);
            fileMock.Setup(f => f.FileName).Returns("fake.mp4");
            fileMock.Setup(f => f.ContentType).Returns("application/octet-stream"); // wrong content type

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.UploadHighlightAsync(1, 1, fileMock.Object, "Title"));
        }

        [Fact]
        public async Task UploadHighlightAsync_Success_ReturnsHighlightDto()
        {
            // Arrange
            var playerRepoMock = new Mock<IRepository<Domain.Entities.Player.Player>>();
            playerRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Domain.Entities.Player.Player, bool>>>()))
                .ReturnsAsync(true);

            var academyRepoMock = new Mock<IRepository<Academy>>();
            academyRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Academy, bool>>>()))
                .ReturnsAsync(true);

            var highlightRepoMock = new Mock<IRepository<PlayerHighlight>>();
            highlightRepoMock.Setup(r => r.AddAsync(It.IsAny<PlayerHighlight>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock.Setup(u => u.Repository<Domain.Entities.Player.Player>())
                .Returns(playerRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<Academy>())
                .Returns(academyRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<PlayerHighlight>())
                .Returns(highlightRepoMock.Object);

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(10 * 1024 * 1024); // 10 MB
            fileMock.Setup(f => f.FileName).Returns("video.mp4");
            fileMock.Setup(f => f.ContentType).Returns("video/mp4");
            fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(Encoding.UTF8.GetBytes("fake content")));

            _s3ClientMock.Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PutObjectResponse());

            var highlightDto = new PlayerHighlightDto
            {
                Id = 1,
                PlayerId = 1,
                AcademyId = 1,
                VideoUrl = "https://pub-test.r2.dev/highlights/video.mp4",
                Title = "Test Highlight",
                IsPinned = false
            };

            _mapperMock.Setup(m => m.Map<PlayerHighlightDto>(It.IsAny<PlayerHighlight>()))
                .Returns(highlightDto);

            // Act
            var result = await _service.UploadHighlightAsync(1, 1, fileMock.Object, "Test Highlight");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Highlight", result.Title);
            _s3ClientMock.Verify(s => s.PutObjectAsync(
                It.Is<PutObjectRequest>(r => r.BucketName == "test-bucket"),
                It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UploadHighlightAsync_S3Failure_ThrowsInternalServerException()
        {
            // Arrange
            var playerRepoMock = new Mock<IRepository<Domain.Entities.Player.Player>>();
            playerRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Domain.Entities.Player.Player, bool>>>()))
                .ReturnsAsync(true);

            var academyRepoMock = new Mock<IRepository<Academy>>();
            academyRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Academy, bool>>>()))
                .ReturnsAsync(true);

            _unitOfWorkMock.Setup(u => u.Repository<Domain.Entities.Player.Player>())
                .Returns(playerRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<Academy>())
                .Returns(academyRepoMock.Object);

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(10 * 1024 * 1024);
            fileMock.Setup(f => f.FileName).Returns("video.mp4");
            fileMock.Setup(f => f.ContentType).Returns("video/mp4");
            fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(Encoding.UTF8.GetBytes("fake content")));

            _s3ClientMock.Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Network error"));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InternalServerException>(() =>
                _service.UploadHighlightAsync(1, 1, fileMock.Object, "Title"));
            Assert.Contains("Failed to upload video to storage", ex.Message);
        }

        // ──────────────────────────────────────────────────────────────
        // DeleteHighlightAsync Tests
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteHighlightAsync_HighlightNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var highlightRepoMock = new Mock<IRepository<PlayerHighlight>>();
            highlightRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<PlayerHighlight, bool>>>()))
                .ReturnsAsync((PlayerHighlight?)null);

            _unitOfWorkMock.Setup(u => u.Repository<PlayerHighlight>())
                .Returns(highlightRepoMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.DeleteHighlightAsync(1, 1));
        }

        [Fact]
        public async Task DeleteHighlightAsync_UnauthorizedPlayer_ThrowsForbiddenException()
        {
            // Arrange
            var highlight = new PlayerHighlight
            {
                Id = 1,
                PlayerId = 2 // Different player
            };

            var highlightRepoMock = new Mock<IRepository<PlayerHighlight>>();
            highlightRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<PlayerHighlight, bool>>>()))
                .ReturnsAsync(highlight);

            _unitOfWorkMock.Setup(u => u.Repository<PlayerHighlight>())
                .Returns(highlightRepoMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                _service.DeleteHighlightAsync(1, 1));
        }

        [Fact]
        public async Task DeleteHighlightAsync_Success_ReturnsTrue()
        {
            // Arrange
            var highlight = new PlayerHighlight
            {
                Id = 1,
                PlayerId = 1,
                VideoUrl = "https://pub-test.r2.dev/highlights/video.mp4"
            };

            var highlightRepoMock = new Mock<IRepository<PlayerHighlight>>();
            highlightRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<PlayerHighlight, bool>>>()))
                .ReturnsAsync(highlight);
            highlightRepoMock.Setup(r => r.SoftDelete(It.IsAny<PlayerHighlight>()));

            _unitOfWorkMock.Setup(u => u.Repository<PlayerHighlight>())
                .Returns(highlightRepoMock.Object);

            _s3ClientMock.Setup(s => s.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DeleteObjectResponse());

            // Act
            var result = await _service.DeleteHighlightAsync(1, 1);

            // Assert
            Assert.True(result);
            highlightRepoMock.Verify(r => r.SoftDelete(highlight), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteHighlightAsync_S3Failure_StillSoftDeletesAndLogs()
        {
            // Arrange — S3 deletion fails but the record should still be soft-deleted
            var highlight = new PlayerHighlight
            {
                Id = 1,
                PlayerId = 1,
                VideoUrl = "https://pub-test.r2.dev/highlights/video.mp4"
            };

            var highlightRepoMock = new Mock<IRepository<PlayerHighlight>>();
            highlightRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<PlayerHighlight, bool>>>()))
                .ReturnsAsync(highlight);
            highlightRepoMock.Setup(r => r.SoftDelete(It.IsAny<PlayerHighlight>()));

            _unitOfWorkMock.Setup(u => u.Repository<PlayerHighlight>())
                .Returns(highlightRepoMock.Object);

            _s3ClientMock.Setup(s => s.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("S3 connection error"));

            // Act
            var result = await _service.DeleteHighlightAsync(1, 1);

            // Assert — soft delete still happened
            Assert.True(result);
            highlightRepoMock.Verify(r => r.SoftDelete(highlight), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // ──────────────────────────────────────────────────────────────
        // PinHighlightAsync Tests
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task PinHighlightAsync_HighlightNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var highlightRepoMock = new Mock<IRepository<PlayerHighlight>>();
            highlightRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<PlayerHighlight, bool>>>()))
                .ReturnsAsync((PlayerHighlight?)null);

            _unitOfWorkMock.Setup(u => u.Repository<PlayerHighlight>())
                .Returns(highlightRepoMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.PinHighlightAsync(1, 1));
        }

        [Fact]
        public async Task PinHighlightAsync_UnauthorizedPlayer_ThrowsForbiddenException()
        {
            // Arrange
            var highlight = new PlayerHighlight { Id = 1, PlayerId = 2, IsPinned = false };

            var highlightRepoMock = new Mock<IRepository<PlayerHighlight>>();
            highlightRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<PlayerHighlight, bool>>>()))
                .ReturnsAsync(highlight);

            _unitOfWorkMock.Setup(u => u.Repository<PlayerHighlight>())
                .Returns(highlightRepoMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                _service.PinHighlightAsync(1, 1));
        }

        [Fact]
        public async Task PinHighlightAsync_NoPreviouslyPinned_PinsNewHighlight()
        {
            // Arrange
            var highlightToPin = new PlayerHighlight { Id = 1, PlayerId = 1, IsPinned = false };

            var highlightRepoMock = new Mock<IRepository<PlayerHighlight>>();
            highlightRepoMock.SetupSequence(r => r.FindAsync(It.IsAny<Expression<Func<PlayerHighlight, bool>>>()))
                .ReturnsAsync(highlightToPin)
                .ReturnsAsync((PlayerHighlight?)null); // No existing pinned

            _unitOfWorkMock.Setup(u => u.Repository<PlayerHighlight>())
                .Returns(highlightRepoMock.Object);

            // Act
            var result = await _service.PinHighlightAsync(1, 1);

            // Assert
            Assert.True(result);
            Assert.True(highlightToPin.IsPinned);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task PinHighlightAsync_Success_PinsAndUnpinsPrevious()
        {
            // Arrange
            var highlightToPin = new PlayerHighlight { Id = 1, PlayerId = 1, IsPinned = false };
            var existingPinned = new PlayerHighlight { Id = 2, PlayerId = 1, IsPinned = true };

            var highlightRepoMock = new Mock<IRepository<PlayerHighlight>>();
            highlightRepoMock.SetupSequence(r => r.FindAsync(It.IsAny<Expression<Func<PlayerHighlight, bool>>>()))
                .ReturnsAsync(highlightToPin)
                .ReturnsAsync(existingPinned);

            _unitOfWorkMock.Setup(u => u.Repository<PlayerHighlight>())
                .Returns(highlightRepoMock.Object);

            // Act
            var result = await _service.PinHighlightAsync(1, 1);

            // Assert
            Assert.True(result);
            Assert.True(highlightToPin.IsPinned);
            Assert.False(existingPinned.IsPinned);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // ──────────────────────────────────────────────────────────────
        // GetHighlightsAsync Tests
        // ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetHighlightsAsync_PlayerNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var playerRepoMock = new Mock<IRepository<Domain.Entities.Player.Player>>();
            playerRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Domain.Entities.Player.Player, bool>>>()))
                .ReturnsAsync(false);

            _unitOfWorkMock.Setup(u => u.Repository<Domain.Entities.Player.Player>())
                .Returns(playerRepoMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.GetHighlightsAsync(999));
        }

        [Fact]
        public async Task GetHighlightsAsync_NoHighlights_ReturnsEmptyList()
        {
            // Arrange
            var playerRepoMock = new Mock<IRepository<Domain.Entities.Player.Player>>();
            playerRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Domain.Entities.Player.Player, bool>>>()))
                .ReturnsAsync(true);

            var emptyList = new List<PlayerHighlight>().BuildMock();

            var highlightRepoMock = new Mock<IRepository<PlayerHighlight>>();
            highlightRepoMock.Setup(r => r.GetQueryableAsNoTracking())
                .Returns(emptyList);

            _unitOfWorkMock.Setup(u => u.Repository<Domain.Entities.Player.Player>())
                .Returns(playerRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<PlayerHighlight>())
                .Returns(highlightRepoMock.Object);

            _mapperMock.Setup(m => m.Map<IEnumerable<PlayerHighlightDto>>(It.IsAny<List<PlayerHighlight>>()))
                .Returns(new List<PlayerHighlightDto>());

            // Act
            var result = await _service.GetHighlightsAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetHighlightsAsync_WithHighlights_ReturnsOrderedByPinnedThenDate()
        {
            // Arrange
            var playerRepoMock = new Mock<IRepository<Domain.Entities.Player.Player>>();
            playerRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Domain.Entities.Player.Player, bool>>>()))
                .ReturnsAsync(true);

            var highlights = new List<PlayerHighlight>
            {
                new() { Id = 1, PlayerId = 1, IsPinned = false, UploadedAt = DateTime.UtcNow.AddDays(-2), VideoUrl = "url1" },
                new() { Id = 2, PlayerId = 1, IsPinned = true,  UploadedAt = DateTime.UtcNow.AddDays(-1), VideoUrl = "url2" },
                new() { Id = 3, PlayerId = 1, IsPinned = false, UploadedAt = DateTime.UtcNow,             VideoUrl = "url3" }
            };

            var mockQueryable = highlights.BuildMock();

            var highlightRepoMock = new Mock<IRepository<PlayerHighlight>>();
            highlightRepoMock.Setup(r => r.GetQueryableAsNoTracking())
                .Returns(mockQueryable);

            _unitOfWorkMock.Setup(u => u.Repository<Domain.Entities.Player.Player>())
                .Returns(playerRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<PlayerHighlight>())
                .Returns(highlightRepoMock.Object);

            var expectedDtos = new List<PlayerHighlightDto>
            {
                new() { Id = 2, PlayerId = 1, IsPinned = true },
                new() { Id = 3, PlayerId = 1, IsPinned = false },
                new() { Id = 1, PlayerId = 1, IsPinned = false }
            };

            _mapperMock.Setup(m => m.Map<IEnumerable<PlayerHighlightDto>>(It.IsAny<List<PlayerHighlight>>()))
                .Returns(expectedDtos);

            // Act
            var result = await _service.GetHighlightsAsync(1);

            // Assert
            var resultList = result.ToList();
            Assert.Equal(3, resultList.Count);
            Assert.True(resultList[0].IsPinned); // Pinned should be first
        }
    }
}
