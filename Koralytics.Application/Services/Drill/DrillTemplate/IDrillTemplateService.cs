
using global::Koralytics.Application.DTOs.Drill;
using Koralytics.Application.DTOs.Drill;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Koralytics.Application.Services.Drill.DrillTemplate
{
    public interface IDrillTemplateService
    {
        Task<DrillTemplateDto> CreateTemplateAsync(CreateDrillTemplateDto dto, int currentUserId, string currentUserRole, int? currentUserAcademyId);
        Task<IEnumerable<DrillTemplateDto>> GetTemplatesAsync(int academyId, int currentUserId, TemplateFilterDto filter);
        Task<IEnumerable<DrillTemplateDto>> GetTemplatesByCategoryAsync(int categoryId, int academyId, int currentUserId, TemplateFilterDto filter);
        Task ShareTemplateAsync(int templateId, int currentUserId, string currentUserRole, int? currentUserAcademyId);
        Task<DrillTemplateDto> GetTemplateByIdAsync(int id, int currentUserId, int? currentUserAcademyId);

        Task<DrillTemplateDto> UpdateTemplateAsync(int id, UpdateDrillTemplateDto dto, int currentUserId, string currentUserRole, int? currentUserAcademyId);

        Task DeleteTemplateAsync(int id, int currentUserId, string currentUserRole, int? currentUserAcademyId);
    }

 }
