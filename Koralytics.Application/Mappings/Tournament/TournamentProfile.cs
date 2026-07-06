using AutoMapper;
using Koralytics.Application.DTOs.Tournament;
using Koralytics.Domain.Entities.Tournamet;
namespace Koralytics.Application.Mappings.Tournaments
{
    public class TournamentProfile:Profile
    {
        public TournamentProfile()
        {
            CreateMap<CreateTournamentDto, Tournament>();

            CreateMap<Tournament, TournamentDto>()
                .ForMember(dest => dest.AgeGroupName,
                    opt => opt.MapFrom(src => src.AgeGroup.Name));
        }
    }
}
