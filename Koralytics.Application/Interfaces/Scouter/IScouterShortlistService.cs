using Koralytics.Application.DTOs.Player;
using Koralytics.Application.DTOs.ScouterDtos;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Entities.Scouter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.Interfaces.Scouter
{
    public interface IScouterShortlistService
    {
        Task<ScouterShortlistDto> AddToShortlistAsync(int scouterId, int playerId);
        Task<bool> RemoveFromShortlistAsync( int scouterId,int playerId);
        Task<PaginatedResult<PlayerCardDto>> GetShortlistAsync( int scouterId, int pageNumber = 1, int pageSize = 10, string? searchTerm = null);
    }
}
