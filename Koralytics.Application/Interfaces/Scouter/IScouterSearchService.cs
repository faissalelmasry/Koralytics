
using Koralytics.Application.DTOs.Player;
using Koralytics.Application.DTOs.ScouterDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.Interfaces.ScouterInterfaces
{
    public interface IScouterSearchService
    {
       Task<PaginatedResult<PlayerCardDto>> SearchPlayersAsync(PlayerSearchFiltersDto filters);

    }
}
