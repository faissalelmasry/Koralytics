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
                .ForMember(d => d.Positions, o => o.MapFrom(s => s.PlayerPositions));

            CreateMap<ScouterEntity, ScouterProfileResponseDto>()
                .IncludeBase<User, BaseUserProfileResponseDto>();

            CreateMap<AcademyAdmin, AcademyAdminProfileResponseDto>()
                .IncludeBase<User, BaseUserProfileResponseDto>()
                .ForMember(d => d.AcademyName, o => o.MapFrom(s => s.Academy != null ? s.Academy.Name : null));

            CreateMap<CoachEntity, CoachProfileResponseDto>()
                .IncludeBase<User, BaseUserProfileResponseDto>();

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
