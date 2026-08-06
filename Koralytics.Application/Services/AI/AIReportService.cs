using Koralytics.Application.DTOs.AI;
using Koralytics.Application.DTOs.Notification;
using Koralytics.Application.Interfaces.AI;
using Koralytics.Application.Options;
using Koralytics.Domain.Entities.AI;
using Koralytics.Domain.Entities.Tournamet;
using Koralytics.Domain.Entities.Academy;
using System;
using System.Collections.Generic;
using Koralytics.Domain.Enums;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Notification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Koralytics.Application.Services.AI
{
    public class AIReportService : IAIReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAIProvider _aiProvider;
        private readonly IRealTimeBridge _realTimeBridge;
        private readonly ILogger<AIReportService> _logger;

        public AIReportService(
            IUnitOfWork unitOfWork,
            IAIProvider aiProvider,
            IRealTimeBridge realTimeBridge,
            ILogger<AIReportService> logger)
        {
            _unitOfWork = unitOfWork;
            _aiProvider = aiProvider;
            _realTimeBridge = realTimeBridge;
            _logger = logger;
        }

        public async Task<AIReportDto?> GetTournamentReportAsync(int tournamentId)
        {
            var report = await _unitOfWork.Repository<AIReport>()
                .FindAsNoTrackingAsync(r =>
                    r.ReportType == AIReportType.Tournament &&
                    r.ReferenceId == tournamentId);

            if (report is null)
            {
                var tournamentExists = await _unitOfWork.Repository<Tournament>()
                    .ExistsAsync(t => t.Id == tournamentId);

                if (!tournamentExists) return null;

                // No report row yet — create a pending one so the background worker picks it up
                await EnsurePendingReportRowAsync(tournamentId);

                report = await _unitOfWork.Repository<AIReport>()
                    .FindAsNoTrackingAsync(r =>
                        r.ReportType == AIReportType.Tournament &&
                        r.ReferenceId == tournamentId);
            }

            if (report is null) return null;

            var dto = MapToDto(report, tournamentId);
            if (report.Status == AIReportStatus.Completed)
            {
                await PopulateStatsAsync(dto, tournamentId);
            }
            return dto;
        }

        public async Task GenerateTournamentReportAsync(int tournamentId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Generating AI report for tournament {TournamentId}", tournamentId);

            var tournament = await _unitOfWork.Repository<Tournament>()
                .GetQueryable()
                .Include(t => t.TournamentTeams)
                    .ThenInclude(tt => tt.Team)
                .Include(t => t.TournamentRounds)
                    .ThenInclude(r => r.TournamentFixtures)
                        .ThenInclude(f => f.HomeTeam)
                            .ThenInclude(tt => tt.Team)
                .Include(t => t.TournamentRounds)
                    .ThenInclude(r => r.TournamentFixtures)
                        .ThenInclude(f => f.AwayTeam)
                            .ThenInclude(tt => tt.Team)
                .Include(t => t.TournamentRounds)
                    .ThenInclude(r => r.TournamentFixtures)
                        .ThenInclude(f => f.WinnerTeam)
                            .ThenInclude(tt => tt.Team)
                .FirstOrDefaultAsync(t => t.Id == tournamentId, cancellationToken);

            if (tournament is null)
            {
                _logger.LogWarning("Tournament {TournamentId} not found while generating AI report.", tournamentId);
                return;
            }

            var hallOfFame = await _unitOfWork.Repository<TournamentHallOfFame>()
                .GetQueryableAsNoTracking()
                .Include(h => h.Player)
                .Where(h => h.TournamentId == tournamentId)
                .ToListAsync(cancellationToken);

            // Fetch or create the AIReport row
            var reportRow = await _unitOfWork.Repository<AIReport>()
                .FindAsync(r =>
                    r.ReportType == AIReportType.Tournament &&
                    r.ReferenceId == tournamentId);

            if (reportRow is null)
            {
                reportRow = new AIReport
                {
                    ReportType = AIReportType.Tournament,
                    ReferenceId = tournamentId,
                    AcademyId = null,
                    ReportText = string.Empty,
                    Status = AIReportStatus.Pending
                };

                await _unitOfWork.Repository<AIReport>().AddAsync(reportRow);
                await _unitOfWork.SaveChangesAsync();
            }

            // Mark as Generating so no other worker picks it up concurrently
            reportRow.Status = AIReportStatus.Generating;
            await _unitOfWork.SaveChangesAsync();

            var prompt = await BuildTournamentPromptAsync(tournament, hallOfFame);
            string generatedReport;

            try
            {
                generatedReport = await _aiProvider.GenerateTournamentReportAsync(prompt, cancellationToken);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to generate tournament report for tournament {TournamentId}", tournamentId);

                // Mark as Failed so the background service does NOT retry endlessly
                reportRow.Status = AIReportStatus.Failed;
                await _unitOfWork.SaveChangesAsync();
                return;
            }

            reportRow.ReportText = generatedReport;
            reportRow.Status = AIReportStatus.Completed;
            reportRow.GeneratedAt = System.DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "AI report generated successfully for tournament {TournamentId}", tournamentId);

            var notification = new CachedNotification
            {
                Title = "Tournament AI Report Ready",
                Content = $"The AI post-tournament report for '{tournament.Name}' is now available.",
                Type = "TournamentReportReady",
                Payload = new { TournamentId = tournament.Id, TournamentName = tournament.Name }
            };

            await _realTimeBridge.SendToGroupAsync(
                "Role_academyadmin",
                "ReceiveAnnouncement",
                notification);
        }

        // ──────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ──────────────────────────────────────────────────────────────

        private async Task PopulateStatsAsync(AIReportDto dto, int tournamentId)
        {
            try
            {
                var academies = await _unitOfWork.Repository<Domain.Entities.Academy.Academy>()
                    .GetQueryableAsNoTracking()
                    .ToDictionaryAsync(a => a.Id, a => a.Name);

                string GetClubName(Team? team)
                {
                    if (team == null) return string.Empty;
                    return academies.TryGetValue(team.AcademyId, out var academyName) && !string.IsNullOrWhiteSpace(academyName)
                        ? academyName
                        : team.Name;
                }

                var tournament = await _unitOfWork.Repository<Tournament>()
                    .GetQueryableAsNoTracking()
                    .Include(t => t.TournamentTeams).ThenInclude(tt => tt.Team)
                    .Include(t => t.TournamentRounds).ThenInclude(tr => tr.TournamentFixtures).ThenInclude(tf => tf.WinnerTeam).ThenInclude(tt => tt.Team)
                    .FirstOrDefaultAsync(t => t.Id == tournamentId);

                if (tournament == null) return;

                Team? winnerTeam = null;
                int? winnerTournamentTeamId = null;

                if (tournament.Structure == TournamentStructure.Knockout || tournament.Structure == TournamentStructure.GroupAndKnockout)
                {
                    var finalRound = tournament.TournamentRounds
                        .OrderByDescending(r => r.RoundNumber)
                        .FirstOrDefault();

                    var finalFixture = finalRound?.TournamentFixtures
                        .OrderByDescending(f => f.LegNumber ?? 0)
                        .FirstOrDefault();

                    if (finalFixture?.WinnerTeam != null)
                    {
                        winnerTeam = finalFixture.WinnerTeam.Team;
                        winnerTournamentTeamId = finalFixture.WinnerTeamId;
                    }
                }
                else if (tournament.Structure == TournamentStructure.League)
                {
                    var topStanding = await _unitOfWork.Repository<TournamentStanding>()
                        .GetQueryableAsNoTracking()
                        .Include(s => s.TournamentTeam).ThenInclude(tt => tt.Team)
                        .Where(s => s.Group.TournamentId == tournamentId && s.Group.IsDummy == true)
                        .OrderByDescending(s => s.Points)
                        .ThenByDescending(s => s.GoalsFor - s.GoalsAgainst)
                        .ThenByDescending(s => s.GoalsFor)
                        .FirstOrDefaultAsync();

                    if (topStanding != null)
                    {
                        winnerTeam = topStanding.TournamentTeam.Team;
                        winnerTournamentTeamId = topStanding.TournamentTeamId;
                    }
                }

                if (winnerTeam != null)
                {
                    dto.WinnerTeamName = GetClubName(winnerTeam);
                    dto.WinnerTeamId = winnerTournamentTeamId;
                }

                var hallOfFame = await _unitOfWork.Repository<TournamentHallOfFame>()
                    .GetQueryableAsNoTracking()
                    .Include(h => h.Player)
                    .Where(h => h.TournamentId == tournamentId)
                    .ToListAsync();

                var bestPlayer = hallOfFame.FirstOrDefault(h => h.AwardType == "BestPlayer")?.Player;
                var topScorer = hallOfFame.FirstOrDefault(h => h.AwardType == "TopScorer")?.Player;
                var topAssister = hallOfFame.FirstOrDefault(h => h.AwardType == "MostAssists")?.Player;

                if (bestPlayer != null)
                {
                    dto.BestPlayerName = $"{bestPlayer.FirstName} {bestPlayer.LastName}";
                    dto.BestPlayerId = bestPlayer.Id;
                }

                if (topScorer != null)
                {
                    dto.TopScorerName = $"{topScorer.FirstName} {topScorer.LastName}";
                    dto.TopScorerId = topScorer.Id;
                }

                if (topAssister != null)
                {
                    dto.TopAssisterName = $"{topAssister.FirstName} {topAssister.LastName}";
                    dto.TopAssisterId = topAssister.Id;
                }

                var tournamentMatchIds = await _unitOfWork.Repository<TournamentFixture>()
                    .GetQueryableAsNoTracking()
                    .Where(f => f.MatchId != null &&
                                (f.Round != null ? f.Round.TournamentId == tournamentId : f.Group != null && f.Group.TournamentId == tournamentId))
                    .Select(f => f.MatchId!.Value)
                    .ToListAsync();

                if (tournamentMatchIds.Count > 0)
                {
                    var allRatings = await _unitOfWork.Repository<Koralytics.Domain.Entities.Match.MatchPlayerRating>()
                        .GetQueryableAsNoTracking()
                        .Where(r => tournamentMatchIds.Contains(r.MatchId))
                        .ToListAsync();

                    if (topScorer != null)
                    {
                        dto.TopScorerGoals = allRatings.Where(r => r.PlayerId == topScorer.Id).Sum(r => r.Goals);
                    }

                    if (topAssister != null)
                    {
                        dto.TopAssisterAssists = allRatings.Where(r => r.PlayerId == topAssister.Id).Sum(r => r.Assists);
                    }
                }

                var standings = await _unitOfWork.Repository<TournamentStanding>()
                    .GetQueryableAsNoTracking()
                    .Include(s => s.TournamentTeam).ThenInclude(tt => tt.Team)
                    .Where(s => s.Group.TournamentId == tournamentId)
                    .ToListAsync();

                if (standings.Count > 0)
                {
                    var mostScored = standings.OrderByDescending(s => s.GoalsFor).First();
                    dto.MostScoredClubName = GetClubName(mostScored.TournamentTeam.Team);
                    dto.MostScoredClubGoals = mostScored.GoalsFor;

                    var mostConceded = standings.OrderByDescending(s => s.GoalsAgainst).First();
                    dto.MostConcededClubName = GetClubName(mostConceded.TournamentTeam.Team);
                    dto.MostConcededClubGoals = mostConceded.GoalsAgainst;

                    var leastScored = standings.OrderBy(s => s.GoalsFor).First();
                    dto.LeastScoredClubName = GetClubName(leastScored.TournamentTeam.Team);
                    dto.LeastScoredClubGoals = leastScored.GoalsFor;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error populating stats in AIReportService");
            }
        }

        private async Task EnsurePendingReportRowAsync(int tournamentId)
        {
            var existing = await _unitOfWork.Repository<AIReport>()
                .FindAsync(r =>
                    r.ReportType == AIReportType.Tournament &&
                    r.ReferenceId == tournamentId);

            if (existing is null)
            {
                await _unitOfWork.Repository<AIReport>().AddAsync(new AIReport
                {
                    ReportType = AIReportType.Tournament,
                    ReferenceId = tournamentId,
                    AcademyId = null,
                    ReportText = string.Empty,
                    Status = AIReportStatus.Pending
                });
                await _unitOfWork.SaveChangesAsync();
            }
        }

        private static AIReportDto MapToDto(AIReport report, int tournamentId) =>
            new()
            {
                TournamentId = tournamentId,
                ReportText = report.ReportText,
                IsPending = report.Status == AIReportStatus.Pending || report.Status == AIReportStatus.Generating,
                Status = report.Status.ToString(),
                GeneratedAt = report.GeneratedAt
            };

        private async Task<string> BuildTournamentPromptAsync(Tournament tournament, List<TournamentHallOfFame> hallOfFame)
        {
            var teams = tournament.TournamentTeams
                .Select(tt => tt.Team.Name)
                .Distinct()
                .ToList();

            var teamsListStr = teams.Count > 0 ? string.Join("، ", teams) : "الفرق المشاركة بالبطولة";

            var finalRound = tournament.TournamentRounds
                .OrderByDescending(r => r.RoundNumber)
                .FirstOrDefault();

            var finalMatch = finalRound?.TournamentFixtures.FirstOrDefault(f => f.WinnerTeamId != null);
            var winnerName = finalMatch?.WinnerTeam?.Team?.Name ?? (teams.FirstOrDefault() ?? "الفريق الفائز بالبطولة");

            var bestPlayer = hallOfFame.FirstOrDefault(h => h.AwardType == "BestPlayer")?.Player;
            var topScorer = hallOfFame.FirstOrDefault(h => h.AwardType == "TopScorer")?.Player;
            var bestGk = hallOfFame.FirstOrDefault(h => h.AwardType == "BestGoalkeeper")?.Player;
            var mostAssists = hallOfFame.FirstOrDefault(h => h.AwardType == "MostAssists")?.Player;
            var mostMotm = hallOfFame.FirstOrDefault(h => h.AwardType == "MostMOTM")?.Player;

            var bestPlayerStr = bestPlayer != null ? $"{bestPlayer.FirstName} {bestPlayer.LastName}" : "غير محدد";
            var topScorerStr = topScorer != null ? $"{topScorer.FirstName} {topScorer.LastName}" : "غير محدد";
            var bestGkStr = bestGk != null ? $"{bestGk.FirstName} {bestGk.LastName}" : "غير محدد";
            var mostAssistsStr = mostAssists != null ? $"{mostAssists.FirstName} {mostAssists.LastName}" : "غير محدد";
            var mostMotmStr = mostMotm != null ? $"{mostMotm.FirstName} {mostMotm.LastName}" : "غير محدد";

            // Calculate extra stats for AI Context
            int topScorerGoals = 0;
            int topAssisterAssists = 0;
            string mostScoredClub = "غير محدد";
            int mostScoredGoals = 0;
            string mostConcededClub = "غير محدد";
            int mostConcededGoals = 0;
            string leastScoredClub = "غير محدد";
            int leastScoredGoals = 0;

            try
            {
                var tournamentMatchIds = await _unitOfWork.Repository<TournamentFixture>()
                    .GetQueryableAsNoTracking()
                    .Where(f => f.MatchId != null &&
                                (f.Round != null ? f.Round.TournamentId == tournament.Id : f.Group != null && f.Group.TournamentId == tournament.Id))
                    .Select(f => f.MatchId!.Value)
                    .ToListAsync();

                if (tournamentMatchIds.Count > 0)
                {
                    var allRatings = await _unitOfWork.Repository<Koralytics.Domain.Entities.Match.MatchPlayerRating>()
                        .GetQueryableAsNoTracking()
                        .Where(r => tournamentMatchIds.Contains(r.MatchId))
                        .ToListAsync();

                    if (topScorer != null)
                        topScorerGoals = allRatings.Where(r => r.PlayerId == topScorer.Id).Sum(r => r.Goals);

                    if (mostAssists != null)
                        topAssisterAssists = allRatings.Where(r => r.PlayerId == mostAssists.Id).Sum(r => r.Assists);
                }

                var standings = await _unitOfWork.Repository<TournamentStanding>()
                    .GetQueryableAsNoTracking()
                    .Include(s => s.TournamentTeam).ThenInclude(tt => tt.Team)
                    .Where(s => s.Group.TournamentId == tournament.Id)
                    .ToListAsync();

                if (standings.Count > 0)
                {
                    var ms = standings.OrderByDescending(s => s.GoalsFor).First();
                    mostScoredClub = ms.TournamentTeam.Team.Name;
                    mostScoredGoals = ms.GoalsFor;

                    var mc = standings.OrderByDescending(s => s.GoalsAgainst).First();
                    mostConcededClub = mc.TournamentTeam.Team.Name;
                    mostConcededGoals = mc.GoalsAgainst;

                    var ls = standings.OrderBy(s => s.GoalsFor).First();
                    leastScoredClub = ls.TournamentTeam.Team.Name;
                    leastScoredGoals = ls.GoalsFor;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building prompt stats context.");
            }

            var builder = new StringBuilder();
            builder.AppendLine("من فضلك اكتب تقريراً فنياً وإدارياً شاملاً وتفصيلياً كاملاً لمدير الأكاديمية عن البطولة الرياضية المكتملة.");
            builder.AppendLine("اكتب التقرير باللغة العربية الاحترافية والأسلوب الرياضي المصري الفاخر بلهجة محلل كرة قدم محترف.");
            builder.AppendLine("نريد أن يكون التقرير طويلاً ومفصلاً وموثقاً بالكامل، مقسماً إلى العناوين التالية بوضوح:");
            builder.AppendLine();
            builder.AppendLine("### 1. الخلاصة التنفيذية للبطولة (Executive Summary)");
            builder.AppendLine("(اكتب ملخصاً طويلاً ومفصلاً عن سير البطولة ونظامها ونجاحها وتتويج البطل).");
            builder.AppendLine();
            builder.AppendLine("### 2. لوحة الشرف وتحليل الأداء الفردي (Hall of Fame)");
            builder.AppendLine("(حلل أداء اللاعبين الفائزين بالجوائز الفردية بالتفصيل واشرح دور كل منهم التكتيكي والبدني وكيف ساعد فريقه).");
            builder.AppendLine();
            builder.AppendLine("### 3. التحليل التكتيكي والأداء الجماعي");
            builder.AppendLine("(حلل التكتيك وأساليب اللعب والضغط العالي والارتداد والتحولات الهجومية والدفاعية للفرق المشاركة بالتفصيل).");
            builder.AppendLine();
            builder.AppendLine("### 4. توصيات الذكاء الاصطناعي الاستراتيجية للأكاديمية");
            builder.AppendLine("(ضع خطة وتوصيات عملية تفصيلية للأجهزة الفنية لتطوير اللاعبين، علاج الأخطاء، البناء من الخلف، واستغلال الفرص).");
            builder.AppendLine();
            builder.AppendLine("بيانات البطولة المتاحة:");
            builder.AppendLine($"- اسم البطولة: {tournament.Name}");
            builder.AppendLine($"- بطل البطولة (أفضل نادي): {winnerName}");
            builder.AppendLine($"- الفرق المشاركة: {teamsListStr}");
            builder.AppendLine($"- أفضل لاعب في البطولة: {bestPlayerStr}");
            builder.AppendLine($"- هداف البطولة: {topScorerStr} (سجل {topScorerGoals} أهداف)");
            builder.AppendLine($"- أفضل حارس مرمى: {bestGkStr}");
            builder.AppendLine($"- الأكثر صناعة للأهداف (أفضل صانع ألعاب): {mostAssistsStr} (صنع {topAssisterAssists} أهداف)");
            builder.AppendLine($"- رجل المباراة الأكثر تكراراً: {mostMotmStr}");
            builder.AppendLine($"- النادي الأكثر تسجيلاً للأهداف: {mostScoredClub} (سجل {mostScoredGoals} أهداف)");
            builder.AppendLine($"- النادي الأكثر استقبالاً للأهداف: {mostConcededClub} (استقبل {mostConcededGoals} أهداف)");
            builder.AppendLine($"- النادي الأقل تسجيلاً للأهداف: {leastScoredClub} (سجل {leastScoredGoals} أهداف)");
            builder.AppendLine();
            builder.AppendLine("تعليمات هامة جداً: لا تختصر أبداً! اكتب تقريراً كاملاً ومفصلاً وبأسلوب احترافي رفيع ليكون منتج SaaS حقيقي ومقنع للعملاء.");

            return builder.ToString();
        }
    }
}
