using Koralytics.Application.DTOs.Player;
using Koralytics.Application.DTOs.Scouter;
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
        Task FollowPlayerAsync(int playerId, int scouterId);
        Task UnfollowPlayerAsync(int playerId, int scouterId);
        Task LogProfileViewAsync(int scouterId, int playerId);
        Task<List<PlayerCardDto>> GetFollowedPlayersAsync(int scouterId);
        Task<PlayerProfileViewAnalyticsDto> GetProfileViewsAnalyticsAsync(int playerId);
      
    }
}
