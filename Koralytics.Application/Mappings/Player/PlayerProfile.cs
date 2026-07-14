using AutoMapper;
using Koralytics.Application.DTOs.Player;
using Koralytics.Domain.Entities.Drill;
using Koralytics.Domain.Entities.Match;
using Koralytics.Domain.Entities.Player;
using PlayerEntity = Koralytics.Domain.Entities.Player.Player;

namespace Koralytics.Application.Mappings.Player
{
    public class PlayerProfile:Profile
    {
        public PlayerProfile()
        {
            CreateMap<PlayerEntity, PlayerProfileDto>()
                .ForMember(d => d.Age, o => o.Ignore())
                .ForMember(d => d.Positions, o => o.Ignore())
                .ForMember(d => d.CurrentAcademy, o => o.Ignore())
                .ForMember(d => d.Teams, o => o.Ignore())
                .ForMember(d => d.PlayerCard, o => o.Ignore())
                .ForMember(d => d.TotalMatches, o => o.Ignore())
                .ForMember(d => d.TotalGoals, o => o.Ignore())
                .ForMember(d => d.TotalAssists, o => o.Ignore())
                .ForMember(d => d.TotalMOTMs, o => o.Ignore())
                .ForMember(d => d.SessionStats, o => o.Ignore())
                .ForMember(d => d.FriendlyStats, o => o.Ignore())
                .ForMember(d => d.TournamentStats, o => o.Ignore());

            CreateMap<PlayerPosition, PlayerPositionDto>();
            CreateMap<PlayerHighlight, PlayerHighlightDto>();

            CreateMap<PlayerAcademy, PlayerAcademyDto>()
                .ForMember(d => d.AcademyName,
                    o => o.MapFrom(s => s.Academy.Name));

            CreateMap<PlayerTeam, PlayerTeamDto>()
                .ForMember(d => d.TeamName,
                    o => o.MapFrom(s => s.Team.Name))
                .ForMember(d => d.AgeGroupName,
                    o => o.MapFrom(s => s.Team.AgeGroup != null ? s.Team.AgeGroup.Name : null));

            CreateMap<PlayerCard, TransferRateDto>()
                .ForMember(d => d.PlayerName,
                    o => o.MapFrom(s => $"{s.Player.FirstName} {s.Player.LastName}"))
                .ForMember(d => d.TransferGap,
                    o => o.MapFrom(s => s.OverallTrainingAvg - s.OverallTournamentAvg))
                .ForMember(d => d.Classification,
                    o => o.MapFrom(s => s.TransferClassification.ToString()));

            CreateMap<DrillResult, TimelineEvent>()
                .ForMember(d => d.Date, o => o.MapFrom(s => s.Drill!.DrillSession!.SessionDate))
                .ForMember(d => d.EventType, o => o.MapFrom(_ => "DrillSession"))
                .ForMember(d => d.Title, o => o.MapFrom(s =>
                    s.Drill!.DrillTemplate != null && s.Drill.DrillTemplate.DrillCategory != null
                        ? s.Drill.DrillTemplate.DrillCategory.Name
                        : "Training Session"))
                .ForMember(d => d.Description, o => o.MapFrom(s => s.Drill!.DrillSession!.Notes))
                .ForMember(d => d.SessionId, o => o.MapFrom(s => s.Drill!.SessionId))
                .ForMember(d => d.SessionType, o => o.MapFrom(s => s.Drill!.DrillSession!.Type.ToString()))
                .ForMember(d => d.DrillCategoryName, o => o.MapFrom(s =>
                    s.Drill!.DrillTemplate != null && s.Drill.DrillTemplate.DrillCategory != null
                        ? s.Drill.DrillTemplate.DrillCategory.Name
                        : null))
                .ForMember(d => d.DrillTemplateName, o => o.MapFrom(s =>
                    s.Drill!.DrillTemplate != null ? s.Drill.DrillTemplate.Name : null))
                .ForMember(d => d.FinalScore, o => o.MapFrom(s => s.FinalScore))
                .ForMember(d => d.DrillNotes, o => o.MapFrom(s => s.CoachNotes));

            CreateMap<MatchPlayerRating, TimelineEvent>()
                .ForMember(d => d.Date, o => o.MapFrom(s => s.Match!.MatchDate))
                .ForMember(d => d.EventType, o => o.MapFrom(_ => "Match"))
                .ForMember(d => d.Title, o => o.MapFrom(s =>
                    (s.Match!.HomeTeam != null ? s.Match.HomeTeam.Name : "TBD") +
                    " vs " +
                    (s.Match.AwayTeam != null ? s.Match.AwayTeam.Name : "TBD")))
                .ForMember(d => d.Description, o => o.MapFrom(s =>
                    s.Match!.HomeScore == 0 && s.Match.AwayScore == 0
                        ? null
                        : $"{s.Match.HomeScore} - {s.Match.AwayScore}"))
                .ForMember(d => d.MatchId, o => o.MapFrom(s => s.Match!.Id))
                .ForMember(d => d.MatchType, o => o.MapFrom(s => s.Match!.Type.ToString()))
                .ForMember(d => d.HomeTeamName, o => o.MapFrom(s =>
                    s.Match!.HomeTeam != null ? s.Match.HomeTeam.Name : null))
                .ForMember(d => d.AwayTeamName, o => o.MapFrom(s =>
                    s.Match!.AwayTeam != null ? s.Match.AwayTeam.Name : null))
                .ForMember(d => d.HomeScore, o => o.MapFrom(s => s.Match!.HomeScore))
                .ForMember(d => d.AwayScore, o => o.MapFrom(s => s.Match!.AwayScore))
                .ForMember(d => d.Goals, o => o.MapFrom(s => s.Goals))
                .ForMember(d => d.Assists, o => o.MapFrom(s => s.Assists))
                .ForMember(d => d.MinutesPlayed, o => o.MapFrom(s => s.MinutesPlayed))
                .ForMember(d => d.IsMOTM, o => o.MapFrom(s => s.IsMOTM))
                .ForMember(d => d.Rating, o => o.MapFrom(s => s.Rating))
                .ForMember(d => d.CoachNote, o => o.MapFrom(s => s.CoachNote));

            CreateMap<PlayerAchievement, TimelineEvent>()
                .ForMember(d => d.Date, o => o.MapFrom(s => s.AwardedAt))
                .ForMember(d => d.EventType, o => o.MapFrom(_ => "Achievement"))
                .ForMember(d => d.Title, o => o.MapFrom(s => s.AchievementType))
                .ForMember(d => d.Description, o => o.MapFrom(s => s.ReferenceType))
                .ForMember(d => d.AchievementId, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.AchievementType, o => o.MapFrom(s => s.AchievementType));

            CreateMap<PlayerGoal, PlayerGoalDto>();

            CreateMap<CreatePlayerGoalDto, PlayerGoal>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.PlayerId, o => o.Ignore())
                .ForMember(d => d.Achieved, o => o.Ignore())
                .ForMember(d => d.Player, o => o.Ignore())
                .ForMember(d => d.Academy, o => o.Ignore())
                .ForMember(d => d.CreatedAt, o => o.Ignore())
                .ForMember(d => d.CreatedById, o => o.Ignore())
                .ForMember(d => d.CreatedByUser, o => o.Ignore())
                .ForMember(d => d.IsDeleted, o => o.Ignore());
        }
    }
}
