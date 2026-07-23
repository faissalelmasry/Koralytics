using AutoMapper;
using Koralytics.Application.DTOs.Player;
using Koralytics.Application.Interfaces;
using Koralytics.Domain.Entities.Academy;
using Koralytics.Domain.Entities.Drill;
using Koralytics.Domain.Entities.Match;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Enums;
using Koralytics.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using PlayerAchievementEntity = Koralytics.Domain.Entities.Player.PlayerAchievement;
using PlayerEntity = Koralytics.Domain.Entities.Player.Player;
using MatchEntity = Koralytics.Domain.Entities.Match.Match;

namespace Koralytics.Application.Services.Player.PlayerProfileServices
{
    public class PlayerProfileService : IPlayerProfileService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PlayerProfileService> _logger;
        private readonly IMapper _mapper;

        public PlayerProfileService(
            IUnitOfWork unitOfWork,
            ILogger<PlayerProfileService> logger,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<PlayerProfileDto> GetPlayerProfileAsync(int playerId)
        {
            _logger.LogInformation("Fetching profile for player {PlayerId}", playerId);

            var player = await _unitOfWork.Repository<PlayerEntity>()
                .GetQueryableAsNoTracking()
                .Include(p => p.PlayerPositions)
                .Include(p => p.PlayerAcademies.Where(pa => pa.LeftAt == null))
                    .ThenInclude(pa => pa.Academy)
                .Include(p => p.PlayerTeams.Where(pt => pt.LeftAt == null))
                    .ThenInclude(pt => pt.Team)
                    .ThenInclude(t => t.AgeGroup)
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player is null)
                throw new NotFoundException($"Player with id {playerId} was not found");

            PlayerCardDto? playerCard = null;

            var cardEntity = await _unitOfWork.Repository<PlayerCard>()
                .GetQueryableAsNoTracking()
                .Include(pc => pc.CategoryRatings)
                    .ThenInclude(cr => cr.DrillCategory)
                .FirstOrDefaultAsync(pc => pc.PlayerId == playerId);

            if (cardEntity is not null)
            {
                playerCard = MapPlayerCardToDto(cardEntity, player);
            }

            var profile = _mapper.Map<PlayerProfileDto>(player);
            profile.Age = CalculateAge(player.DateOfBirth);
            profile.Positions = _mapper.Map<List<PlayerPositionDto>>(player.PlayerPositions);
            profile.CurrentAcademy = _mapper.Map<PlayerAcademyDto>(
                player.PlayerAcademies.FirstOrDefault(pa => pa.LeftAt == null));
            profile.Teams = _mapper.Map<List<PlayerTeamDto>>(
                player.PlayerTeams.Where(pt => pt.LeftAt == null));
            profile.PlayerCard = playerCard;

            var matchQuery = _unitOfWork.Repository<MatchPlayerRating>()
                .GetQueryableAsNoTracking()
                .Where(mpr => mpr.PlayerId == playerId && mpr.Match != null);

            profile.TotalMatches = await matchQuery.Select(mpr => mpr.MatchId).Distinct().CountAsync();

            var matchStats = await matchQuery
                .GroupBy(mpr => mpr.Match!.Type)
                .Select(g => new
                {
                    MatchType = g.Key,
                    Goals = g.Sum(mpr => mpr.Goals),
                    Assists = g.Sum(mpr => mpr.Assists),
                    MOTMs = g.Count(mpr => mpr.IsMOTM),
                })
                .ToListAsync();

            var typeMatches = await matchQuery
                .Select(mpr => new { mpr.MatchId, mpr.Match!.Type })
                .Distinct()
                .GroupBy(x => x.Type)
                .Select(g => new { MatchType = g.Key, Matches = g.Count() })
                .ToListAsync();

            var typeMatchDict = typeMatches.ToDictionary(x => x.MatchType, x => x.Matches);

            var sessionStats = new MatchTypeStats();
            var friendlyStats = new MatchTypeStats();
            var tournamentStats = new MatchTypeStats();

            foreach (var stat in matchStats)
            {
                profile.TotalGoals += stat.Goals;
                profile.TotalAssists += stat.Assists;
                profile.TotalMOTMs += stat.MOTMs;

                var typeStat = new MatchTypeStats
                {
                    Matches = typeMatchDict.TryGetValue(stat.MatchType, out var m) ? m : 0,
                    Goals = stat.Goals,
                    Assists = stat.Assists,
                    MOTMs = stat.MOTMs,
                };

                if (stat.MatchType == Domain.Enums.MatchType.Session)
                    sessionStats = typeStat;
                else if (stat.MatchType == Domain.Enums.MatchType.Friendly)
                    friendlyStats = typeStat;
                else if (stat.MatchType == Domain.Enums.MatchType.Tournament)
                    tournamentStats = typeStat;
            }

            profile.SessionStats = sessionStats;
            profile.FriendlyStats = friendlyStats;
            profile.TournamentStats = tournamentStats;

            return profile;
        }

