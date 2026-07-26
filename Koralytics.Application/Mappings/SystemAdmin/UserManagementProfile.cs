using AutoMapper;
using Koralytics.Application.DTOs.SystemAdmin;
using Koralytics.Domain.Entities.Identity;

namespace Koralytics.Application.Mappings.SystemAdmin
{
    public class UserManagementProfile : Profile
    {
        public UserManagementProfile()
        {
            CreateMap<User, UserSummaryDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}".Trim()))
                .ForMember(dest => dest.Roles, opt => opt.Ignore()); // Roles are usually populated manually via UserManager

            CreateMap<User, UserDetailDto>()
                .IncludeBase<User, UserSummaryDto>();
        }
    }
}
