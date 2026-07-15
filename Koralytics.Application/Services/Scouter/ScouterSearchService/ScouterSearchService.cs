
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
using Koralytics.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using ScouterEntity = Koralytics.Domain.Entities.Scouter.Scouter;

namespace Koralytics.Application.Services.Scouter.ScouterSearchService
{
    public class ScouterSearchService : IScouterSearchService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ScouterSearchService> _logger;

        public ScouterSearchService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<ScouterSearchService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }
        public async Task<PaginatedResult<PlayerCardDto>> SearchPlayersAsync(PlayerSearchFiltersDto filters)
        {

            _logger.LogInformation("Executing structured player search. Search filters input payload received.");

            var query = _unitOfWork.Repository<Domain.Entities.Player.Player>().GetQueryableAsNoTracking();
            var today = DateTime.UtcNow.Date;

            if (filters != null)
            {
               
                if (filters.MinAge != null)
                {
                    query = query.Where(p => today.Year - p.DateOfBirth.Year -
                        (today.Month < p.DateOfBirth.Month || (today.Month == p.DateOfBirth.Month && today.Day < p.DateOfBirth.Day) ? 1 : 0) >= filters.MinAge);
                }

                if (filters.MaxAge != null)
                {
                    query = query.Where(p => today.Year - p.DateOfBirth.Year -
                        (today.Month < p.DateOfBirth.Month || (today.Month == p.DateOfBirth.Month && today.Day < p.DateOfBirth.Day) ? 1 : 0) <= filters.MaxAge);
                }

                if (filters.PreferredFoot != null) query = query.Where(p => p.PreferredFoot == filters.PreferredFoot);
                if (filters.Positions != null && filters.Positions.Any()) query = query.Where(p => p.PlayerPositions.Any(pp => filters.Positions.Contains(pp.Position)));
                if (filters.AcademyId != null) query = query.Where(p => p.PlayerAcademies.OrderByDescending(pa => pa.Id).Select(pa => (int?)pa.AcademyId).FirstOrDefault() == filters.AcademyId);
                if (filters.Format != null) query = query.Where(p => _unitOfWork.Repository<MatchLineup>().GetQueryableAsNoTracking().Any(ml => ml.PlayerId == p.Id && ml.Match.Format == filters.Format));

                if (filters.MinRating != null)
                {
                    query = query.Where(p => _unitOfWork.Repository<PlayerCard>().GetQueryableAsNoTracking()
                        .Where(pc => pc.PlayerId == p.Id).Select(pc => pc.OverallRating).FirstOrDefault() >= filters.MinRating);
                }

                if (filters.MaxRating != null)
                {
                    query = query.Where(p => _unitOfWork.Repository<PlayerCard>().GetQueryableAsNoTracking()
                        .Where(pc => pc.PlayerId == p.Id).Select(pc => pc.OverallRating).FirstOrDefault() <= filters.MaxRating);
                }
            }

            int totalCount = await query.CountAsync();

            if (totalCount == 0 || filters == null)
            {
                _logger.LogInformation("No players matched the specified search matrix filters.");
                return new PaginatedResult<PlayerCardDto>
                {
                    Items = new List<PlayerCardDto>(),
                    TotalCount = 0,
                    PageNumber = filters?.PageNumber ?? 1,
                    PageSize = filters?.PageSize ?? 10
                };
            }
            var playerCardQuery = _unitOfWork.Repository<PlayerCard>().GetQueryableAsNoTracking();

           
            var playerCardDtos = await query
                .OrderByDescending(p => p.Id)
                .Skip((filters.PageNumber - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .Select(p => playerCardQuery.FirstOrDefault(pc => pc.PlayerId == p.Id)) 
                .ProjectTo<PlayerCardDto>(_mapper.ConfigurationProvider) 
                .ToListAsync();

            _logger.LogInformation("Successfully completed player search execution. Returned {Count} item records.", playerCardDtos.Count);

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
            _logger.LogInformation("Retrieving profile for ScouterId: {ScouterId}", scouterId);
            var scouterDto = await _unitOfWork.Repository<ScouterEntity>()
                .GetQueryableAsNoTracking()
                .Where(s => s.Id == scouterId)
                .ProjectTo<ScouterProfileDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            if (scouterDto == null)
            {
                throw new NotFoundException($"Scouter with ID {scouterId} was not found.");
            }

            _logger.LogInformation("Successfully loaded profile for ScouterId: {ScouterId}", scouterId);
            return scouterDto;
        }
    }
}
