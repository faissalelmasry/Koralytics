using AutoMapper;
using Koralytics.Application.Common;
using Koralytics.Application.DTOs.Drill;
using Koralytics.Application.Interfaces;
using Koralytics.Domain.Entities.Drill;
using Microsoft.EntityFrameworkCore;

namespace Koralytics.Application.Services.Drill.DrillTemplate
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
            var categoryExists = await _unitOfWork.Repository<DrillCategory>().ExistsAsync(c => c.Id == dto.CategoryId);
            if (!categoryExists)
            {
                throw new KeyNotFoundException($"Drill Category with ID {dto.CategoryId} does not exist.");
            }
            var template = _mapper.Map<Domain.Entities.Drill.DrillTemplate>(dto);
            template.IsShared = false;

            template.CreatedById = currentUserId;

            if (currentUserRole == "SystemAdmin")
            {
                template.AcademyId = null;
            }
            else
            {
                template.AcademyId = currentUserAcademyId;
            }

            await _unitOfWork.Repository<Domain.Entities.Drill.DrillTemplate>().AddAsync(template);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<DrillTemplateDto>(template);
        }

        public async Task<PagedResult<DrillTemplateDto>> GetTemplatesAsync(int academyId, int currentUserId, TemplateFilterDto filter)
        {
            var query = _unitOfWork.Repository<Domain.Entities.Drill.DrillTemplate>()
                .GetQueryableAsNoTracking()
                .Include(t => t.DrillCategory) // 🟢 ADDED: Eager load the category entity
                .Include(t => t.DrillTemplateAcademy) // 🟢 ADDED: Eager load the academy entity
                .Where(t =>
                    t.AcademyId == null ||
                    (t.AcademyId == academyId && t.IsShared == true) ||
                    (t.AcademyId == academyId && t.CreatedById == currentUserId)
                );

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(t => t.Name.Contains(filter.SearchTerm));
            }

            var totalCount = await query.CountAsync();

            var pagedTemplates = await query
                .OrderByDescending(t => t.Id) 
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PagedResult<DrillTemplateDto>
            {
                Items = _mapper.Map<IReadOnlyList<DrillTemplateDto>>(pagedTemplates),
                Page = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<PagedResult<DrillTemplateDto>> GetTemplatesByCategoryAsync(int categoryId, int academyId, int currentUserId, TemplateFilterDto filter)
        {
            var query = _unitOfWork.Repository<Domain.Entities.Drill.DrillTemplate>()
                .GetQueryableAsNoTracking()
                .Include(t => t.DrillCategory)
                .Include(t => t.DrillTemplateAcademy)
                .Where(t =>
                    t.CategoryId == categoryId && (
                        t.AcademyId == null ||
                        (t.AcademyId == academyId && t.IsShared == true) ||
                        (t.AcademyId == academyId && t.CreatedById == currentUserId)
                    )
                );

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(t => t.Name.Contains(filter.SearchTerm));
            }

            var totalCount = await query.CountAsync();

            var pagedTemplates = await query
                .OrderByDescending(t => t.Id)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PagedResult<DrillTemplateDto>
            {
                Items = _mapper.Map<IReadOnlyList<DrillTemplateDto>>(pagedTemplates),
                Page = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task ShareTemplateAsync(int templateId, int currentUserId, string currentUserRole, int? currentUserAcademyId)
        {
            var template = await _unitOfWork.Repository<Domain.Entities.Drill.DrillTemplate>().GetByIdAsync(templateId);

            if (template != null)
            {
                if (currentUserRole == "SystemAdmin")
                {
                    if (template.AcademyId != null)
                    {
                        throw new UnauthorizedAccessException("SystemAdmins can only share system-wide templates.");
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

                template.IsShared = !template.IsShared;
                template.UpdatedById = currentUserId;

                await _unitOfWork.SaveChangesAsync();
            }
        }
        public async Task<DrillTemplateDto> GetTemplateByIdAsync(int id, int currentUserId, int? currentUserAcademyId)
        {
            var template = await _unitOfWork.Repository<Domain.Entities.Drill.DrillTemplate>()
                .GetQueryableAsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null)
            {
                throw new KeyNotFoundException($"Drill Template with ID {id} was not found.");
            }

            // Security Bouncer: Can this user actually see this template?
            bool isSystemTemplate = template.AcademyId == null;
            bool isOwnAcademyShared = template.AcademyId == currentUserAcademyId && template.IsShared;
            bool isOwner = template.CreatedById == currentUserId;

            if (!isSystemTemplate && !isOwnAcademyShared && !isOwner)
            {
                throw new UnauthorizedAccessException("You do not have permission to view this template.");
            }

            return _mapper.Map<DrillTemplateDto>(template);
        }

        public async Task<DrillTemplateDto> UpdateTemplateAsync(int id, UpdateDrillTemplateDto dto, int currentUserId, string currentUserRole, int? currentUserAcademyId)
        {
            var template = await _unitOfWork.Repository<Domain.Entities.Drill.DrillTemplate>().GetByIdAsync(id);

            if (template == null)
            {
                throw new KeyNotFoundException($"Drill Template with ID {id} was not found.");
            }

            // Security Bouncer: Who is allowed to edit this?
            if (currentUserRole == "SystemAdmin" && template.AcademyId != null)
            {
                throw new UnauthorizedAccessException("SystemAdmins can only edit system-wide templates.");
            }
            else if (currentUserRole == "AcademyAdmin" && template.AcademyId != currentUserAcademyId)
            {
                throw new UnauthorizedAccessException("You can only edit templates belonging to your academy.");
            }
            else if (currentUserRole == "Coach" && template.CreatedById != currentUserId)
            {
                throw new UnauthorizedAccessException("Coaches can only edit their own private templates.");
            }

            // Verify if the new category exists
            var categoryExists = await _unitOfWork.Repository<DrillCategory>().ExistsAsync(c => c.Id == dto.CategoryId);
            if (!categoryExists) throw new KeyNotFoundException($"Category ID {dto.CategoryId} does not exist.");

            // Apply updates
            template.Name = dto.Name;
            template.DrillMode = dto.DrillMode;
            template.CategoryId = dto.CategoryId;
            template.DifficultyLevel = dto.DifficultyLevel;
            template.UpdatedById = currentUserId;

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<DrillTemplateDto>(template);
        }

        public async Task DeleteTemplateAsync(int id, int currentUserId, string currentUserRole, int? currentUserAcademyId)
        {
            var template = await _unitOfWork.Repository<Domain.Entities.Drill.DrillTemplate>().GetByIdAsync(id);

            if (template == null)
            {
                throw new KeyNotFoundException($"Drill Template with ID {id} was not found.");
            }

            if (currentUserRole == "SystemAdmin" && template.AcademyId != null)
            {
                throw new UnauthorizedAccessException("SystemAdmins can only delete system-wide templates.");
            }
            else if (currentUserRole == "AcademyAdmin" && template.AcademyId != currentUserAcademyId)
            {
                throw new UnauthorizedAccessException("You can only delete templates belonging to your academy.");
            }
            else if (currentUserRole == "Coach" && template.CreatedById != currentUserId)
            {
                throw new UnauthorizedAccessException("Coaches can only delete their own private templates.");
            }

            var isTemplateInUse = await _unitOfWork.Repository<Domain.Entities.Drill.Drill>()
                .ExistsAsync(d => d.DrillTemplateId == id);

            if (isTemplateInUse)
            {
                throw new InvalidOperationException("This template cannot be deleted because it is already attached to historical drill sessions. Consider renaming it or marking it as inactive instead.");
            }

            await _unitOfWork.Repository<Domain.Entities.Drill.DrillTemplate>()
                .GetQueryable()
                .Where(t => t.Id == id)
                .ExecuteDeleteAsync();
        }
       
        public async Task<IEnumerable<DrillCategoryDto>> GetCategoriesAsync()
        {
            var categories = await _unitOfWork.Repository<DrillCategory>()
                .GetQueryableAsNoTracking()
                .ToListAsync();

            return _mapper.Map<IEnumerable<DrillCategoryDto>>(categories);
        }
    }
}