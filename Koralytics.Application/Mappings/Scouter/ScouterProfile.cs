using AutoMapper;
using Koralytics.Application.DTOs.Player;
using Koralytics.Application.DTOs.Scouter;
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
            CreateMap<ScouterView, ProfileViewerDetailDto>()
                .ForMember(dest => dest.ScouterId, opt => opt.MapFrom(src => src.ScouterId))
                .ForMember(dest => dest.ViewedAt, opt => opt.MapFrom(src => src.CreatedAt)) 
                .ForMember(dest => dest.IsScouterVerified, opt => opt.MapFrom(src => src.Scouter.IsVerified))
                .ForMember(dest => dest.ScouterName, opt => opt.MapFrom(src => $"{src.Scouter.FirstName} {src.Scouter.LastName}"));
               

         
            CreateMap<Domain.Entities.Player.Player, PlayerProfileViewAnalyticsDto>()
                .ForMember(dest => dest.TotalViewsCount, opt => opt.MapFrom(src => src.ScouterViews.Count))
                .ForMember(dest => dest.RecentViews, opt => opt.MapFrom(src => src.ScouterViews
                    .OrderByDescending(v => v.CreatedAt)
                    .Take(10)));

            CreateMap<Scouter, ScouterProfileDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.IsVerified, opt => opt.MapFrom(src => src.IsVerified))
                .ForMember(dest => dest.VerifiedAt, opt => opt.MapFrom(src => src.VerifiedAt))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));
        }

    }
}
