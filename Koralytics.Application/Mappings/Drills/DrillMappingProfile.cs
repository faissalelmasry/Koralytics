using AutoMapper;
using Koralytics.Application.DTOs.Drill;
using Koralytics.Domain.Entities.Drill;

namespace Koralytics.Application.Mappings.Drills
{
    public class DrillMappingProfile : Profile
    {
        public DrillMappingProfile()
        {
            CreateMap<CreateDrillTemplateDto, DrillTemplate>();

            // Map DrillTemplate → DrillTemplateDto, resolving CategoryName and AcademyName from navigation properties
            CreateMap<DrillTemplate, DrillTemplateDto>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src =>
                    src.DrillCategory != null ? src.DrillCategory.Name : string.Empty))
                .ForMember(dest => dest.AcademyName, opt => opt.MapFrom(src =>
                    src.DrillTemplateAcademy != null ? src.DrillTemplateAcademy.Name : string.Empty));

            // ✅ ADDED: This was missing — caused 500 on /api/drills/categories
            CreateMap<DrillCategory, DrillCategoryDto>();

            CreateMap<CreateDrillSessionDto, DrillSession>();
            CreateMap<DrillSession, DrillSessionDto>()
                .ForMember(dest => dest.CoachName, opt => opt.MapFrom(src =>
                    src.DrillSessionCoach != null ? src.DrillSessionCoach.FirstName + " " + src.DrillSessionCoach.LastName : "Unknown Coach"))
                .ForMember(dest => dest.TeamName, opt => opt.MapFrom(src =>
                    src.DrillSessionTeam != null ? src.DrillSessionTeam.Name : "Unknown Team"));

            CreateMap<DrillSession, DrillSessionDetailsDto>();
            CreateMap<AddSessionDrillDto, Koralytics.Domain.Entities.Drill.Drill>();
            CreateMap<Koralytics.Domain.Entities.Drill.Drill, DrillDto>();
            CreateMap<Koralytics.Domain.Entities.Drill.DrillResult, DrillResultDto>();
            CreateMap<Koralytics.Domain.Entities.Drill.SessionAttendance, PlayerAttendanceDto>();
            CreateMap<PlayerDrillResultDto, Koralytics.Domain.Entities.Drill.DrillResult>();
        }
    }
}