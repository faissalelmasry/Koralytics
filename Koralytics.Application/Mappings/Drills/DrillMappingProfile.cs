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
        }
    }
}