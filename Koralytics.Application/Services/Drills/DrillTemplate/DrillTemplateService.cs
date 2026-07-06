using AutoMapper;
using Koralytics.Application.DTOs.Drill;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Services.Drills.DrillTemplate;
using Koralytics.Domain.Entities.Drill;

namespace Koralytics.Application.Services.Drill
{
    public class DrillTemplateService : IDrillTemplateService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public DrillTemplateService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<DrillTemplateDto> CreateTemplateAsync(CreateDrillTemplateDto dto, int currentUserId, string currentUserRole, int? currentUserAcademyId)
        {
            var template = _mapper.Map<DrillTemplate>(dto);
            template.IsShared = false;

            template.CreatedById = currentUserId;

            if (currentUserRole == "SuperAdmin")
            {
                template.AcademyId = null;
            }
            else
            {
                template.AcademyId = currentUserAcademyId;
            }

            await _unitOfWork.Repository<DrillTemplate>().AddAsync(template);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<DrillTemplateDto>(template);
        }

        public async Task<IEnumerable<DrillTemplateDto>> GetTemplatesAsync(int academyId, int currentUserId)
        {
            var templates = await _unitOfWork.Repository<DrillTemplate>()
                .FindAllAsNoTrackingAsync(t =>
                    t.AcademyId == null ||
                    (t.AcademyId == academyId && t.IsShared == true) ||
                    (t.AcademyId == academyId && t.CreatedById == currentUserId)
                );

            return _mapper.Map<IEnumerable<DrillTemplateDto>>(templates);
        }

        public async Task<IEnumerable<DrillTemplateDto>> GetTemplatesByCategoryAsync(int categoryId, int academyId, int currentUserId)
        {
            var templates = await _unitOfWork.Repository<DrillTemplate>()
                .FindAllAsNoTrackingAsync(t =>
                    t.CategoryId == categoryId && (
                        t.AcademyId == null ||
                        (t.AcademyId == academyId && t.IsShared == true) ||
                        (t.AcademyId == academyId && t.CreatedById == currentUserId)
                    )
                );

            return _mapper.Map<IEnumerable<DrillTemplateDto>>(templates);
        }

        public async Task ShareTemplateAsync(int templateId, int currentUserId, string currentUserRole, int? currentUserAcademyId)
        {
            var template = await _unitOfWork.Repository<DrillTemplate>().GetByIdAsync(templateId);

            if (template != null)
            {
                if (currentUserRole == "SuperAdmin")
                {
                    if (template.AcademyId != null)
                    {
                        throw new UnauthorizedAccessException("SuperAdmins can only share system-wide templates.");
                    }
                }
                else if (currentUserRole == "AcademyAdmin")
                {
                    if (template.AcademyId != currentUserAcademyId)
                    {
                        throw new UnauthorizedAccessException("You can only share templates belonging to your academy.");
                    }
                }
                else if (currentUserRole == "Coach")
                {
                    if (template.CreatedById != currentUserId)
                    {
                        throw new UnauthorizedAccessException("Coaches can only share their own private templates.");
                    }
                }

                template.IsShared = true;
                template.UpdatedById = currentUserId;

                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}