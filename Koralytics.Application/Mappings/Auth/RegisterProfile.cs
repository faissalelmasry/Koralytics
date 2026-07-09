using AutoMapper;

using Koralytics.Application.DTOs.AuthDTOs.RegisterDTOs;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Coach;
using Koralytics.Domain.Entities.Parents;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Entities.Scouter;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerEntity = Koralytics.Domain.Entities.Player.Player;

namespace Koralytics.Application.Mappings.Auth
{
    public class RegisterProfile : Profile
    {
        public RegisterProfile()
        {
            CreateMap<RegisterPlayerRequestDto, PlayerEntity>().ReverseMap();
            CreateMap<RegisterParentRequestDto, Parent>().ReverseMap();
            CreateMap<RegisterCoachRequestDto, Coach>().ReverseMap();
            CreateMap<RegisterScouterRequestDto, Scouter>().ReverseMap();
            CreateMap<RegisterAcademyAdminRequestDto, AcademyAdmin>().ReverseMap();
        }
    }
}
