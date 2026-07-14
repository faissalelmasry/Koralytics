using AutoMapper;
using Koralytics.Application.DTOs.Match;
using MatchEntity = Koralytics.Domain.Entities.Match.Match;
using MatchEventEntity = Koralytics.Domain.Entities.Match.MatchEvent;
using MatchLineupEntity = Koralytics.Domain.Entities.Match.MatchLineup;
using MatchPlayerRatingEntity = Koralytics.Domain.Entities.Match.MatchPlayerRating;
using MatchRequestEntity = Koralytics.Domain.Entities.Match.MatchRequest;
using DomainEnums = Koralytics.Domain.Enums;

namespace Koralytics.Application.Mappings.Match
{
    public class MatchProfile : Profile
    {
        public MatchProfile()
        {
            CreateMap<CreateFriendlyMatchDto, MatchEntity>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.Type, o => o.Ignore())
                .ForMember(d => d.Status, o => o.Ignore())
                .ForMember(d => d.HomeScore, o => o.Ignore())
                .ForMember(d => d.AwayScore, o => o.Ignore())
                .ForMember(d => d.HomePenaltyScore, o => o.Ignore())
                .ForMember(d => d.AwayPenaltyScore, o => o.Ignore())
                .ForMember(d => d.WinningTeamId, o => o.Ignore())
                .ForMember(d => d.TournamentId, o => o.Ignore())
                .ForMember(d => d.SessionId, o => o.Ignore())
                .ForMember(d => d.HomeTeam, o => o.Ignore())
                .ForMember(d => d.AwayTeam, o => o.Ignore())
                .ForMember(d => d.WinningTeam, o => o.Ignore())
                .ForMember(d => d.Tournament, o => o.Ignore())
                .ForMember(d => d.Session, o => o.Ignore())
                .ForMember(d => d.MatchLineups, o => o.Ignore())
                .ForMember(d => d.MatchEvents, o => o.Ignore())
                .ForMember(d => d.MatchPlayerRatings, o => o.Ignore())
                .ForMember(d => d.CreatedAt, o => o.Ignore())
                .ForMember(d => d.CreatedById, o => o.Ignore())
                .ForMember(d => d.CreatedByUser, o => o.Ignore())
                .ForMember(d => d.UpdatedAt, o => o.Ignore())
                .ForMember(d => d.UpdatedById, o => o.Ignore())
                .ForMember(d => d.UpdatedByUser, o => o.Ignore())
                .ForMember(d => d.IsDeleted, o => o.Ignore());

            CreateMap<CreateTournamentMatchDto, MatchEntity>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.Type, o => o.Ignore())
                .ForMember(d => d.Status, o => o.Ignore())
                .ForMember(d => d.HomeScore, o => o.Ignore())
                .ForMember(d => d.AwayScore, o => o.Ignore())
                .ForMember(d => d.HomePenaltyScore, o => o.Ignore())
                .ForMember(d => d.AwayPenaltyScore, o => o.Ignore())
                .ForMember(d => d.WinningTeamId, o => o.Ignore())
                .ForMember(d => d.TournamentId, o => o.Ignore())
                .ForMember(d => d.SessionId, o => o.Ignore())
                .ForMember(d => d.HomeTeam, o => o.Ignore())
                .ForMember(d => d.AwayTeam, o => o.Ignore())
                .ForMember(d => d.WinningTeam, o => o.Ignore())
                .ForMember(d => d.Tournament, o => o.Ignore())
                .ForMember(d => d.Session, o => o.Ignore())
                .ForMember(d => d.MatchLineups, o => o.Ignore())
                .ForMember(d => d.MatchEvents, o => o.Ignore())
                .ForMember(d => d.MatchPlayerRatings, o => o.Ignore())
                .ForMember(d => d.CreatedAt, o => o.Ignore())
                .ForMember(d => d.CreatedById, o => o.Ignore())
                .ForMember(d => d.CreatedByUser, o => o.Ignore())
                .ForMember(d => d.UpdatedAt, o => o.Ignore())
                .ForMember(d => d.UpdatedById, o => o.Ignore())
                .ForMember(d => d.UpdatedByUser, o => o.Ignore())
                .ForMember(d => d.IsDeleted, o => o.Ignore());