        private static int CalculateAge(DateTime dateOfBirth)
        {
            var today = DateTime.Today;
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age))
                age--;
            return age;
        }

        private static PlayerCardDto MapPlayerCardToDto(PlayerCard card, PlayerEntity player)
        {
            var dto = new PlayerCardDto
            {
                PlayerName = $"{player.FirstName} {player.LastName}",
                OverallRating = card.OverallRating,
                OverallTrainingAvg = card.OverallTrainingAvg,
                OverallTournamentAvg = card.OverallTournamentAvg,
                TransferClassification = card.TransferClassification.ToString(),
                Position = player.PlayerPositions
                    .FirstOrDefault(p => p.IsPrimary)?.Position ?? string.Empty,
                PreferredFoot = player.PreferredFoot,
                WeakFootRating = player.WeakFootRating,
                ArchetypePlayerName = player.ArchetypePlayerName,
                PlayStyleTag = player.PlayStyleTag,
                ProfileImageUrl = player.ProfileImageUrl,
            };

            foreach (var rating in card.CategoryRatings ?? Enumerable.Empty<PlayerCategoryRating>())
            {
                switch (rating.DrillCategory?.Name)
                {
                    case "Passing": dto.PassingRating = rating.Score; break;
                    case "Shooting": dto.ShootingRating = rating.Score; break;
                    case "Dribbling": dto.DribblingRating = rating.Score; break;
                    case "Defending": dto.DefendingRating = rating.Score; break;
                    case "Speed": dto.PaceRating = rating.Score; break;
                    case "Physical": dto.PhysicalRating = rating.Score; break;
                    case "GoalKeeping": dto.GoalkeepingRating = rating.Score; break;
                }
            }

            return dto;
        }

        public async Task<DrillTimelineDto> GetDrillTimelineAsync(
            int playerId, int page = 1, int pageSize = 20,
            DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            _logger.LogInformation("Fetching drill timeline for player {PlayerId}", playerId);

            var playerExists = await _unitOfWork.Repository<PlayerEntity>()
                .ExistsAsync(p => p.Id == playerId);

            if (!playerExists)
                throw new NotFoundException($"Player with id {playerId} was not found");

            var baseQuery = _unitOfWork.Repository<DrillResult>()
                .GetQueryableAsNoTracking()
                .Where(dr => dr.PlayerId == playerId && dr.Drill != null);

            if (dateFrom.HasValue)
                baseQuery = baseQuery.Where(dr => dr.Drill!.DrillSession!.SessionDate >= dateFrom.Value);

            if (dateTo.HasValue)
            {
                var dateToEnd = dateTo.Value.Date.AddDays(1).AddTicks(-1);
                baseQuery = baseQuery.Where(dr => dr.Drill!.DrillSession!.SessionDate <= dateToEnd);
            }

            var totalCount = await baseQuery.CountAsync();

            var events = await baseQuery
                .OrderByDescending(dr => dr.Drill!.DrillSession!.SessionDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(dr => new DrillTimelineEvent
                {
                    Date = dr.Drill!.DrillSession!.SessionDate,
                    Title = (dr.Drill!.DrillTemplate != null && dr.Drill.DrillTemplate.DrillCategory != null
                        ? dr.Drill.DrillTemplate.DrillCategory.Name : "Training Session"),
                    Description = dr.Drill!.DrillSession!.Notes,
                    SessionId = dr.Drill!.SessionId,
                    SessionType = dr.Drill!.DrillSession!.Type.ToString(),
                    DrillCategoryName = (dr.Drill!.DrillTemplate != null && dr.Drill.DrillTemplate.DrillCategory != null
                        ? dr.Drill.DrillTemplate.DrillCategory.Name : null),
                    DrillTemplateName = (dr.Drill!.DrillTemplate != null ? dr.Drill.DrillTemplate.Name : null),
                    FinalScore = dr.FinalScore,
                    DrillNotes = dr.CoachNotes,
                })
                .ToListAsync();

            return new DrillTimelineDto
            {
                Events = events,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }

        public async Task<MatchTimelineDto> GetMatchTimelineAsync(
            int playerId, int page = 1, int pageSize = 20,
            string? matchType = null, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            _logger.LogInformation("Fetching match timeline for player {PlayerId}", playerId);

            var playerExists = await _unitOfWork.Repository<PlayerEntity>()
                .ExistsAsync(p => p.Id == playerId);

            if (!playerExists)
                throw new NotFoundException($"Player with id {playerId} was not found");

            var baseQuery = _unitOfWork.Repository<MatchPlayerRating>()
                .GetQueryableAsNoTracking()
                .Where(mpr => mpr.PlayerId == playerId && mpr.Match != null);

            if (!string.IsNullOrWhiteSpace(matchType) && Enum.TryParse<Domain.Enums.MatchType>(matchType, true, out var parsedType))
                baseQuery = baseQuery.Where(mpr => mpr.Match!.Type == parsedType);

            if (dateFrom.HasValue)
                baseQuery = baseQuery.Where(mpr => mpr.Match!.MatchDate >= dateFrom.Value);

            if (dateTo.HasValue)
            {
                var dateToEnd = dateTo.Value.Date.AddDays(1).AddTicks(-1);
                baseQuery = baseQuery.Where(mpr => mpr.Match!.MatchDate <= dateToEnd);
            }

            var totalCount = await baseQuery.CountAsync();

            var events = await baseQuery
                .OrderByDescending(mpr => mpr.Match!.MatchDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(mpr => new MatchTimelineEvent
                {
                    Date = mpr.Match!.MatchDate,
                    Title = mpr.Match!.Type == Domain.Enums.MatchType.Session
                        ? "Home Side vs Away Side"
                        : (mpr.Match!.HomeTeam != null ? mpr.Match.HomeTeam.Name : "TBD")
                            + " vs "
                            + (mpr.Match.AwayTeam != null ? mpr.Match.AwayTeam.Name : "TBD"),
                    MatchId = mpr.Match!.Id,
                    MatchType = mpr.Match!.Type.ToString(),
                    HomeTeamName = mpr.Match!.Type == Domain.Enums.MatchType.Session
                        ? "Home Side"
                        : mpr.Match!.HomeTeam != null ? mpr.Match.HomeTeam.Name : null,
                    AwayTeamName = mpr.Match!.Type == Domain.Enums.MatchType.Session
                        ? "Away Side"
                        : mpr.Match!.AwayTeam != null ? mpr.Match.AwayTeam.Name : null,
                    HomeScore = mpr.Match!.HomeScore,
                    AwayScore = mpr.Match!.AwayScore,
                    HomePenaltyScore = mpr.Match!.HomePenaltyScore,
                    AwayPenaltyScore = mpr.Match!.AwayPenaltyScore,
                    Goals = mpr.Goals,
                    Assists = mpr.Assists,
                    MinutesPlayed = mpr.MinutesPlayed,
                    IsMOTM = mpr.IsMOTM,
                    Rating = mpr.CategoryRatings.Any()
                        ? mpr.CategoryRatings.Average(cr => cr.Rating)
                        : (decimal?)null,
                    CoachNote = mpr.CoachNote,
                })
                .ToListAsync();

            foreach (var evt in events)
            {
                if (!(evt.HomeScore == 0 && evt.AwayScore == 0))
                {
                    evt.Description = $"{evt.HomeScore} - {evt.AwayScore}";
                    if (evt.HomePenaltyScore.HasValue && evt.AwayPenaltyScore.HasValue)
                        evt.Description += $" ({evt.HomePenaltyScore} - {evt.AwayPenaltyScore} pen)";
                }
            }

            return new MatchTimelineDto
            {
                Events = events,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }

        public async Task<AchievementTimelineDto> GetAchievementTimelineAsync(
            int playerId, int page = 1, int pageSize = 20)
        {
            _logger.LogInformation("Fetching achievement timeline for player {PlayerId}", playerId);

            var playerExists = await _unitOfWork.Repository<PlayerEntity>()
                .ExistsAsync(p => p.Id == playerId);

            if (!playerExists)
                throw new NotFoundException($"Player with id {playerId} was not found");

            var baseQuery = _unitOfWork.Repository<PlayerAchievementEntity>()
                .GetQueryableAsNoTracking()
                .Where(pa => pa.PlayerId == playerId);

            var totalCount = await baseQuery.CountAsync();

            var events = await baseQuery
                .OrderByDescending(pa => pa.AwardedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(pa => new AchievementTimelineEvent
                {
                    Date = pa.AwardedAt,
                    Title = pa.AchievementType,
                    Description = pa.ReferenceType,
                    AchievementId = pa.Id,
                    AchievementType = pa.AchievementType,
                })
                .ToListAsync();

            return new AchievementTimelineDto
            {
                Events = events,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }

        public async Task<PlayerVsAcademyAverageDto> GetPlayerVsAcademyAverageAsync(
            int playerId, int academyId)
        {
            _logger.LogInformation(
                "Comparing player {PlayerId} vs academy {AcademyId} averages",
                playerId, academyId);

            var player = await _unitOfWork.Repository<PlayerEntity>()
                .GetQueryableAsNoTracking()
                .Include(p => p.PlayerPositions)
                .Include(p => p.PlayerTeams.Where(pt => pt.LeftAt == null))
                    .ThenInclude(pt => pt.Team)
                        .ThenInclude(t => t.AgeGroup)
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player is null)
                throw new NotFoundException($"Player with id {playerId} was not found");

            var academy = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                .GetQueryableAsNoTracking()
                .Where(a => a.Id == academyId)
                .Select(a => a.Name)
                .FirstOrDefaultAsync();

            if (academy is null)
                throw new NotFoundException($"Academy with id {academyId} was not found");

            var ageGroupIds = player.PlayerTeams
                .Where(pt => pt.LeftAt == null && pt.Team.AcademyId == academyId)
                .Select(pt => pt.Team.AgeGroupId)
                .Distinct()
                .ToList();

            var ageGroupName = player.PlayerTeams
                .Where(pt => pt.LeftAt == null && pt.Team.AcademyId == academyId)
                .Select(pt => pt.Team.AgeGroup?.Name)
                .FirstOrDefault();

            var playerAverages = await _unitOfWork.Repository<PlayerCategoryRating>()
                .GetQueryableAsNoTracking()
                .Where(cr => cr.PlayerCard!.PlayerId == playerId)
                .GroupBy(cr => cr.DrillCategoryId)
                .Select(g => new { DrillCategoryId = g.Key, Average = g.Average(cr => cr.Score) })
                .ToDictionaryAsync(g => g.DrillCategoryId, g => g.Average);

            Dictionary<int, decimal> academyAverages = new();

            if (ageGroupIds.Count > 0)
            {
                var teamIds = await _unitOfWork.Repository<Team>()
                    .GetQueryableAsNoTracking()
                    .Where(t => t.AcademyId == academyId && ageGroupIds.Contains(t.AgeGroupId))
                    .Select(t => t.Id)
                    .ToListAsync();

                var academyPlayerIds = await _unitOfWork.Repository<PlayerTeam>()
                    .GetQueryableAsNoTracking()
                    .Where(pt => pt.LeftAt == null && teamIds.Contains(pt.TeamId))
                    .Select(pt => pt.PlayerId)
                    .Distinct()
                    .ToListAsync();

                academyAverages = await _unitOfWork.Repository<PlayerCategoryRating>()
                    .GetQueryableAsNoTracking()
                    .Where(cr => academyPlayerIds.Contains(cr.PlayerCard!.PlayerId))
                    .GroupBy(cr => cr.DrillCategoryId)
                    .Select(g => new { DrillCategoryId = g.Key, Average = g.Average(cr => cr.Score) })
                    .ToDictionaryAsync(g => g.DrillCategoryId, g => g.Average);
            }

            var allCategoryIds = playerAverages.Keys.Union(academyAverages.Keys).Distinct().ToList();

            List<CategoryComparison> categories;

            if (allCategoryIds.Count == 0)
            {
                categories = [];
            }
            else
            {
                var categoryNames = await _unitOfWork.Repository<DrillCategory>()
                    .GetQueryableAsNoTracking()
                    .Where(dc => allCategoryIds.Contains(dc.Id))
                    .ToDictionaryAsync(dc => dc.Id, dc => dc.Name);

                categories = allCategoryIds.Select(catId => new CategoryComparison
                {
                    CategoryId = catId,
                    CategoryName = categoryNames.TryGetValue(catId, out var name) ? name : "Unknown",
                    PlayerAverage = playerAverages.TryGetValue(catId, out var pAvg) ? Math.Round(pAvg, 2) : 0,
                    AcademyAverage = academyAverages.TryGetValue(catId, out var aAvg) ? Math.Round(aAvg, 2) : 0,
                }).ToList();

                foreach (var c in categories)
                {
                    c.Difference = Math.Round(c.PlayerAverage - c.AcademyAverage, 2);
                }
            }

            var isGoalkeeper = player.PlayerPositions
                .Any(pp => pp.IsPrimary && string.Equals(pp.Position, "GK", StringComparison.OrdinalIgnoreCase));

            if (isGoalkeeper)
            {
                categories = categories
                    .Where(c => string.Equals(c.CategoryName, "Goalkeeping", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            else
            {
                categories = categories
                    .Where(c => !string.Equals(c.CategoryName, "Goalkeeping", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return new PlayerVsAcademyAverageDto
            {
                PlayerId = playerId,
                PlayerName = $"{player.FirstName} {player.LastName}",
                AcademyId = academyId,
                AcademyName = academy,
                AgeGroupName = ageGroupName,
                Categories = categories,
            };
        }

        public async Task<ScouterViewsCountDto> GetScouterViewsCountAsync(
            int playerId, int year, int month)
        {
            _logger.LogInformation(
                "Fetching scouter views for player {PlayerId} in {Year}-{Month}",
                playerId, year, month);

            var playerExists = await _unitOfWork.Repository<PlayerEntity>()
                .ExistsAsync(p => p.Id == playerId);

            if (!playerExists)
                throw new NotFoundException($"Player with id {playerId} was not found");

            var count = await _unitOfWork.Repository<ScouterView>()
                .CountAsync(sv =>
                    sv.PlayerId == playerId
                    && sv.ViewedAt.Year == year
                    && sv.ViewedAt.Month == month);

            return new ScouterViewsCountDto
            {
                PlayerId = playerId,
                Year = year,
                Month = month,
                ViewsCount = count,
            };
        }

        public async Task<TeamScheduledEventsResponseDto> GetTeamScheduledEventsAsync(
            int playerId, int page = 1, int pageSize = 20,
            string? eventType = null, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            _logger.LogInformation("Fetching team scheduled events for player {PlayerId}", playerId);

            var playerExists = await _unitOfWork.Repository<PlayerEntity>()
                .ExistsAsync(p => p.Id == playerId);

            if (!playerExists)
                throw new NotFoundException($"Player with id {playerId} was not found");

            var teamIds = await _unitOfWork.Repository<PlayerTeam>()
                .GetQueryableAsNoTracking()
                .Where(pt => pt.PlayerId == playerId && pt.LeftAt == null)
                .Select(pt => pt.TeamId)
                .ToListAsync();

            if (teamIds.Count == 0)
            {
                return new TeamScheduledEventsResponseDto
                {
                    Events = [],
                    TotalCount = 0,
                    Page = page,
                    PageSize = pageSize,
                };
            }

            var now = DateTime.UtcNow;

            var scheduledMatchesRaw = await _unitOfWork.Repository<MatchEntity>()
                .GetQueryableAsNoTracking()
                .Where(m => m.Status == MatchStatus.Scheduled
                    && m.MatchDate > now
                    && m.Type != Domain.Enums.MatchType.Session
                    && (teamIds.Contains(m.HomeTeamId) || teamIds.Contains(m.AwayTeamId)))
                .Select(m => new
                {
                    m.Id,
                    m.MatchDate,
                    m.Type,
                    m.HomeTeamId,
                    m.AwayTeamId,
                    HomeTeamName = m.HomeTeam.Name,
                    AwayTeamName = m.AwayTeam.Name,
                })
                .ToListAsync();

            var scheduledMatches = scheduledMatchesRaw
                .Select(m => new TeamScheduledEventDto
                {
                    EventType = "Match",
                    Date = m.MatchDate,
                    MatchId = m.Id,
                    MatchType = m.Type.ToString(),
                    HomeTeamName = m.HomeTeamName,
                    AwayTeamName = m.AwayTeamName,
                    TeamId = teamIds.Contains(m.HomeTeamId) ? m.HomeTeamId : m.AwayTeamId,
                    TeamName = teamIds.Contains(m.HomeTeamId) ? m.HomeTeamName : m.AwayTeamName,
                })
                .ToList();

            var scheduledDrills = await _unitOfWork.Repository<DrillSession>()
                .GetQueryableAsNoTracking()
                .Include(ds => ds.DrillSessionTeam)
                .Where(ds => ds.Status == SessionStatus.Scheduled
                    && ds.SessionDate > now
                    && teamIds.Contains(ds.TeamId))
                .Select(ds => new TeamScheduledEventDto
                {
                    EventType = "Drill",
                    Date = ds.SessionDate,
                    SessionId = ds.Id,
                    SessionType = ds.Type.ToString(),
                    TeamId = ds.TeamId,
                    TeamName = ds.DrillSessionTeam!.Name,
                    Notes = ds.Notes,
                })
                .ToListAsync();

            var combined = scheduledMatches
                .Concat(scheduledDrills)
                .OrderBy(e => e.Date)
                .ToList();

            if (!string.IsNullOrWhiteSpace(eventType))
            {
                combined = combined.Where(e =>
                    string.Equals(e.EventType, eventType, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (dateFrom.HasValue)
            {
                combined = combined.Where(e => e.Date >= dateFrom.Value).ToList();
            }

            if (dateTo.HasValue)
            {
                combined = combined.Where(e => e.Date <= dateTo.Value).ToList();
            }

            var totalCount = combined.Count;
            var pagedEvents = combined
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new TeamScheduledEventsResponseDto
            {
                Events = pagedEvents,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }
    }
}
