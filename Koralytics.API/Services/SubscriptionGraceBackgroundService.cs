using Koralytics.Application.Interfaces;
using Koralytics.Application.Interfaces.Notification;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Enums;
using StackExchange.Redis; 

public class SubscriptionGraceBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubscriptionGraceBackgroundService> _logger;
    private readonly TimeSpan _period = TimeSpan.FromDays(5); 

    public SubscriptionGraceBackgroundService(IServiceScopeFactory scopeFactory, ILogger<SubscriptionGraceBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_period);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                    break;

                await ProcessGracePeriodsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing subscription grace notifications.");
            }
        }
    }

    private async Task ProcessGracePeriodsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var notificationService = scope.ServiceProvider.GetRequiredService<IPlayerNotificationService>(); 
        var redisMultiplexer = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();

        var redisDb = redisMultiplexer.GetDatabase();
        var subscriptionsInGrace = await unitOfWork.Repository<PlayerSubscription>()
            .FindAllAsync(s => s.Status == SubscriptionStatus.Grace);

        foreach (var sub in subscriptionsInGrace)
        {
           
            string redisFlagKey = $"Player:{sub.PlayerId}:GracePeriodNotified";
            if (!await redisDb.KeyExistsAsync(redisFlagKey))
            {
              
                await notificationService.NotifySubscriptionGraceAsync(sub.PlayerId, sub.AcademyId, cancellationToken);

                await redisDb.StringSetAsync(redisFlagKey, "sent", TimeSpan.FromDays(3));
            }
        }
    }
}