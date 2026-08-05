using Koralytics.Application.Interfaces.AI;
using Koralytics.Application.Interfaces;
using Koralytics.Domain.Entities.AI;
using Koralytics.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Koralytics.API.Services.AI
{
    /// <summary>
    /// Long-running hosted service that polls for AIReport rows with Status = Pending
    /// and generates the tournament wrap-up report in the background without blocking
    /// any HTTP request thread.
    /// 
    /// Lifecycle per report:
    ///   Pending → (picked up here) → AIReportService sets Generating → Completed | Failed
    /// 
    /// Only Pending rows are processed — Failed rows are NOT retried automatically,
    /// preventing an infinite retry loop when the AI provider is down.
    /// </summary>
    public class AIReportBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AIReportBackgroundService> _logger;
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

        public AIReportBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<AIReportBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AI report background service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingReportsAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AI report background service loop failed.");
                }

                try
                {
                    await Task.Delay(PollInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("AI report background service stopped.");
        }

        private async Task ProcessPendingReportsAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var aiReportService = scope.ServiceProvider.GetRequiredService<IAIReportService>();

            // Only pick up Pending rows — Generating, Completed, and Failed are excluded.
            // This prevents:
            //   • Double-processing (Generating)
            //   • Unnecessary re-runs (Completed)
            //   • Infinite retry loops (Failed)
            var pendingReports = await unitOfWork.Repository<AIReport>()
                .GetQueryable()
                .Where(r =>
                    r.ReportType == AIReportType.Tournament &&
                    r.Status == AIReportStatus.Pending)
                .ToListAsync(stoppingToken);

            if (pendingReports.Count == 0)
                return;

            _logger.LogInformation(
                "AI background service found {Count} pending tournament report(s) to process.",
                pendingReports.Count);

            foreach (var pending in pendingReports)
            {
                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    _logger.LogInformation(
                        "Processing AI report for tournament {TournamentId}", pending.ReferenceId);

                    await aiReportService.GenerateTournamentReportAsync(
                        pending.ReferenceId, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to process pending AI report for tournament {TournamentId}",
                        pending.ReferenceId);
                }
            }
        }
    }
}
