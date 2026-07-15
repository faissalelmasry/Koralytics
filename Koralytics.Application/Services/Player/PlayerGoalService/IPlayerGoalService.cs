using Koralytics.Application.DTOs.Player;

namespace Koralytics.Application.Services.Player.PlayerGoalService
{
    public interface IPlayerGoalService
    {
        Task<PlayerGoalDto> CreatePlayerGoalAsync(int playerId, CreatePlayerGoalDto dto);
        Task<PlayerGoalDto> UpdatePlayerGoalAsync(int goalId, UpdatePlayerGoalDto dto);
    }
}