            CreateMap<CreateSessionMatchDto, MatchEntity>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.HomeTeamId, o => o.Ignore())
                .ForMember(d => d.AwayTeamId, o => o.Ignore())
                .ForMember(d => d.Type, o => o.Ignore())
                .ForMember(d => d.Status, o => o.Ignore())
                .ForMember(d => d.HomeScore, o => o.Ignore())
                .ForMember(d => d.AwayScore, o => o.Ignore())
                .ForMember(d => d.HomePenaltyScore, o => o.Ignore())
                .ForMember(d => d.AwayPenaltyScore, o => o.Ignore())
                .ForMember(d => d.WinningTeamId, o => o.Ignore())
                .ForMember(d => d.TournamentId, o => o.Ignore())
                .ForMember(d => d.HomeTeam, o => o.Ignore())
                .ForMember(d => d.AwayTeam, o => o.Ignore())
                .ForMember(d => d.WinningTeam, o => o.Ignore())
                .ForMember(d => d.Tournament, o => o.Ignore())
                .ForMember(d => d.Session, o => o.Ignore())
                .ForMember(d => d.MatchLineups, o => o.Ignore())
                .ForMember(d => d.MatchEvents, o => o.Ignore())
                .ForMember(d => d.MatchPlayerRatings, o => o.Ignore())
                .ForMember(d => d.CreatedAt, o => o.Ignore())
                .ForMember(d => d.CreatedById, o => o.Ignore())
                .ForMember(d => d.CreatedByUser, o => o.Ignore())
                .ForMember(d => d.UpdatedAt, o => o.Ignore())
                .ForMember(d => d.UpdatedById, o => o.Ignore())
                .ForMember(d => d.UpdatedByUser, o => o.Ignore())
                .ForMember(d => d.IsDeleted, o => o.Ignore());

            CreateMap<MatchEntity, MatchResponseDto>()
                .ForMember(d => d.Type, o => o.MapFrom(s => s.Type.ToString()))
                .ForMember(d => d.Format, o => o.MapFrom(s => s.Format.ToString()))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.HomeTeamName, o => o.MapFrom(s => s.HomeTeam.Name))
                .ForMember(d => d.AwayTeamName, o => o.MapFrom(s => s.AwayTeam.Name));

            CreateMap<LogMatchEventDto, MatchEventEntity>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.MatchId, o => o.Ignore())
                .ForMember(d => d.Match, o => o.Ignore())
                .ForMember(d => d.Team, o => o.Ignore())
                .ForMember(d => d.Player, o => o.Ignore())
                .ForMember(d => d.AssistPlayer, o => o.Ignore())
                .ForMember(d => d.IsHomeSide, o => o.Ignore())
                .ForMember(d => d.CreatedAt, o => o.Ignore())
                .ForMember(d => d.CreatedById, o => o.Ignore())
                .ForMember(d => d.CreatedByUser, o => o.Ignore())
                .ForMember(d => d.UpdatedAt, o => o.Ignore())
                .ForMember(d => d.UpdatedById, o => o.Ignore())
                .ForMember(d => d.UpdatedByUser, o => o.Ignore())
                .ForMember(d => d.IsDeleted, o => o.Ignore());

            CreateMap<MatchEventEntity, MatchEventResponseDto>()
                .ForMember(d => d.TeamName, o => o.MapFrom(s => s.Team.Name))
                .ForMember(d => d.PlayerName, o => o.MapFrom(s => s.Player.FirstName + " " + s.Player.LastName))
                .ForMember(d => d.AssistPlayerName, o => o.MapFrom(s => s.AssistPlayer != null ? s.AssistPlayer.FirstName + " " + s.AssistPlayer.LastName : null))
                .ForMember(d => d.EventType, o => o.MapFrom(s => s.EventType.ToString()));

            CreateMap<SubmitLineupPlayerDto, MatchLineupEntity>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.MatchId, o => o.Ignore())
                .ForMember(d => d.Match, o => o.Ignore())
                .ForMember(d => d.Player, o => o.Ignore())
                .ForMember(d => d.Team, o => o.Ignore())
                .ForMember(d => d.CreatedAt, o => o.Ignore())
                .ForMember(d => d.CreatedById, o => o.Ignore())
                .ForMember(d => d.CreatedByUser, o => o.Ignore())
                .ForMember(d => d.UpdatedAt, o => o.Ignore())
                .ForMember(d => d.UpdatedById, o => o.Ignore())
                .ForMember(d => d.UpdatedByUser, o => o.Ignore())
                .ForMember(d => d.IsDeleted, o => o.Ignore());

            CreateMap<MatchLineupEntity, LineupResponseDto>()
                .ForMember(d => d.PlayerName, o => o.MapFrom(s => s.Player.FirstName + " " + s.Player.LastName))
                .ForMember(d => d.TeamName, o => o.MapFrom(s => s.Team.Name));

            CreateMap<MatchPlayerRatingEntity, MatchPlayerRatingDto>()
                .ForMember(d => d.PlayerName, o => o.Ignore())
                .ForMember(d => d.CoachName, o => o.Ignore())
                .ForMember(d => d.CategoryRatings, o => o.Ignore());

            CreateMap<MatchRequestEntity, MatchRequestResponseDto>()
                .ForMember(d => d.RequesterTeamName, o => o.MapFrom(s => s.RequesterTeam.Name))
                .ForMember(d => d.TargetTeamName, o => o.MapFrom(s => s.TargetTeam.Name))
                .ForMember(d => d.RequesterCoachName, o => o.MapFrom(s => s.RequesterCoach.FirstName + " " + s.RequesterCoach.LastName))
                .ForMember(d => d.ResolvedByCoachName, o => o.MapFrom(s => s.ResolvedByCoach != null ? s.ResolvedByCoach.FirstName + " " + s.ResolvedByCoach.LastName : null))
                .ForMember(d => d.Format, o => o.MapFrom(s => s.Format.ToString()))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));
        }
    }
}
