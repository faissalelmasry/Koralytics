using AutoMapper;
using Koralytics.Application.DTOs.ProfileManagement;
using Koralytics.Domain.Entities.Academy;
using CoachEntity = Koralytics.Domain.Entities.Coach.Coach;
using Koralytics.Domain.Entities.Identity;
using Koralytics.Domain.Entities.Parents;
using PlayerEntity = Koralytics.Domain.Entities.Player.Player;
using PlayerPosition = Koralytics.Domain.Entities.Player.PlayerPosition;
using ScouterEntity = Koralytics.Domain.Entities.Scouter.Scouter;
using Koralytics.Domain.Entities.SystemAdmin;
using System;
using System.Linq;

namespace Koralytics.Application.Mappings.ProfileManagement
{
    public class ProfileManagementProfile : Profile
    {
        public ProfileManagementProfile()
        {
            CreateMap<PlayerPosition, PlayerPositionDto>();

            CreateMap<User, BaseUserProfileResponseDto>()
                .ForMember(d => d.Role, o => o.Ignore());

            CreateMap<PlayerEntity, PlayerProfileResponseDto>()
                .IncludeBase<User, BaseUserProfileResponseDto>()
                .ForMember(d => d.Age, o => o.MapFrom(s => CalculateAge(s.DateOfBirth)))
                .ForMember(d => d.Positions, o => o.MapFrom(s => s.PlayerPositions))
                .ForMember(d => d.AcademyId, o => o.MapFrom(s => s.PlayerAcademies.Where(pa => pa.LeftAt == null).Select(pa => (int?)pa.AcademyId).FirstOrDefault()))
                .ForMember(d => d.AcademyName, o => o.MapFrom(s => s.PlayerAcademies.Where(pa => pa.LeftAt == null && pa.Academy != null).Select(pa => pa.Academy.Name).FirstOrDefault()));

            CreateMap<ScouterEntity, ScouterProfileResponseDto>()
                .IncludeBase<User, BaseUserProfileResponseDto>();

            CreateMap<AcademyAdmin, AcademyAdminProfileResponseDto>()
                .IncludeBase<User, BaseUserProfileResponseDto>()
                .ForMember(d => d.AcademyName, o => o.MapFrom(s => s.Academy != null ? s.Academy.Name : null));

            CreateMap<CoachEntity, CoachProfileResponseDto>()
                .IncludeBase<User, BaseUserProfileResponseDto>()
                .ForMember(d => d.AcademyId, o => o.MapFrom(s => s.CoachAcademies.Where(ca => ca.LeftAt == null).Select(ca => (int?)ca.AcademyId).FirstOrDefault()))
                .ForMember(d => d.AcademyName, o => o.MapFrom(s => s.CoachAcademies.Where(ca => ca.LeftAt == null && ca.Academy != null).Select(ca => ca.Academy.Name).FirstOrDefault()));

            CreateMap<Parent, ParentProfileResponseDto>()
                .IncludeBase<User, BaseUserProfileResponseDto>();

            CreateMap<SystemAdminUser, SystemAdminProfileResponseDto>()
                .IncludeBase<User, BaseUserProfileResponseDto>();
        }

        private static int CalculateAge(DateTime dateOfBirth)
        {
            var today = DateTime.UtcNow.Date;
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age)) age--;
            return age;
        }
    }
}
