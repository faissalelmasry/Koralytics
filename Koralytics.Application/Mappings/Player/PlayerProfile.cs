using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Koralytics.Application.DTOs.Player;
using Koralytics.Domain.Entities.Player;

namespace Koralytics.Application.Mappings.Player
{
    public class PlayerProfile:Profile
    {
        public PlayerProfile()
        {

            CreateMap<PlayerCard, TransferRateDto>()
                .ForMember(d => d.PlayerName,
                    o => o.MapFrom(s => $"{s.Player.FirstName} {s.Player.LastName}"))
                .ForMember(d => d.TransferGap,
                    o => o.MapFrom(s => s.OverallTrainingAvg - s.OverallTournamentAvg))
                .ForMember(d => d.Classification,
                    o => o.MapFrom(s => s.TransferClassification.ToString()));
        }
    }
}
