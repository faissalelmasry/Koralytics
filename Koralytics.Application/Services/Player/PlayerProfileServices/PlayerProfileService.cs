using AutoMapper;
using Koralytics.Application.DTOs.Player;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Services.Player.Helpers;
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
        private readonly ICardInvalidationList _cardInvalidationList;

        public PlayerProfileService(
            IUnitOfWork unitOfWork,
            ILogger<PlayerProfileService> logger,
            IMapper mapper,
            ICardInvalidationList cardInvalidationList)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
            _cardInvalidationList = cardInvalidationList;
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

            // Single DB round-trip: fetch all match-participation rows for this player,
            // then compute TotalMatches, per-type match counts, goals, assists and MOTMs
            // entirely in memory. Replaces 3 sequential queries that hit the same table.
            var rawMatchRows = await _unitOfWork.Repository<MatchPlayerRating>()
                .GetQueryableAsNoTracking()
                .Where(mpr => mpr.PlayerId == playerId && mpr.Match != null)
                .Select(mpr => new
                {
                    mpr.MatchId,
                    mpr.Match!.Type,
                    mpr.Goals,
                    mpr.Assists,
                    mpr.IsMOTM
                })
                .ToListAsync();

            profile.TotalMatches = rawMatchRows.Select(r => r.MatchId).Distinct().Count();

            var matchStatsByType = rawMatchRows
                .GroupBy(r => r.Type)
                .Select(g => new
                {
                    MatchType  = g.Key,
                    MatchCount = g.Select(r => r.MatchId).Distinct().Count(),
                    Goals      = g.Sum(r => r.Goals),
                    Assists    = g.Sum(r => r.Assists),
                    MOTMs      = g.Count(r => r.IsMOTM),
                })
                .ToList();

            var sessionStats    = new MatchTypeStats();
            var friendlyStats   = new MatchTypeStats();
            var tournamentStats = new MatchTypeStats();

            foreach (var stat in matchStatsByType)
            {
                profile.TotalGoals   += stat.Goals;
                profile.TotalAssists += stat.Assists;
                profile.TotalMOTMs   += stat.MOTMs;

                var typeStat = new MatchTypeStats
                {
                    Matches = stat.MatchCount,
                    Goals   = stat.Goals,
                    Assists = stat.Assists,
                    MOTMs   = stat.MOTMs,
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

            // Deferred existence check: only hit the Player table when the result is empty.
            // Saves a round-trip on every request that returns data (the common path).
            if (totalCount == 0)
            {
                var playerExists = await _unitOfWork.Repository<PlayerEntity>()
                    .ExistsAsync(p => p.Id == playerId);
                if (!playerExists)
                    throw new NotFoundException($"Player with id {playerId} was not found");
            }

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

            // Deferred existence check: only hit the Player table when the result is empty.
            // Saves a round-trip on every request that returns data (the common path).
            if (totalCount == 0)
            {
                var playerExists = await _unitOfWork.Repository<PlayerEntity>()
                    .ExistsAsync(p => p.Id == playerId);
                if (!playerExists)
                    throw new NotFoundException($"Player with id {playerId} was not found");
            }

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

            var baseQuery = _unitOfWork.Repository<PlayerAchievementEntity>()
                .GetQueryableAsNoTracking()
                .Where(pa => pa.PlayerId == playerId);

            var totalCount = await baseQuery.CountAsync();

            // Deferred existence check: only hit the Player table when the result is empty.
            // Saves a round-trip on every request that returns data (the common path).
            if (totalCount == 0)
            {
                var playerExists = await _unitOfWork.Repository<PlayerEntity>()
                    .ExistsAsync(p => p.Id == playerId);
                if (!playerExists)
                    throw new NotFoundException($"Player with id {playerId} was not found");
            }

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

            // Determine position type early — needed to select the right academy peer group
            var isGoalkeeper = player.PlayerPositions
                .Any(pp => pp.IsPrimary && string.Equals(pp.Position, "GK", StringComparison.OrdinalIgnoreCase));

            Dictionary<int, decimal> academyAverages = new();

            if (ageGroupIds.Count > 0)
            {
                // Collapsed from 3 sequential queries:
                //   1. Team  → get teamIds for this academy + age-group
                //   2. PlayerTeam → get all peer playerIds in those teams
                //   3. PlayerPosition → get GK playerIds among those peers
                // Into ONE query that navigates Team (age-group/academy filter) and
                // Player.PlayerPositions (primary position) via EF navigation properties.
                // The GK vs. field split is then done in memory.
                var peerPlayerData = await _unitOfWork.Repository<PlayerTeam>()
                    .GetQueryableAsNoTracking()
                    .Where(pt =>
                        pt.LeftAt == null &&
                        pt.Team.AcademyId == academyId &&
                        ageGroupIds.Contains(pt.Team.AgeGroupId))
                    .Select(pt => new
                    {
                        pt.PlayerId,
                        PrimaryPosition = pt.Player.PlayerPositions
                            .Where(pp => pp.IsPrimary)
                            .Select(pp => pp.Position)
                            .FirstOrDefault()
                    })
                    .Distinct()
                    .ToListAsync();

                // GK vs. field split happens in memory — primary positions are already fetched.
                var peerPlayerIds = isGoalkeeper
                    ? peerPlayerData
                        .Where(x => x.PrimaryPosition != null &&
                                    x.PrimaryPosition.Equals("gk", StringComparison.OrdinalIgnoreCase))
                        .Select(x => x.PlayerId)
                        .ToList()
                    : peerPlayerData
                        .Where(x => x.PrimaryPosition == null ||
                                    !x.PrimaryPosition.Equals("gk", StringComparison.OrdinalIgnoreCase))
                        .Select(x => x.PlayerId)
                        .ToList();

                academyAverages = await _unitOfWork.Repository<PlayerCategoryRating>()
                    .GetQueryableAsNoTracking()
                    .Where(cr => peerPlayerIds.Contains(cr.PlayerCard!.PlayerId))
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

            if (isGoalkeeper)
            {
                categories = categories
                    .Where(c => string.Equals(c.CategoryName, "Goalkeeping", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                // GK with no Goalkeeping data yet → inject a zero-placeholder so the bar always renders
                if (categories.Count == 0)
                {
                    categories.Add(new CategoryComparison
                    {
                        CategoryId = -1,
                        CategoryName = "Goalkeeping",
                        PlayerAverage = 0,
                        AcademyAverage = 0,
                        Difference = 0,
                    });
                }
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
                IsGoalkeeper = isGoalkeeper,
                Categories = categories,
            };

        }

        public async Task<PlayerVsAcademyAverageDto> GetPlayerAcademyComparisonByIdAsync(int playerId)
        {
            _logger.LogInformation(
                "Resolving academy for player {PlayerId} to run academy comparison", playerId);

            // Resolve the player's current academy from their active team memberships
            var currentAcademyId = await _unitOfWork.Repository<PlayerTeam>()
                .GetQueryableAsNoTracking()
                .Where(pt => pt.PlayerId == playerId && pt.LeftAt == null)
                .Select(pt => (int?)pt.Team.AcademyId)
                .FirstOrDefaultAsync();

            if (currentAcademyId is null)
                throw new NotFoundException($"No active academy found for player {playerId}.");

            return await GetPlayerVsAcademyAverageAsync(playerId, currentAcademyId.Value);
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

            // Determine which sources to query based on eventType.
            // Skipping an irrelevant source entirely avoids a wasted DB round-trip.
            var fetchMatches = string.IsNullOrWhiteSpace(eventType) ||
                               eventType.Equals("Match", StringComparison.OrdinalIgnoreCase);
            var fetchDrills  = string.IsNullOrWhiteSpace(eventType) ||
                               eventType.Equals("Drill", StringComparison.OrdinalIgnoreCase);

            // ── Matches ──────────────────────────────────────────────────────────
            // dateFrom / dateTo are pushed directly into the WHERE clause so the DB
            // only returns rows that survive all filters (no in-memory post-filtering).
            List<TeamScheduledEventDto> scheduledMatches = [];

            if (fetchMatches)
            {
                var scheduledMatchesRaw = await _unitOfWork.Repository<MatchEntity>()
                    .GetQueryableAsNoTracking()
                    .Where(m =>
                        m.Status == MatchStatus.Scheduled &&
                        m.MatchDate > now &&
                        m.Type != Domain.Enums.MatchType.Session &&
                        (teamIds.Contains(m.HomeTeamId) || teamIds.Contains(m.AwayTeamId)) &&
                        (!dateFrom.HasValue || m.MatchDate >= dateFrom.Value) &&
                        (!dateTo.HasValue   || m.MatchDate <= dateTo.Value))
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

                scheduledMatches = scheduledMatchesRaw
                    .Select(m => new TeamScheduledEventDto
                    {
                        EventType     = "Match",
                        Date          = m.MatchDate,
                        MatchId       = m.Id,
                        MatchType     = m.Type.ToString(),
                        HomeTeamName  = m.HomeTeamName,
                        AwayTeamName  = m.AwayTeamName,
                        TeamId        = teamIds.Contains(m.HomeTeamId) ? m.HomeTeamId : m.AwayTeamId,
                        TeamName      = teamIds.Contains(m.HomeTeamId) ? m.HomeTeamName : m.AwayTeamName,
                    })
                    .ToList();
            }

            // ── Drill sessions ────────────────────────────────────────────────────
            List<TeamScheduledEventDto> scheduledDrills = [];

            if (fetchDrills)
            {
                scheduledDrills = await _unitOfWork.Repository<DrillSession>()
                    .GetQueryableAsNoTracking()
                    .Where(ds =>
                        (ds.Status == SessionStatus.Scheduled || ds.Status == SessionStatus.Cancelled) &&
                        ds.SessionDate > now &&
                        teamIds.Contains(ds.TeamId) &&
                        (!dateFrom.HasValue || ds.SessionDate >= dateFrom.Value) &&
                        (!dateTo.HasValue   || ds.SessionDate <= dateTo.Value))
                    .Select(ds => new TeamScheduledEventDto
                    {
                        EventType   = "Drill",
                        Date        = ds.SessionDate,
                        SessionId   = ds.Id,
                        SessionType = ds.Type.ToString(),
                        TeamId      = ds.TeamId,
                        TeamName    = ds.DrillSessionTeam!.Name,
                        Notes       = ds.Notes,
                        Location    = ds.Location,
                        CoachName   = ds.DrillSessionCoach != null
                            ? ds.DrillSessionCoach.FirstName + " " + ds.DrillSessionCoach.LastName
                            : null,
                        IsCancelled = ds.Status == SessionStatus.Cancelled,
                    })
                    .ToListAsync();
            }

            // ── Combine, sort and paginate ────────────────────────────────────────
            // Cross-table pagination still happens in memory (unavoidable for a UNION
            // of two heterogeneous sources), but the combined set is now pre-filtered
            // by the DB so only surviving rows are transferred and sorted.
            var combined = scheduledMatches
                .Concat(scheduledDrills)
                .OrderBy(e => e.Date)
                .ToList();

            var totalCount = combined.Count;
            var pagedEvents = combined
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new TeamScheduledEventsResponseDto
            {
                Events     = pagedEvents,
                TotalCount = totalCount,
                Page       = page,
                PageSize   = pageSize,
            };
        }

        public async Task AddPlayerPositionAsync(int playerId, string position, bool isPrimary)
        {
            _logger.LogInformation("Adding position {Position} (isPrimary={IsPrimary}) for player {PlayerId}", position, isPrimary, playerId);

            var player = await _unitOfWork.Repository<PlayerEntity>()
                .GetQueryable()
                .Include(p => p.PlayerPositions)
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player is null)
                throw new NotFoundException($"Player with id {playerId} was not found");

            if (player.PlayerPositions.Any(pp =>
                string.Equals(pp.Position, position, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Player already has the position '{position}'");

            if (isPrimary)
            {
                foreach (var pp in player.PlayerPositions)
                {
                    pp.IsPrimary = false;
                }
            }

            player.PlayerPositions.Add(new PlayerPosition
            {
                PlayerId = playerId,
                Position = position,
                IsPrimary = isPrimary,
            });

            await _unitOfWork.SaveChangesAsync();

            if (isPrimary)
            {
                _cardInvalidationList.Invalidate(playerId);
            }
        }

        public async Task UpdatePrimaryPositionAsync(int playerId, string position)
        {
            _logger.LogInformation("Updating primary position to {Position} for player {PlayerId}", position, playerId);

            var player = await _unitOfWork.Repository<PlayerEntity>()
                .GetQueryable()
                .Include(p => p.PlayerPositions)
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player is null)
                throw new NotFoundException($"Player with id {playerId} was not found");

            var targetPosition = player.PlayerPositions.FirstOrDefault(pp =>
                string.Equals(pp.Position, position, StringComparison.OrdinalIgnoreCase));

            if (targetPosition is null)
                throw new InvalidOperationException($"Player does not have the position '{position}'");

            if (targetPosition.IsPrimary)
                return;

            foreach (var pp in player.PlayerPositions)
            {
                pp.IsPrimary = false;
            }

            targetPosition.IsPrimary = true;

            await _unitOfWork.SaveChangesAsync();

            _cardInvalidationList.Invalidate(playerId);
        }

        public async Task RemovePlayerPositionAsync(int playerId, string position)
        {
            _logger.LogInformation("Removing position {Position} from player {PlayerId}", position, playerId);

            var player = await _unitOfWork.Repository<PlayerEntity>()
                .GetQueryable()
                .Include(p => p.PlayerPositions)
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player is null)
                throw new NotFoundException($"Player with id {playerId} was not found");

            var targetPosition = player.PlayerPositions.FirstOrDefault(pp =>
                string.Equals(pp.Position, position, StringComparison.OrdinalIgnoreCase));

            if (targetPosition is null)
                throw new InvalidOperationException($"Player does not have the position '{position}'");

            if (targetPosition.IsPrimary)
                throw new InvalidOperationException("Cannot remove the primary position. Change your primary position first.");

            player.PlayerPositions.Remove(targetPosition);

            await _unitOfWork.SaveChangesAsync();
        }
    }
}
