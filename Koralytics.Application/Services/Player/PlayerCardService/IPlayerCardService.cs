using Koralytics.Application.DTOs.Player;

namespace Koralytics.Application.Services.Player.PlayerCardService
{
    public interface IPlayerCardService
    {
        Task<PlayerCardDto> GetPlayerCardAsync(int playerId);
        Task RecalculatePlayerCardAsync(int playerId);
        Task<TransferRateDto?> GetDrillToMatchTransferRateAsync(int playerId);
        Task<List<MiniPlayerCardDto?>> GetMiniPlayerCardsAsync(int[] playerIds);
    }
}
