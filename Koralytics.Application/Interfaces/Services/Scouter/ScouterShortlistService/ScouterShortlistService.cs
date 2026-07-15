using AutoMapper;
using Koralytics.Application.DTOs.Player;
using Koralytics.Application.DTOs.ScouterDtos;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Scouter;
using Koralytics.Application.Interfaces.ScouterInterfaces;
using Koralytics.Application.Services.Player.Helpers;
using Koralytics.Application.Services.Player.PlayerCardService;
using Koralytics.Domain.Entities.Match;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Entities.Scouter;
using Koralytics.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Koralytics.Application.Services.ScouterServices.ScouterShortlistService
{
    public class ScouterShortlistService : IScouterShortlistService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IPlayerCardService _playerCardService;
        private readonly CardInvalidationList _invalidationList;

        public ScouterShortlistService(
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
        public async Task<ScouterShortlistDto> AddToShortlistAsync(int scouterId, int playerId)
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

            var alreadyShortlisted = await _unitOfWork.Repository<ScouterShortlist>()
                .FindAsNoTrackingAsync(sl => sl.ScouterUserId == scouterId && sl.PlayerId == playerId);

            if (alreadyShortlisted != null)
            {
                return _mapper.Map<ScouterShortlistDto>(alreadyShortlisted);
            }

            var entry = new ScouterShortlist
            {
                ScouterUserId = scouterId,
                PlayerId = playerId,
                AddedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<ScouterShortlist>().AddAsync(entry);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ScouterShortlistDto>(entry);
        }
        public async Task<bool> RemoveFromShortlistAsync(int scouterId, int playerId)
        {
            var entry = await _unitOfWork.Repository<ScouterShortlist>()
                .FindAsync(sl => sl.ScouterUserId == scouterId && sl.PlayerId == playerId);

            if (entry == null)
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

                throw new NotFoundException($"Player with ID {playerId} is not in Scouter {scouterId}'s shortlist.");
            }

            _unitOfWork.Repository<ScouterShortlist>().SoftDelete(entry);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        public async Task<List<PlayerCardDto>> GetShortlistAsync(int scouterId)
        {
            var scouterExists = await _unitOfWork.Repository<Scouter>().ExistsAsync(s => s.Id == scouterId);
            if (!scouterExists)
            {
                throw new NotFoundException($"Scouter with ID {scouterId} not found.");
            }

            var shortlistedPlayerIds = await _unitOfWork.Repository<ScouterShortlist>()
                .GetQueryableAsNoTracking()
                .Where(sl => sl.ScouterUserId == scouterId)
                .OrderByDescending(sl => sl.Id)
                .Select(sl => sl.PlayerId)
                .ToListAsync();

            if (!shortlistedPlayerIds.Any())
            {
                return new List<PlayerCardDto>();
            }

            var existingCardsState = await _unitOfWork.Repository<PlayerCard>()
                .GetQueryableAsNoTracking()
                .Where(pc => shortlistedPlayerIds.Contains(pc.PlayerId))
                .Select(pc => new { pc.PlayerId, pc.NeedsRecalculation })
                .ToListAsync();

            var cardStateLookup = existingCardsState.ToDictionary(x => x.PlayerId, x => x.NeedsRecalculation);

            foreach (var id in shortlistedPlayerIds)
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

            var pagedRecords = await _unitOfWork.Repository<Domain.Entities.Player.Player>()
                .GetQueryableAsNoTracking()
                .Where(p => shortlistedPlayerIds.Contains(p.Id))
                .Select(p => new
                {
                    PlayerId = p.Id,
                    Player = p,
                    PrimaryPosition = p.PlayerPositions.Where(pp => pp.IsPrimary).Select(pp => pp.Position.ToString()).FirstOrDefault() ?? string.Empty,
                    Card = playerCardQuery.FirstOrDefault(pc => pc.PlayerId == p.Id)
                })
                .ToListAsync();

            var shortlistOrderLookup = shortlistedPlayerIds
                .Select((playerId, index) => new { playerId, index })
                .ToDictionary(x => x.playerId, x => x.index);

            var playerCardDtos = pagedRecords
                .OrderBy(x => shortlistOrderLookup[x.PlayerId])
                .Select(x => new PlayerCardDto
                {
                    PlayerName = x.Player.FirstName + " " + x.Player.LastName,
                    Position = x.PrimaryPosition,
                    OverallRating = x.Card != null ? x.Card.OverallRating : 0,
                    TransferClassification = x.Card != null ? x.Card.TransferClassification.ToString() : string.Empty,

                    PaceRating = x.Card?.CategoryRatings?.Where(cr => cr.DrillCategory.Name == "Speed").Select(cr => (decimal?)cr.Score).FirstOrDefault(),
                    ShootingRating = x.Card?.CategoryRatings?.Where(cr => cr.DrillCategory.Name == "Shooting").Select(cr => (decimal?)cr.Score).FirstOrDefault(),
                    DribblingRating = x.Card?.CategoryRatings?.Where(cr => cr.DrillCategory.Name == "Dribbling").Select(cr => (decimal?)cr.Score).FirstOrDefault(),
                    DefendingRating = x.Card?.CategoryRatings?.Where(cr => cr.DrillCategory.Name == "Defending").Select(cr => (decimal?)cr.Score).FirstOrDefault(),
                    PassingRating = x.Card?.CategoryRatings?.Where(cr => cr.DrillCategory.Name == "Passing").Select(cr => (decimal?)cr.Score).FirstOrDefault(),
                    PhysicalRating = x.Card?.CategoryRatings?.Where(cr => cr.DrillCategory.Name == "Physical").Select(cr => (decimal?)cr.Score).FirstOrDefault(),
                    GoalkeepingRating = x.Card?.CategoryRatings?.Where(cr => cr.DrillCategory.Name == "GoalKeeping").Select(cr => (decimal?)cr.Score).FirstOrDefault(),

                    PlayStyleTag = x.Player.PlayStyleTag,
                    PreferredFoot = x.Player.PreferredFoot,
                    WeakFootRating = x.Player.WeakFootRating,
                    ProfileImageUrl = x.Player.ProfileImageUrl,
                    ArchetypePlayerName = x.Player.ArchetypePlayerName
                })
                .ToList();

            return playerCardDtos;
        }

    }
}
