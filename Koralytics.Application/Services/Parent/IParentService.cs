using System.Collections.Generic;
using System.Threading.Tasks;
using Koralytics.Application.DTOs.Parent;
using Koralytics.Domain.Enums;

namespace Koralytics.Application.Services.Parent
{
    public interface IParentService
    {
        Task<IEnumerable<ParentChildDto>> GetMyChildrenAsync(int parentUserId);
        Task<IEnumerable<ParentPlayerSearchResponseDto>> SearchAvailablePlayersAsync(string? name, int parentUserId);
        Task SendChildJoinRequestAsync(int parentUserId, int playerId);
        Task<IEnumerable<ParentPlayerJoinRequestResponseDto>> GetPendingRequestsForParentAsync(int parentUserId);
        Task CancelChildJoinRequestAsync(int requestId, int parentUserId);
        Task<IEnumerable<ParentPlayerJoinRequestResponseDto>> GetPendingRequestsForPlayerAsync(int playerUserId);
        Task RespondToChildJoinRequestAsync(int requestId, JoinRequestStatus status, int playerUserId);
        Task UnlinkChildAsync(int parentUserId, int playerId);
        Task<IEnumerable<PlayerParentDto>> GetMyParentsAsync(int playerUserId);
        Task UnlinkParentAsync(int playerUserId, int parentId);
    }
}