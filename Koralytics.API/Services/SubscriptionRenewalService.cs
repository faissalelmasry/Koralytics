using Koralytics.Application.Interfaces;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Koralytics.API.Services
{
    public class SubscriptionRenewalService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SubscriptionRenewalService> _logger;
        private readonly TimeSpan _sweepInterval;

        public SubscriptionRenewalService(
            IServiceScopeFactory scopeFactory,
            ILogger<SubscriptionRenewalService> logger,
            IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            var hours = configuration.GetValue<double>("SubscriptionWorker:IntervalHours", 24d);
            _sweepInterval = TimeSpan.FromHours(hours);

            _logger.LogInformation(
                "[SubscriptionRenewalService] Configured sweep interval: {Interval}",
                _sweepInterval);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[SubscriptionRenewalService] Background service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                await RunSweepAsync(stoppingToken);

                try
                {
                    await Task.Delay(_sweepInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("[SubscriptionRenewalService] Background service stopped.");
        }

        private async Task RunSweepAsync(CancellationToken ct)
        {
            _logger.LogInformation("[SubscriptionRenewalService] Sweep started at {UtcNow:u}", DateTime.UtcNow);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var now = DateTime.UtcNow;

                await RenewExpiredSubscriptionsAsync(unitOfWork, now, ct);
                await EscalateToGraceAsync(unitOfWork, now, ct);

                _logger.LogInformation("[SubscriptionRenewalService] Sweep completed at {UtcNow:u}", DateTime.UtcNow);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _logger.LogWarning("[SubscriptionRenewalService] Sweep cancelled (app shutting down).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SubscriptionRenewalService] Sweep failed with an unhandled exception.");
            }
        }

        private async Task RenewExpiredSubscriptionsAsync(IUnitOfWork uow, DateTime now, CancellationToken ct)
        {
            var subRepo = uow.Repository<PlayerSubscription>();

            // 1. Fetch all expired PAID subscriptions in one round-trip.
            var expiredPaidSubs = await subRepo
                .GetQueryableAsNoTracking()
                .Where(s => s.Status == SubscriptionStatus.Paid && s.DueDate <= now)
                .ToListAsync(ct);

            if (!expiredPaidSubs.Any())
            {
                _logger.LogInformation("[SubscriptionRenewalService] No expired PAID subscriptions found.");
                return;
            }

            // 2. Group by Player + Academy and select only the LATEST subscription per player.
            var latestExpiredSubs = expiredPaidSubs
                .GroupBy(s => new { s.PlayerId, s.AcademyId })
                .Select(g => g.OrderByDescending(s => s.DueDate).First())
                .ToList();

            _logger.LogInformation(
                "[SubscriptionRenewalService] Found {Count} unique expired player subscription(s) to process.",
                latestExpiredSubs.Count);

            // 🟢 OPTIMIZATION: Fetch all existing future subscriptions for these specific players in ONE query
            // to completely eliminate the N+1 database hit inside the foreach loop.
            var targetPlayerIds = latestExpiredSubs.Select(s => s.PlayerId).Distinct().ToList();

            var existingFutureSubs = await subRepo
                .GetQueryableAsNoTracking()
                .Where(s => targetPlayerIds.Contains(s.PlayerId))
                .Select(s => new { s.PlayerId, s.AcademyId, s.StartDate })
                .ToListAsync(ct);

            int created = 0;
            int skipped = 0;
            var newSubscriptionsToInsert = new List<PlayerSubscription>();

            foreach (var oldSub in latestExpiredSubs)
            {
                ct.ThrowIfCancellationRequested();

                // 🟢 OPTIMIZATION: Instantaneous O(1) memory check instead of hitting the DB
                bool alreadyRenewed = existingFutureSubs.Any(e =>
                    e.PlayerId == oldSub.PlayerId &&
                    e.AcademyId == oldSub.AcademyId &&
                    e.StartDate >= oldSub.DueDate);

                if (alreadyRenewed)
                {
                    skipped++;
                    continue;
                }

                var newSub = new PlayerSubscription
                {
                    PlayerId = oldSub.PlayerId,
                    AcademyId = oldSub.AcademyId,
                    Amount = oldSub.Amount,
                    Status = SubscriptionStatus.Unpaid
                };

                newSub.SetBillingCycle(oldSub.DueDate, oldSub.Duration, graceDays: 7);
                newSubscriptionsToInsert.Add(newSub);

                created++;
            }

            // 🟢 OPTIMIZATION: Bulk insert outside of the loop
            if (newSubscriptionsToInsert.Any())
            {
                await subRepo.AddRangeAsync(newSubscriptionsToInsert);
                await uow.SaveChangesAsync();
            }

            _logger.LogInformation(
                "[SubscriptionRenewalService] Renewal pass complete — created: {Created}, skipped: {Skipped}.",
                created, skipped);
        }

        private async Task EscalateToGraceAsync(IUnitOfWork uow, DateTime now, CancellationToken ct)
        {
            // 🟢 OPTIMIZATION: ExecuteUpdateAsync fires a single bulk UPDATE statement directly to SQL.
            // No fetching rows, no tracking, no looping. Instantaneous execution.
            var escalatedCount = await uow.Repository<PlayerSubscription>()
                .GetQueryable()
                .Where(s => s.Status == SubscriptionStatus.Unpaid && s.DueDate < now && s.GraceUntil >= now)
                .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.Status, SubscriptionStatus.Grace), ct);

            if (escalatedCount == 0)
            {
                _logger.LogInformation("[SubscriptionRenewalService] No UNPAID subscriptions to escalate to Grace.");
            }
            else
            {
                _logger.LogInformation(
                    "[SubscriptionRenewalService] Grace escalation pass complete — escalated {Count} subscription(s).",
                    escalatedCount);
            }
        }
    }
}