using AutoMapper;
using AutoMapper.QueryableExtensions;
using Koralytics.Application.DTOs.Player;
using Koralytics.Application.DTOs.Scouter;
using Koralytics.Application.DTOs.ScouterDtos;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Notification;
using Koralytics.Application.Interfaces.Scouter;
using Koralytics.Application.Mappings.ScouterProfile;
using Koralytics.Application.Services.Player.Helpers;
using Koralytics.Application.Services.Player.PlayerCardService;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScouterEntity = Koralytics.Domain.Entities.Scouter.Scouter;
using ScouterFollow = Koralytics.Domain.Entities.Scouter.ScouterFollow;


namespace Koralytics.Application.Services.Scouter.ScouterFollowService
{
    public class ScouterFollowService : IScouterFollowService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ScouterFollowService> _logger;
        private readonly IPlayerNotificationService _playerNotificationService;

        public ScouterFollowService(
             IUnitOfWork unitOfWork,
             IMapper mapper,
             ILogger<ScouterFollowService> logger,
             IPlayerNotificationService playerNotificationService) 
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _playerNotificationService = playerNotificationService;
        }
        public async Task FollowPlayerAsync(int scouterId, int playerId)
        {
            _logger.LogInformation("Initiating FollowPlayer request. ScouterId: {ScouterId}, PlayerId: {PlayerId}", scouterId, playerId);

            var existenceData = await _unitOfWork.Repository<ScouterEntity>()
                .GetQueryableAsNoTracking()
                .Where(s => s.Id == scouterId)
                .Select(s => new { Key = "Scouter" })
                .Concat(
                    _unitOfWork.Repository<Domain.Entities.Player.Player>()
                        .GetQueryableAsNoTracking()
                        .Where(p => p.Id == playerId)
                        .Select(p => new { Key = "Player" })
                )
                .ToListAsync();

            if (!existenceData.Any(x => x.Key == "Scouter"))
            {
                throw new NotFoundException($"Scouter with ID {scouterId} not found.");
            }

            if (!existenceData.Any(x => x.Key == "Player"))
            {
                throw new NotFoundException($"Player with ID {playerId} not found.");
            }

            var alreadyFollowing = await _unitOfWork.Repository<ScouterFollow>()
                .ExistsAsync(f => f.ScouterUserId == scouterId && f.PlayerId == playerId);

            if (alreadyFollowing)
            {
                _logger.LogInformation("Idempotency triggered: Scouter {ScouterId} already follows Player {PlayerId}. No action taken.", scouterId, playerId);
                return;
            }

            var follow = new ScouterFollow
            {
                ScouterUserId = scouterId,
                PlayerId = playerId,
                FollowedAt = DateTime.UtcNow
            };
            _logger.LogInformation("Adding new ScouterFollow record. ScouterId: {ScouterId}, PlayerId: {PlayerId}", scouterId, playerId);
            await _unitOfWork.Repository<ScouterFollow>().AddAsync(follow);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Successfully saved follow relationship. ScouterId: {ScouterId}, PlayerId: {PlayerId}", scouterId, playerId);
        }

        public async Task UnfollowPlayerAsync(int scouterId, int playerId)
        {
            _logger.LogInformation("Initiating UnfollowPlayer request. ScouterId: {ScouterId}, PlayerId: {PlayerId}", scouterId, playerId);
            var follow = await _unitOfWork.Repository<ScouterFollow>()
                .FindAsync(f => f.ScouterUserId == scouterId && f.PlayerId == playerId);

            if (follow != null)
            {
                _logger.LogInformation("Follow relationship found. Processing soft delete for ScouterId: {ScouterId}, PlayerId: {PlayerId}", scouterId, playerId);
                _unitOfWork.Repository<ScouterFollow>().SoftDelete(follow);
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Successfully soft-deleted follow relationship. ScouterId: {ScouterId}, PlayerId: {PlayerId}", scouterId, playerId);
                return;
            }

            var existenceData = await _unitOfWork.Repository<ScouterEntity>()
                .GetQueryableAsNoTracking()
                .Where(s => s.Id == scouterId)
                .Select(s => new { Key = "Scouter" })
                .Concat(
                    _unitOfWork.Repository<Domain.Entities.Player.Player>()
                        .GetQueryableAsNoTracking()
                        .Where(p => p.Id == playerId)
                        .Select(p => new { Key = "Player" })
                )
                .ToListAsync();

            if (!existenceData.Any(x => x.Key == "Scouter"))
                throw new NotFoundException($"Scouter with ID {scouterId} not found.");

            if (!existenceData.Any(x => x.Key == "Player"))
                throw new NotFoundException($"Player with ID {playerId} not found.");

            throw new NotFoundException($"Player with ID {playerId} is not followed by Scouter with ID {scouterId}.");
        }

        // TODO: Refactor to Redis or a fire-and-forget background worker (e.g., Hangfire/Channels) 
        // to offload profile view logging from the primary request thread. 

        public async Task LogProfileViewAsync(int scouterId, int playerId)
        {
            _logger.LogInformation("Logging profile view audit. ScouterId: {ScouterId}, PlayerId: {PlayerId}", scouterId, playerId);

            var scouterExists = await _unitOfWork.Repository<ScouterEntity>().ExistsAsync(s => s.Id == scouterId);
            if (!scouterExists)
            {
                throw new NotFoundException($"Scouter with ID {scouterId} not found.");
            }

            var playerExists = await _unitOfWork.Repository<Domain.Entities.Player.Player>().ExistsAsync(p => p.Id == playerId);
            if (!playerExists)
            {
                throw new NotFoundException($"Player with ID {playerId} does not exist.");
            }

           
            var profileView = new ScouterView
            {
                ScouterId = scouterId,
                PlayerId = playerId,
                ViewedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<ScouterView>().AddAsync(profileView);
            await _unitOfWork.SaveChangesAsync();

           
            string message = "A Scouter is currently viewing your profile!";
            await _playerNotificationService.NotifyPlayerMilestoneAsync(playerId, message);

            _logger.LogInformation("Successfully logged profile view and sent real-time notification. ScouterId: {ScouterId}, PlayerId: {PlayerId}", scouterId, playerId);
        }
        public async Task<PaginatedResult<PlayerCardDto>> GetFollowedPlayersAsync(int scouterId, int pageNumber = 1, int pageSize = 10, string? searchTerm = null)
        {
            _logger.LogInformation("Retrieving paginated followed players grid for ScouterId: {ScouterId}.", scouterId);

            var baseQuery = _unitOfWork.Repository<ScouterFollow>()
                .GetQueryableAsNoTracking()
                .Include(sf => sf.Player)
                .Where(sf => sf.ScouterUserId == scouterId && !sf.IsDeleted);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                baseQuery = baseQuery.Where(sf => sf.Player != null &&
                    (sf.Player.FirstName + " " + sf.Player.LastName).Contains(searchTerm));
            }

            int totalCount = await baseQuery.CountAsync();

            if (totalCount == 0)
            {
                var scouterExists = await _unitOfWork.Repository<ScouterEntity>()
                    .GetQueryableAsNoTracking()
                    .AnyAsync(s => s.Id == scouterId && !s.IsDeleted);

                if (!scouterExists)
                    throw new NotFoundException($"Scouter profile with ID {scouterId} was not found.");

                return new PaginatedResult<PlayerCardDto>
                {
                    Items = new List<PlayerCardDto>(),
                    TotalCount = 0,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            }

            var followedPlayerIds = await baseQuery
                .OrderByDescending(sf => sf.FollowedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(sf => sf.PlayerId)
                .ToListAsync();

            var playerCards = await _unitOfWork.Repository<PlayerCard>()
                .GetQueryableAsNoTracking()
                .Where(pc => followedPlayerIds.Contains(pc.PlayerId))
                .ProjectTo<PlayerCardDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            var items = followedPlayerIds
                .Select(id => playerCards.FirstOrDefault(pc => pc.PlayerId == id))
                .Where(pc => pc != null)
                .Cast<PlayerCardDto>()
                .ToList();

            return new PaginatedResult<PlayerCardDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        public async Task<PlayerProfileViewAnalyticsDto> GetProfileViewsAnalyticsAsync(int playerId)
        {
            _logger.LogInformation("Retrieving profile view analytics for PlayerId: {PlayerId}", playerId);

            var analytics = await _unitOfWork.Repository<Domain.Entities.Player.Player>()
                .GetQueryableAsNoTracking()
                .Where(p => p.Id == playerId)
                .ProjectTo<PlayerProfileViewAnalyticsDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            if (analytics == null)
            {
                _logger.LogWarning("No existing profile views data found for Player {PlayerId}. Returning safe defaults.", playerId);
                return new PlayerProfileViewAnalyticsDto
                {
                    TotalViewsCount = 0,
                    RecentViews = new List<ProfileViewerDetailDto>()
                };
            }

            _logger.LogInformation("Successfully retrieved view stats for Player {PlayerId}. Total Views: {Count}", playerId, analytics.TotalViewsCount);
            return analytics;
        }

      
    }
}
