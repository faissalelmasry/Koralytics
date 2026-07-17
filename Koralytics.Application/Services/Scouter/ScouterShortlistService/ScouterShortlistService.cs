using AutoMapper;
using AutoMapper.QueryableExtensions;
using Koralytics.Application.DTOs.Player;
using Koralytics.Application.DTOs.ScouterDtos;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Scouter;
using Koralytics.Application.Interfaces.ScouterInterfaces;
using Koralytics.Application.Services.Player.Helpers;
using Koralytics.Application.Services.Player.PlayerCardService;
using Koralytics.Domain.Entities.Match;
using Koralytics.Domain.Entities.Player;
using ScouterEntity = Koralytics.Domain.Entities.Scouter.Scouter;
using ScouterShortlist = Koralytics.Domain.Entities.Scouter.ScouterShortlist;
using Koralytics.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Koralytics.Application.Services.Scouter.ScouterShortlistService
{
    public class ScouterShortlistService : IScouterShortlistService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ScouterShortlistService> _logger;

        public ScouterShortlistService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<ScouterShortlistService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper; 
            _logger = logger;
        }
        public async Task<ScouterShortlistDto> AddToShortlistAsync(int scouterId, int playerId)
        {
            _logger.LogInformation("Processing AddToShortlist request. ScouterId: {ScouterId}, PlayerId: {PlayerId}", scouterId, playerId);

            var scouterExists = await _unitOfWork.Repository<ScouterEntity>().ExistsAsync(s => s.Id == scouterId);
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
                _logger.LogInformation("Idempotency triggered: Player {PlayerId} is already shortlisted by Scouter {ScouterId}. Returning mapped existing record.", playerId, scouterId);
                return _mapper.Map<ScouterShortlistDto>(alreadyShortlisted);
            }

            var entry = new ScouterShortlist
            {
                ScouterUserId = scouterId,
                PlayerId = playerId,
                AddedAt = DateTime.UtcNow
            }; 

            _logger.LogInformation("Saving new shortlist record. ScouterId: {ScouterId}, PlayerId: {PlayerId}", scouterId, playerId);
            await _unitOfWork.Repository<ScouterShortlist>().AddAsync(entry);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Successfully shortlisted Player {PlayerId} for Scouter {ScouterId}.", playerId, scouterId);
            return _mapper.Map<ScouterShortlistDto>(entry);
        }
        public async Task<bool> RemoveFromShortlistAsync(int scouterId, int playerId)
        {
            _logger.LogInformation("Processing RemoveFromShortlist request. ScouterId: {ScouterId}, PlayerId: {PlayerId}", scouterId, playerId);
            var entry = await _unitOfWork.Repository<ScouterShortlist>()
                .FindAsync(sl => sl.ScouterUserId == scouterId && sl.PlayerId == playerId);

            if (entry == null)
            {
                var scouterExists = await _unitOfWork.Repository<ScouterEntity>().ExistsAsync(s => s.Id == scouterId);
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

            _logger.LogInformation("Processing soft delete of shortlist record. EntryId: {Id}", entry.Id);
            _unitOfWork.Repository<ScouterShortlist>().SoftDelete(entry);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Successfully removed Player {PlayerId} from Scouter {ScouterId}'s shortlist.", playerId, scouterId);
            return true;
        }
        public async Task<PaginatedResult<PlayerCardDto>> GetShortlistAsync(int scouterId, int pageNumber = 1, int pageSize = 10, string? searchTerm = null)
        {
            _logger.LogInformation("Retrieving paginated shortlist grid for ScouterId: {ScouterId}. Parameters -> PageNumber: {Page}, PageSize: {Size}, SearchTerm: '{Term}'", scouterId, pageNumber, pageSize, searchTerm ?? string.Empty);

            var baseQuery = _unitOfWork.Repository<ScouterShortlist>()
                .GetQueryableAsNoTracking()
                .Where(sl => sl.ScouterUserId == scouterId && !sl.IsDeleted);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                baseQuery = baseQuery.Where(sl =>
                    (sl.Player.FirstName + " " + sl.Player.LastName).Contains(searchTerm));
            }

            int totalCount = await baseQuery.CountAsync();

            if (totalCount == 0)
            {
                var scouterExists = await _unitOfWork.Repository<ScouterEntity>()
                    .GetQueryableAsNoTracking()
                    .AnyAsync(s => s.Id == scouterId && !s.IsDeleted);

                if (!scouterExists)
                {
                    throw new NotFoundException($"Scouter with ID {scouterId} not found.");
                }

                _logger.LogInformation("No matching shortlisted players found for Scouter {ScouterId}.", scouterId);
                return new PaginatedResult<PlayerCardDto>
                {
                    Items = new List<PlayerCardDto>(),
                    TotalCount = 0,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            }

            var playerCardQuery = _unitOfWork.Repository<PlayerCard>().GetQueryableAsNoTracking();
            var items = await baseQuery
                .OrderByDescending(sl => sl.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(sl => playerCardQuery.FirstOrDefault(pc => pc.PlayerId == sl.PlayerId)) 
                .ProjectTo<PlayerCardDto>(_mapper.ConfigurationProvider) 
                .ToListAsync();

            _logger.LogInformation("Fetched {Count} player card records successfully from Scouter {ScouterId}'s shortlist.", items.Count, scouterId);
            return new PaginatedResult<PlayerCardDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}
