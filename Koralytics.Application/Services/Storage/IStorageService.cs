using Microsoft.AspNetCore.Http;
using Koralytics.Application.DTOs.Player;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Koralytics.Application.Services.Storage
{
    public interface IStorageService
    {
        Task<PlayerHighlightDto> UploadHighlightAsync(int playerId, int academyId, IFormFile file, string? title);
        Task<bool> DeleteHighlightAsync(int highlightId, int playerId);
        Task<bool> PinHighlightAsync(int highlightId, int playerId);
        Task<bool> UnpinHighlightAsync(int highlightId, int playerId);
        Task<IEnumerable<PlayerHighlightDto>> GetHighlightsAsync(int playerId);       
        Task<string> UploadImageAsync(IFormFile file, string folderName = "images");
        Task<bool> DeleteImageAsync(string imageUrl);
        Task<bool> DeleteFileAsync(string fileUrl);
    }
}
