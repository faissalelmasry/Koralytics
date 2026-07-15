
using Koralytics.Application.DTOs.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Application.Interfaces.ScouterInterfaces;
using Koralytics.Application.DTOs.ScouterDtos;
using Koralytics.Application.Interfaces;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Entities.Match;
using Microsoft.EntityFrameworkCore;
using AutoMapper.QueryableExtensions;
using AutoMapper;
using Koralytics.Application.Services.Player.Helpers;
using Koralytics.Application.Services.Player.PlayerCardService;
using Koralytics.Application.DTOs.Scouter;
using Koralytics.Domain.Entities.Scouter;
using Koralytics.Domain.Exceptions;

namespace Koralytics.Application.Services.Scouter.ScouterSearchService
{
    public class ScouterSearchService : IScouterSearchService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

       
        private readonly IPlayerCardService _playerCardService;
        private readonly CardInvalidationList _invalidationList;

        public ScouterSearchService(
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
        public async Task<PaginatedResult<PlayerCardDto>> SearchPlayersAsync(PlayerSearchFiltersDto filters)
        {
            var query = _unitOfWork.Repository<Domain.Entities.Player.Player>().GetQueryableAsNoTracking();
            var currentYear = DateTime.UtcNow.Year;
            var playerCardQuery = _unitOfWork.Repository<PlayerCard>()
                .GetQueryableAsNoTracking()
                .Include(pc => pc.CategoryRatings)
                    .ThenInclude(cr => cr.DrillCategory);

            if (filters != null)
            {
                if (filters.MinAge != null) query = query.Where(p => (currentYear - p.DateOfBirth.Year) >= filters.MinAge);
                if (filters.MaxAge != null) query = query.Where(p => (currentYear - p.DateOfBirth.Year) <= filters.MaxAge);
                if (filters.PreferredFoot != null) query = query.Where(p => p.PreferredFoot == filters.PreferredFoot);
                if (filters.Positions != null && filters.Positions.Any()) query = query.Where(p => p.PlayerPositions.Any(pp => filters.Positions.Contains(pp.Position)));
                if (filters.AcademyId != null) query = query.Where(p => p.PlayerAcademies.OrderByDescending(pa => pa.Id).Select(pa => (int?)pa.AcademyId).FirstOrDefault() == filters.AcademyId);
                if (filters.Format != null) query = query.Where(p => _unitOfWork.Repository<MatchLineup>().GetQueryableAsNoTracking().Any(ml => ml.PlayerId == p.Id && ml.Match.Format == filters.Format));

                if (filters.MinRating != null)
                {
                    query = query.Where(p => playerCardQuery.Where(pc => pc.PlayerId == p.Id).Select(pc => pc.OverallRating).FirstOrDefault() >= filters.MinRating);
                }

                if (filters.MaxRating != null)
                {
                    query = query.Where(p => playerCardQuery.Where(pc => pc.PlayerId == p.Id).Select(pc => pc.OverallRating).FirstOrDefault() <= filters.MaxRating);
                }
            }

            int totalCount = await query.CountAsync();

            var pagedPlayerIds = await query
                .OrderByDescending(p => p.Id)
                .Skip((filters.PageNumber - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .Select(p => p.Id)
                .ToListAsync();

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

            var playerCardDtos = await _unitOfWork.Repository<Domain.Entities.Player.Player>()
                .GetQueryableAsNoTracking()
                .Where(p => pagedPlayerIds.Contains(p.Id))
                .OrderByDescending(p => p.Id)
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

            return new PaginatedResult<PlayerCardDto>
            {
                Items = playerCardDtos,
                TotalCount = totalCount,
                PageNumber = filters.PageNumber,
                PageSize = filters.PageSize
            };
        }
        public async Task<ScouterProfileDto> GetScouterByIdAsync(int scouterId)
        {
            var scouterDto = await _unitOfWork.Repository<Koralytics.Domain.Entities.Scouter.Scouter>()
                .GetQueryableAsNoTracking()
                .Where(s => s.Id == scouterId)
                .ProjectTo<ScouterProfileDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            if (scouterDto == null)
            {
                throw new NotFoundException($"Scouter with ID {scouterId} was not found.");
            }

            return scouterDto;
        }
    }
}
