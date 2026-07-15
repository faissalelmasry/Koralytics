using Koralytics.Application.DTOs.Player;
using Koralytics.Application.DTOs.Scouter;
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
    public interface IScouterFollowService
    {
        Task FollowPlayerAsync(int scouterId, int playerId);
        Task UnfollowPlayerAsync(int scouterId, int playerId);
        Task LogProfileViewAsync(int scouterId, int playerId);
        Task<PaginatedResult<PlayerCardDto>> GetFollowedPlayersAsync(int scouterId, int pageNumber = 1, int pageSize = 10, string? searchTerm = null);
        Task<PlayerProfileViewAnalyticsDto> GetProfileViewsAnalyticsAsync(int playerId);
      
    }
}
