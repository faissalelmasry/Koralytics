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
            CreateMap<DrillTemplate, DrillTemplateDto>();

            CreateMap<CreateDrillSessionDto, DrillSession>();
            CreateMap<DrillSession, DrillSessionDto>();

            CreateMap<DrillSession, DrillSessionDetailsDto>();
            CreateMap<AddSessionDrillDto, Koralytics.Domain.Entities.Drill.Drill>();
            CreateMap<Koralytics.Domain.Entities.Drill.Drill, DrillDto>();
            CreateMap<Koralytics.Domain.Entities.Drill.DrillResult, DrillResultDto>();
            CreateMap<Koralytics.Domain.Entities.Drill.SessionAttendance, PlayerAttendanceDto>();
            CreateMap<PlayerDrillResultDto, Koralytics.Domain.Entities.Drill.DrillResult>();
        }
    }
}