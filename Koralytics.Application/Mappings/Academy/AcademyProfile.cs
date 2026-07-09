using AutoMapper;
using Koralytics.Application.DTOs.Academies;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Coach;

namespace Koralytics.Application.Mappings.Academies
{
    public class AcademyProfile : Profile
    {
        public AcademyProfile()
        {
            // ── Academy ──────────────────────────────────────────────────────
            CreateMap<CreateAcademyDto, Domain.Entities.Academy.Academy>()
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(_ => Domain.Enums.AcademyStatus.Active));

            CreateMap<Domain.Entities.Academy.Academy, AcademyResponseDto>()
                .ForMember(dest => dest.AdminFullName,
                    opt => opt.MapFrom(src =>
                        src.Admin != null
                            ? $"{src.Admin.FirstName} {src.Admin.LastName}"
                            : string.Empty));

            // ── AcademyLocation ──────────────────────────────────────────────
            CreateMap<AddLocationDto, AcademyLocation>();
            CreateMap<AcademyLocation, AcademyLocationResponseDto>();

            // ── AgeGroup ─────────────────────────────────────────────────────
            CreateMap<CreateAgeGroupDto, AgeGroup>();
            CreateMap<AgeGroup, AgeGroupResponseDto>();

            // ── Team ─────────────────────────────────────────────────────────
            CreateMap<CreateTeamDto, Team>();
            CreateMap<Team, TeamResponseDto>()
                .ForMember(dest => dest.AgeGroupName,
                    opt => opt.MapFrom(src =>
                        src.AgeGroup != null ? src.AgeGroup.Name : string.Empty))
                .ForMember(dest => dest.LocationName,
                    opt => opt.MapFrom(src =>
                        src.Location != null ? src.Location.Name : string.Empty))
                .ForMember(dest => dest.CoachName,
                                    opt => opt.MapFrom(src =>
                                    src.Coach != null ? $"{src.Coach.FirstName} {src.Coach.LastName}" : string.Empty));


            // ── AcademyAnnouncement ──────────────────────────────────────────
            CreateMap<SendAnnouncementDto, AcademyAnnouncement>();
            CreateMap<AcademyAnnouncement, AnnouncementResponseDto>()
                .ForMember(dest => dest.SentByFullName,
                    opt => opt.Ignore()); // populated manually in service
        }
    }
}
