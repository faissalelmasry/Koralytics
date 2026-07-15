using Amazon.S3;
using Amazon.S3.Model;
using AutoMapper;
using Koralytics.Application.DTOs.Player;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Options;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Koralytics.Application.Services.Storage
{
    public class StorageService : IStorageService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IAmazonS3 _s3Client;
        private readonly ILogger<StorageService> _logger;
        private readonly string _bucketName;
        private readonly string _publicUrl;
        private readonly int _maxFileSizeMb;
        private readonly string[] _allowedExtensions;

        private static readonly string[] AllowedContentTypes =
        {
            "video/mp4", "video/quicktime", "video/x-msvideo", "video/x-matroska", "video/webm"
        };

        public StorageService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IAmazonS3 s3Client,
            IOptions<CloudflareR2Options> r2Options,
            ILogger<StorageService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _s3Client = s3Client;
            _logger = logger;

            var options = r2Options.Value;
            _bucketName = options.BucketName;
            _publicUrl = options.PublicUrl;
            _maxFileSizeMb = options.MaxFileSizeMb;
            _allowedExtensions = options.AllowedExtensions
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(ext => ext.StartsWith('.') ? ext : $".{ext}")
                .ToArray();
        }

        public async Task<PlayerHighlightDto> UploadHighlightAsync(int playerId, int academyId, IFormFile file, string? title)
        {
            // Validate player existence
            var playerExists = await _unitOfWork.Repository<Domain.Entities.Player.Player>().ExistsAsync(p => p.Id == playerId);
            if (!playerExists)
            {
                throw new NotFoundException($"Player with ID {playerId} not found.");
            }

            // Validate academy existence
            var academyExists = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>().ExistsAsync(a => a.Id == academyId);
            if (!academyExists)
            {
                throw new NotFoundException($"Academy with ID {academyId} not found.");
            }

            if (file == null || file.Length == 0)
            {
                throw new BadRequestException("No file was uploaded or file is empty.");
            }

            // Validate file size (configurable limit)
            long maxFileSize = (long)_maxFileSizeMb * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                throw new BadRequestException($"File size exceeds the {_maxFileSizeMb}MB limit.");
            }

            // Validate file format (video only) — reject if EITHER content type OR extension is invalid
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()) ||
                !_allowedExtensions.Contains(fileExtension))
            {
                throw new BadRequestException("Only video files (MP4, MOV, AVI, MKV, WebM) are allowed.");
            }

            // Generate unique file name
            var uniqueFileName = $"highlights/player_{playerId}_{Guid.NewGuid()}{fileExtension}";

            // Upload to Cloudflare R2
            try
            {
                using var stream = file.OpenReadStream();
                var putRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = uniqueFileName,
                    InputStream = stream,
                    ContentType = file.ContentType
                };

                await _s3Client.PutObjectAsync(putRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload video to Cloudflare R2 for player {PlayerId}", playerId);
                throw new InternalServerException($"Failed to upload video to storage: {ex.Message}");
            }

            // Construct VideoUrl
            var publicUrlBase = _publicUrl.EndsWith("/") ? _publicUrl : _publicUrl + "/";
            var videoUrl = $"{publicUrlBase}{uniqueFileName}";

            // Create PlayerHighlight record
            var highlight = new PlayerHighlight
            {
                PlayerId = playerId,
                AcademyId = academyId,
                VideoUrl = videoUrl,
                Title = title ?? file.FileName,
                IsPinned = false,
                UploadedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<PlayerHighlight>().AddAsync(highlight);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PlayerHighlightDto>(highlight);
        }

        public async Task<bool> DeleteHighlightAsync(int highlightId, int playerId)
        {
            var highlight = await _unitOfWork.Repository<PlayerHighlight>()
                .FindAsync(h => h.Id == highlightId && !h.IsDeleted);

            if (highlight == null)
            {
                throw new NotFoundException($"Highlight with ID {highlightId} not found.");
            }

            if (highlight.PlayerId != playerId)
            {
                throw new ForbiddenException("You do not own this highlight.");
            }

            // Delete from Cloudflare R2
            try
            {
                var uri = new Uri(highlight.VideoUrl);
                var fileKey = uri.AbsolutePath.TrimStart('/');

                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = fileKey
                };

                await _s3Client.DeleteObjectAsync(deleteRequest);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to delete video from Cloudflare R2 for highlight {HighlightId}. Proceeding with soft delete.",
                    highlightId);
            }

            // Soft delete record
            _unitOfWork.Repository<PlayerHighlight>().SoftDelete(highlight);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> PinHighlightAsync(int highlightId, int playerId)
        {
            var highlight = await _unitOfWork.Repository<PlayerHighlight>()
                .FindAsync(h => h.Id == highlightId && !h.IsDeleted);

            if (highlight == null)
            {
                throw new NotFoundException($"Highlight with ID {highlightId} not found.");
            }

            if (highlight.PlayerId != playerId)
            {
                throw new ForbiddenException("You do not own this highlight.");
            }

            // Unpin existing pinned highlight for this player
            var pinnedHighlight = await _unitOfWork.Repository<PlayerHighlight>()
                .FindAsync(h => h.PlayerId == playerId && h.IsPinned && !h.IsDeleted);

            if (pinnedHighlight != null)
            {
                pinnedHighlight.IsPinned = false;
            }

            highlight.IsPinned = true;
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<PlayerHighlightDto>> GetHighlightsAsync(int playerId)
        {
            // Verify player existence
            var playerExists = await _unitOfWork.Repository<Domain.Entities.Player.Player>().ExistsAsync(p => p.Id == playerId);
            if (!playerExists)
            {
                throw new NotFoundException($"Player with ID {playerId} not found.");
            }

            var highlights = await _unitOfWork.Repository<PlayerHighlight>()
                .GetQueryableAsNoTracking()
                .Where(h => h.PlayerId == playerId && !h.IsDeleted)
                .OrderByDescending(h => h.IsPinned)
                .ThenByDescending(h => h.UploadedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<PlayerHighlightDto>>(highlights);
        }
    }
}
