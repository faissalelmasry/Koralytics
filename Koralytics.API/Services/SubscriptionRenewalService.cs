using Koralytics.Application.Interfaces;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Koralytics.API.Services
{
    /// <summary>
    /// A long-running <see cref="BackgroundService"/> that fires once every 24 hours (configurable
    /// via <c>SubscriptionWorker:IntervalHours</c> in <c>appsettings.json</c>).
    ///
    /// Each sweep performs two passes over <c>PlayerSubscriptions</c>:
    ///
    /// <b>Pass 1 - Cycle Renewal</b>
    ///   Finds every <c>PAID</c> subscription whose <c>DueDate</c> is today or in the past.
    ///   For each one, it checks (idempotency guard) that no <c>Unpaid</c>/<c>Grace</c> record
    ///   already exists for the same player + academy with a <c>StartDate >= oldSub.DueDate</c>.
    ///   If not present, it auto-creates a new <c>Unpaid</c> subscription for the next cycle,
    ///   using the entity's <see cref="PlayerSubscription.SetBillingCycle"/> helper so that
    ///   <c>StartDate</c>, <c>DueDate</c>, and <c>GraceUntil</c> are all set consistently.
    ///
    /// <b>Pass 2 - Grace Escalation</b>
    ///   Finds every <c>Unpaid</c> subscription whose <c>DueDate</c> has already passed but
    ///   whose <c>GraceUntil</c> is still in the future, and promotes it to <c>Grace</c> so
    ///   the parent portal shows the visual grace warning immediately.
    /// </summary>
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

            // Allow overriding the interval from appsettings.json:
            // "SubscriptionWorker": { "IntervalHours": 24 }
            var hours = configuration.GetValue<double>("SubscriptionWorker:IntervalHours", 24d);
            _sweepInterval = TimeSpan.FromHours(hours);

            _logger.LogInformation(
                "[SubscriptionRenewalService] Configured sweep interval: {Interval}",
                _sweepInterval);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Main loop
        // ──────────────────────────────────────────────────────────────────────────

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[SubscriptionRenewalService] Background service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                await RunSweepAsync(stoppingToken);

                // Wait for the next interval, but wake early on cancellation.
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

        // ──────────────────────────────────────────────────────────────────────────
        // One full sweep
        // ──────────────────────────────────────────────────────────────────────────

        private async Task RunSweepAsync(CancellationToken ct)
        {
            _logger.LogInformation("[SubscriptionRenewalService] Sweep started at {UtcNow:u}", DateTime.UtcNow);

            try
            {
                // Each sweep gets its own DI scope so that the scoped IUnitOfWork
                // (and its underlying DbContext) is properly created and disposed.
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
                // Log but do NOT re-throw — a transient DB error must not kill the host process.
                _logger.LogError(ex, "[SubscriptionRenewalService] Sweep failed with an unhandled exception.");
            }
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Pass 1: Cycle Renewal
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// For every PAID subscription whose DueDate has passed, generates a new UNPAID
        /// subscription record for the immediately following cycle — unless one already exists
        /// (idempotency guard).
        ///
        /// Two critical guardrails are applied before the per-player loop:
        ///   1. <b>Latest-only grouping:</b> Players can accumulate multiple historical PAID rows.
        ///      We group by (PlayerId, AcademyId) and keep only the row with the highest DueDate
        ///      so that a single sweep never spawns more than one renewal per player.
        ///   2. <b>Status-agnostic idempotency:</b> The AnyAsync check no longer filters by Status.
        ///      If ANY subscription for this player+academy already has StartDate &gt;= oldSub.DueDate
        ///      (Paid, Unpaid, or Grace), renewal is skipped — this prevents duplicates when a
        ///      parent pays the new invoice early before the next sweep runs.
        /// </summary>
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
            //    Without this, historical PAID rows (e.g. months-old invoices) would each trigger
            //    their own AnyAsync check — and since newly-queued EF entities are invisible to
            //    GetQueryableAsNoTracking(), the check would pass for every row, creating duplicates
            //    within a single sweep.
            var latestExpiredSubs = expiredPaidSubs
                .GroupBy(s => new { s.PlayerId, s.AcademyId })
                .Select(g => g.OrderByDescending(s => s.DueDate).First())
                .ToList();

            _logger.LogInformation(
                "[SubscriptionRenewalService] Found {Count} unique expired player subscription(s) to process.",
                latestExpiredSubs.Count);

            int created = 0;
            int skipped = 0;

            foreach (var oldSub in latestExpiredSubs)
            {
                ct.ThrowIfCancellationRequested();

                // ── Idempotency guard ──────────────────────────────────────────
                // Check if ANY subscription (Paid, Unpaid, or Grace) already exists
                // for this cycle starting at or after the old DueDate.
                // Removing the Status filter prevents duplicates when a parent pays
                // the next invoice early (Status == Paid) before this sweep runs.
                bool alreadyRenewed = await subRepo
                    .GetQueryableAsNoTracking()
                    .AnyAsync(s =>
                        s.PlayerId == oldSub.PlayerId &&
                        s.AcademyId == oldSub.AcademyId &&
                        s.StartDate >= oldSub.DueDate,
                        ct);

                if (alreadyRenewed)
                {
                    _logger.LogDebug(
                        "[SubscriptionRenewalService] Skipping PlayerId={PlayerId}, AcademyId={AcademyId} — next cycle already exists.",
                        oldSub.PlayerId, oldSub.AcademyId);
                    skipped++;
                    continue;
                }

                // ── Create the next-cycle subscription ────────────────────────
                var newSub = new PlayerSubscription
                {
                    PlayerId  = oldSub.PlayerId,
                    AcademyId = oldSub.AcademyId,
                    Amount    = oldSub.Amount,
                    Status    = SubscriptionStatus.Unpaid
                };

                // SetBillingCycle sets StartDate, Duration, DueDate, and GraceUntil.
                // We pass graceDays: 7 to match the spec (entity default is 5).
                newSub.SetBillingCycle(oldSub.DueDate, oldSub.Duration, graceDays: 7);

                await subRepo.AddAsync(newSub);

                _logger.LogInformation(
                    "[SubscriptionRenewalService] Created renewal for PlayerId={PlayerId}, AcademyId={AcademyId}: " +
                    "StartDate={Start:d}, DueDate={Due:d}, GraceUntil={Grace:d}",
                    newSub.PlayerId, newSub.AcademyId,
                    newSub.StartDate, newSub.DueDate, newSub.GraceUntil);

                created++;
            }

            if (created > 0)
            {
                await uow.SaveChangesAsync();
            }

            _logger.LogInformation(
                "[SubscriptionRenewalService] Renewal pass complete — created: {Created}, skipped: {Skipped}.",
                created, skipped);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Pass 2: Grace Period Escalation
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Promotes UNPAID subscriptions whose DueDate has passed (but whose GraceUntil hasn't)
        /// to Grace status so the parent portal can display the appropriate visual warning.
        /// </summary>
        private async Task EscalateToGraceAsync(IUnitOfWork uow, DateTime now, CancellationToken ct)
        {
            var subRepo = uow.Repository<PlayerSubscription>();

            // Retrieve tracked entities so EF Core tracks the Status change.
            var overdueUnpaidSubs = await subRepo
                .GetQueryable()
                .Where(s =>
                    s.Status == SubscriptionStatus.Unpaid &&
                    s.DueDate < now &&
                    s.GraceUntil >= now)
                .ToListAsync(ct);

            if (!overdueUnpaidSubs.Any())
            {
                _logger.LogInformation("[SubscriptionRenewalService] No UNPAID subscriptions to escalate to Grace.");
                return;
            }

            foreach (var sub in overdueUnpaidSubs)
            {
                sub.Status = SubscriptionStatus.Grace;
            }

            await uow.SaveChangesAsync();

            _logger.LogInformation(
                "[SubscriptionRenewalService] Grace escalation pass complete — escalated {Count} subscription(s).",
                overdueUnpaidSubs.Count);
        }
    }
}
