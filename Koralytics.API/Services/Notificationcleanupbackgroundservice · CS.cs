using Koralytics.Application.Interfaces.Notification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Koralytics.API.Services
{
    /// <summary>
    /// Periodically purges notifications older than 30 days for every user tracked
    /// in the Redis cache. Previously this only happened when an individual user
    /// happened to call DELETE /api/notification/expired, so users who never did
    /// would accumulate an unbounded Redis hash/sorted-set forever.
    ///
    /// </summary>
    public class NotificationCleanupBackgroundService : BackgroundService
    {
        private static readonly TimeSpan SweepInterval = TimeSpan.FromHours(6);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationCleanupBackgroundService> _logger;

        public NotificationCleanupBackgroundService(IServiceScopeFactory scopeFactory, ILogger<NotificationCleanupBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var realTimeBridge = scope.ServiceProvider.GetRequiredService<IRealTimeBridge>();

                    _logger.LogInformation("Starting scheduled notification cleanup sweep.");
                    await realTimeBridge.CleanupAllExpiredNotificationsAsync(stoppingToken);
                    _logger.LogInformation("Completed scheduled notification cleanup sweep.");
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    
                    _logger.LogError(ex, "Notification cleanup sweep failed.");
                }

                try
                {
                    await Task.Delay(SweepInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}