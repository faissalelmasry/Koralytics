using AutoMapper;
using Koralytics.Application.DTOs.Player;
using Koralytics.Application.DTOs.ScouterDtos;
using Koralytics.Domain.Entities.Match;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Entities.Scouter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.Mappings.ScouterProfile
{
    public class ScouterProfile : Profile
    {
        public ScouterProfile()
        {
            CreateMap<ScouterShortlist, ScouterShortlistDto>()
                 .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                 .ForMember(dest => dest.ScouterUserId, opt => opt.MapFrom(src => src.ScouterUserId))
                 .ForMember(dest => dest.PlayerId, opt => opt.MapFrom(src => src.PlayerId))
                 .ForMember(dest => dest.AddedAt, opt => opt.MapFrom(src => src.AddedAt))
                 .ReverseMap();


        }

    }
}
