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
                .ForMember(dest => dest.SessionDate, opt => opt.MapFrom(src => DateTime.SpecifyKind(src.SessionDate, DateTimeKind.Utc)))
                .ForMember(dest => dest.CoachName, opt => opt.MapFrom(src =>
                    src.DrillSessionCoach != null ? src.DrillSessionCoach.FirstName + " " + src.DrillSessionCoach.LastName : "Unknown Coach"))
                .ForMember(dest => dest.TeamName, opt => opt.MapFrom(src =>
                    src.DrillSessionTeam != null ? src.DrillSessionTeam.Name : "Unknown Team"));

            CreateMap<DrillSession, DrillSessionDetailsDto>()
                .ForMember(dest => dest.SessionDate, opt => opt.MapFrom(src => DateTime.SpecifyKind(src.SessionDate, DateTimeKind.Utc)))
                .ForMember(dest => dest.TeamName, opt => opt.MapFrom(src => src.DrillSessionTeam != null ? src.DrillSessionTeam.Name : string.Empty))
                .ForMember(dest => dest.CoachName, opt => opt.MapFrom(src => src.DrillSessionCoach != null ? $"{src.DrillSessionCoach.FirstName} {src.DrillSessionCoach.LastName}" : string.Empty));
            CreateMap<AddSessionDrillDto, Koralytics.Domain.Entities.Drill.Drill>();
            CreateMap<Koralytics.Domain.Entities.Drill.Drill, DrillDto>()
                .ForMember(dest => dest.TemplateName, opt => opt.MapFrom(src => 
                    src.DrillTemplate != null ? src.DrillTemplate.Name : string.Empty))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => 
                    src.DrillTemplate != null && src.DrillTemplate.DrillCategory != null ? src.DrillTemplate.DrillCategory.Name : string.Empty));
            CreateMap<Koralytics.Domain.Entities.Drill.DrillResult, DrillResultDto>();
            CreateMap<Koralytics.Domain.Entities.Drill.SessionAttendance, PlayerAttendanceDto>()
                .ForMember(dest => dest.PlayerFullName, opt => opt.MapFrom(src => 
                    src.Player != null ? src.Player.FirstName + " " + src.Player.LastName : string.Empty))
                .ForMember(dest => dest.Position, opt => opt.MapFrom(src => 
                    src.Player != null && src.Player.PlayerPositions.Any() ? 
                        (src.Player.PlayerPositions.FirstOrDefault(p => p.IsPrimary) ?? src.Player.PlayerPositions.First()).Position 
                        : null));
            CreateMap<PlayerDrillResultDto, Koralytics.Domain.Entities.Drill.DrillResult>();
        }
    }
}