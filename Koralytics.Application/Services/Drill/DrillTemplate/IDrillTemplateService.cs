
using global::Koralytics.Application.DTOs.Drill;
using Koralytics.Application.DTOs.Drill;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Koralytics.Application.Services.Drills.DrillTemplate
{
    public interface IDrillTemplateService
    {
        Task<DrillTemplateDto> CreateTemplateAsync(CreateDrillTemplateDto dto, int currentUserId, string currentUserRole, int? currentUserAcademyId);
        Task<IEnumerable<DrillTemplateDto>> GetTemplatesAsync(int academyId, int currentUserId);
        Task<IEnumerable<DrillTemplateDto>> GetTemplatesByCategoryAsync(int categoryId, int academyId, int currentUserId);
        Task ShareTemplateAsync(int templateId, int currentUserId, string currentUserRole, int? currentUserAcademyId);
    }

 }
