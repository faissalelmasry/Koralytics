using AutoMapper;

using Koralytics.Application.DTOs.AuthDTOs.RegisterDTOs;
using Koralytics.Domain.Entities.Coach;
using Koralytics.Domain.Entities.Parents;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Entities.Scouter;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.Mappings.Auth
{
    public class RegisterProfile : Profile
    {
        public RegisterProfile()
        {
            CreateMap<RegisterPlayerRequestDto, Player>().ReverseMap();
            CreateMap<RegisterParentRequestDto, Parent>().ReverseMap();
            CreateMap<RegisterCoachRequestDto, Coach>().ReverseMap();
            CreateMap<RegisterScouterRequestDto, Scouter>().ReverseMap();
            CreateMap<RegisterAcademyAdminRequestDto, Coach>().ReverseMap();// Change to AcademyAdmin entity when available
        }
    }
}
