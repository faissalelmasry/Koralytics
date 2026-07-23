using Koralytics.Application.DTOs.Academies;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Koralytics.Application.Services.Academy.AcademyBadgeService
{
    public interface IAcademyBadgeService
    {
        Task<AcademyBadgeResponseDto> CreateBadgeAsync(int academyId, CreateAcademyBadgeDto dto, int performedByUserId);
        Task<IEnumerable<AcademyBadgeResponseDto>> GetBadgesByAcademyAsync(int academyId);
        Task DeleteBadgeAsync(int academyId, int badgeId, int performedByUserId);
    }
}
