using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Koralytics.Infrastructure.BackGroundJobs
{
    public class QueuedHostedService : BackgroundService
    {
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<QueuedHostedService> _logger;

        public QueuedHostedService(
            IBackgroundTaskQueue taskQueue,
            IServiceScopeFactory scopeFactory,
            ILogger<QueuedHostedService> logger)
        {
            _taskQueue = taskQueue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Queued Hosted Service is running.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var workItem = await _taskQueue.DequeueAsync(stoppingToken);

                    // IMPORTANT: create a NEW scope for every job.
                    // This gives us a fresh DbContext / UnitOfWork, not the disposed one from the HTTP request.
                    using var scope = _scopeFactory.CreateScope();
                    await workItem(scope.ServiceProvider, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Expected during application shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing background work item.");
                }
            }

            _logger.LogInformation("Queued Hosted Service is stopped.");
        }
    }
}
