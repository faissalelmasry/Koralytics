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
          
                CreateMap<PlayerCard, PlayerCardDto>()
                    .ForMember(d => d.PlayerName, opt => opt.MapFrom(s => $"{s.Player.FirstName} {s.Player.LastName}"))
                    .ForMember(d => d.TransferClassification, opt => opt.MapFrom(s => s.TransferClassification.ToString()))
                    .ForMember(d => d.Position, opt => opt.MapFrom(s => s.Player.PlayerPositions.FirstOrDefault(p => p.IsPrimary).Position.ToString() ?? string.Empty))
                    .ForMember(d => d.PreferredFoot, opt => opt.MapFrom(s => s.Player.PreferredFoot))
                    .ForMember(d => d.WeakFootRating, opt => opt.MapFrom(s => s.Player.WeakFootRating))
                    .ForMember(d => d.ArchetypePlayerName, opt => opt.MapFrom(s => s.Player.ArchetypePlayerName))
                    .ForMember(d => d.PlayStyleTag, opt => opt.MapFrom(s => s.Player.PlayStyleTag))
                    .ForMember(d => d.ProfileImageUrl, opt => opt.MapFrom(s => s.Player.ProfileImageUrl))
                    // Map the category attributes using your teammate's switch/conditional logic
                    .ForMember(d => d.PassingRating, opt => opt.MapFrom(s => s.CategoryRatings.Where(r => r.DrillCategory.Name == "Passing").Select(r => (decimal?)r.Score).FirstOrDefault() ?? 0))
    .ForMember(d => d.ShootingRating, opt => opt.MapFrom(s => s.CategoryRatings.Where(r => r.DrillCategory.Name == "Shooting").Select(r => (decimal?)r.Score).FirstOrDefault() ?? 0))
    .ForMember(d => d.DribblingRating, opt => opt.MapFrom(s => s.CategoryRatings.Where(r => r.DrillCategory.Name == "Dribbling").Select(r => (decimal?)r.Score).FirstOrDefault() ?? 0))
    .ForMember(d => d.DefendingRating, opt => opt.MapFrom(s => s.CategoryRatings.Where(r => r.DrillCategory.Name == "Defending").Select(r => (decimal?)r.Score).FirstOrDefault() ?? 0))
    .ForMember(d => d.PaceRating, opt => opt.MapFrom(s => s.CategoryRatings.Where(r => r.DrillCategory.Name == "Speed").Select(r => (decimal?)r.Score).FirstOrDefault() ?? 0))
    .ForMember(d => d.PhysicalRating, opt => opt.MapFrom(s => s.CategoryRatings.Where(r => r.DrillCategory.Name == "Physical").Select(r => (decimal?)r.Score).FirstOrDefault() ?? 0))
    .ForMember(d => d.GoalkeepingRating, opt => opt.MapFrom(s => s.CategoryRatings.Where(r => r.DrillCategory.Name == "GoalKeeping").Select(r => (decimal?)r.Score).FirstOrDefault() ?? 0));

        }
        
    }

    }

