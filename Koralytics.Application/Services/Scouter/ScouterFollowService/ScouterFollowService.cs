using AutoMapper;
using AutoMapper.QueryableExtensions;
using Koralytics.Application.DTOs.Player;
using Koralytics.Application.DTOs.Scouter;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Scouter;
using Koralytics.Application.Mappings.ScouterProfile;
using Koralytics.Application.Services.Player.Helpers;
using Koralytics.Application.Services.Player.PlayerCardService;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Entities.Scouter;
using Koralytics.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.Services.ScouterServices.ScouterFollowService
{
    public class ScouterFollowService : IScouterFollowService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IPlayerCardService _playerCardService;
        private readonly CardInvalidationList _invalidationList;

        public ScouterFollowService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IPlayerCardService playerCardService,
            CardInvalidationList invalidationList)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _playerCardService = playerCardService;
            _invalidationList = invalidationList;
        }
        public async Task FollowPlayerAsync(int scouterId, int playerId)
        {
            
            var scouterExists = await _unitOfWork.Repository<Scouter>().ExistsAsync(s => s.Id == scouterId);
            if (!scouterExists)
            {
                throw new NotFoundException($"Scouter with ID {scouterId} not found.");
            }

            var playerExists = await _unitOfWork.Repository<Domain.Entities.Player.Player>().ExistsAsync(p => p.Id == playerId);
            if (!playerExists)
            {
                throw new NotFoundException($"Player with ID {playerId} not found.");
            }

            var alreadyFollowing = await _unitOfWork.Repository<ScouterFollow>()
                .ExistsAsync(f => f.ScouterUserId == scouterId && f.PlayerId == playerId);

            if (alreadyFollowing)
            {
                return; // Idempotent handling: silently succeed if already following
            }

            var follow = new ScouterFollow
            {
                ScouterUserId = scouterId,
                PlayerId = playerId,
                FollowedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<ScouterFollow>().AddAsync(follow);
            await _unitOfWork.SaveChangesAsync();
        }
        public async Task UnfollowPlayerAsync(int playerId, int scouterId)
        {
            var isFollowing = await _unitOfWork.Repository<ScouterFollow>()
               .FindAsync(f => f.ScouterUserId == scouterId && f.PlayerId == playerId);
           
            if (isFollowing==null) {
                var scouterExists = await _unitOfWork.Repository<Scouter>().ExistsAsync(s => s.Id == scouterId);
                if (!scouterExists)
                {
                    throw new NotFoundException($"Scouter with ID {scouterId} not found.");
                }

                var playerExists = await _unitOfWork.Repository<Domain.Entities.Player.Player>().ExistsAsync(p => p.Id == playerId);
                if (!playerExists)
                {
                    throw new NotFoundException($"Player with ID {playerId} not found.");
                }
                throw new NotFoundException($"Player with ID {playerId} is not followed by Scouter with ID {scouterId}.");
            }
            _unitOfWork.Repository<ScouterFollow>().SoftDelete(isFollowing);
            await _unitOfWork.SaveChangesAsync();

        }

        // TODO: Refactor to Redis or a fire-and-forget background worker (e.g., Hangfire/Channels) 
        // to offload profile view logging from the primary request thread. 
        
        public async Task LogProfileViewAsync(int scouterId, int playerId)
        {
            var scouterExists = await _unitOfWork.Repository<Scouter>().ExistsAsync(s => s.Id == scouterId);
            if (!scouterExists)
            {
                throw new NotFoundException($"Scouter with ID {scouterId} not found.");
            }

            var playerExists = await _unitOfWork.Repository<Domain.Entities.Player.Player>().ExistsAsync(p => p.Id == playerId);
            if (!playerExists)
            {
                throw new NotFoundException($"Player with ID {playerId} not found.");
            }
            var profileView = new ScouterView
            {
                ScouterId = scouterId,
                PlayerId = playerId,
                ViewedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<ScouterView>().AddAsync(profileView);
            await _unitOfWork.SaveChangesAsync();

        }

        public async Task<List<PlayerCardDto>> GetFollowedPlayersAsync(int scouterId)
        {
            var scouterExists = await _unitOfWork.Repository<Scouter>()
                .GetQueryableAsNoTracking()
                .AnyAsync(s => s.Id == scouterId && !s.IsDeleted);

            if (!scouterExists)
            {
                throw new NotFoundException($"Scouter profile with ID {scouterId} was not found.");
            }

            var pagedPlayerIds = await _unitOfWork.Repository<ScouterFollow>()
                .GetQueryableAsNoTracking()
                .Where(sf => sf.ScouterUserId == scouterId && !sf.IsDeleted)
                .Select(sf => sf.Player.Id)
                .ToListAsync();

            if (!pagedPlayerIds.Any())
            {
                return new List<PlayerCardDto>();
            }

            var existingCardsState = await _unitOfWork.Repository<PlayerCard>()
                .GetQueryableAsNoTracking()
                .Where(pc => pagedPlayerIds.Contains(pc.PlayerId))
                .Select(pc => new { pc.PlayerId, pc.NeedsRecalculation })
                .ToListAsync();

            var cardStateLookup = existingCardsState.ToDictionary(x => x.PlayerId, x => x.NeedsRecalculation);

            foreach (var id in pagedPlayerIds)
            {
                var cardExists = cardStateLookup.TryGetValue(id, out var dbNeedsRecalculation);

                if (!cardExists || _invalidationList.TryConsume(id) || dbNeedsRecalculation)
                {
                    await _playerCardService.RecalculatePlayerCardAsync(id);
                }
            }
            var playerCardQuery = _unitOfWork.Repository<PlayerCard>()
                .GetQueryableAsNoTracking()
                .Include(pc => pc.CategoryRatings)
                    .ThenInclude(cr => cr.DrillCategory);

            var followedPlayerCards = await _unitOfWork.Repository<Koralytics.Domain.Entities.Player.Player>()
                .GetQueryableAsNoTracking()
                .Where(p => pagedPlayerIds.Contains(p.Id))
                .Select(p => new
                {
                    Player = p,
                    PrimaryPosition = p.PlayerPositions.Where(pp => pp.IsPrimary).Select(pp => pp.Position.ToString()).FirstOrDefault() ?? string.Empty,
                    Card = playerCardQuery.FirstOrDefault(pc => pc.PlayerId == p.Id)
                })
                .Select(x => new PlayerCardDto
                {
                    PlayerName = x.Player.FirstName + " " + x.Player.LastName,
                    Position = x.PrimaryPosition,
                    OverallRating = x.Card != null ? x.Card.OverallRating : 0,
                    TransferClassification = x.Card != null ? x.Card.TransferClassification.ToString() : string.Empty,

                    PaceRating = x.Card != null ? x.Card.CategoryRatings.Where(cr => cr.DrillCategory.Name == "Speed").Select(cr => (decimal?)cr.Score).FirstOrDefault() : null,
                    ShootingRating = x.Card != null ? x.Card.CategoryRatings.Where(cr => cr.DrillCategory.Name == "Shooting").Select(cr => (decimal?)cr.Score).FirstOrDefault() : null,
                    DribblingRating = x.Card != null ? x.Card.CategoryRatings.Where(cr => cr.DrillCategory.Name == "Dribbling").Select(cr => (decimal?)cr.Score).FirstOrDefault() : null,
                    DefendingRating = x.Card != null ? x.Card.CategoryRatings.Where(cr => cr.DrillCategory.Name == "Defending").Select(cr => (decimal?)cr.Score).FirstOrDefault() : null,
                    PassingRating = x.Card != null ? x.Card.CategoryRatings.Where(cr => cr.DrillCategory.Name == "Passing").Select(cr => (decimal?)cr.Score).FirstOrDefault() : null,
                    PhysicalRating = x.Card != null ? x.Card.CategoryRatings.Where(cr => cr.DrillCategory.Name == "Physical").Select(cr => (decimal?)cr.Score).FirstOrDefault() : null,
                    GoalkeepingRating = x.Card != null ? x.Card.CategoryRatings.Where(cr => cr.DrillCategory.Name == "GoalKeeping").Select(cr => (decimal?)cr.Score).FirstOrDefault() : null,

                    PlayStyleTag = x.Player.PlayStyleTag,
                    PreferredFoot = x.Player.PreferredFoot,
                    WeakFootRating = x.Player.WeakFootRating,
                    ProfileImageUrl = x.Player.ProfileImageUrl,
                    ArchetypePlayerName = x.Player.ArchetypePlayerName
                })
                .ToListAsync();

            return followedPlayerCards;
        }

        public async Task<PlayerProfileViewAnalyticsDto> GetProfileViewsAnalyticsAsync(int playerId)
        {
            var analytics = await _unitOfWork.Repository<Domain.Entities.Player.Player>()
                .GetQueryableAsNoTracking()
                .Where(p => p.Id == playerId)
                .ProjectTo<PlayerProfileViewAnalyticsDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            return analytics ?? new PlayerProfileViewAnalyticsDto
            {
                TotalViewsCount = 0,
                RecentViews = new List<ProfileViewerDetailDto>()
            };
        }
    }
}
