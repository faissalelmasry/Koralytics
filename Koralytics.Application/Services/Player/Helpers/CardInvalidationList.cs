using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Application.Interfaces;
using Koralytics.Domain.Entities.Player;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Koralytics.Application.Services.Player.Helpers
{
    public interface ICardInvalidationList
    {
        void Invalidate(int playerId);
        bool TryConsume(int playerId);
    }

    public class CardInvalidationList : ICardInvalidationList, IHostedService
    {
        private readonly ConcurrentDictionary<int, bool> _invalidated = new();
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CardInvalidationList> _logger;

        public CardInvalidationList(IServiceScopeFactory scopeFactory,ILogger<CardInvalidationList> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        // Called by services — instant, no DB
        public void Invalidate(int playerId)
            => _invalidated.TryAdd(playerId, true);

        public bool TryConsume(int playerId)
            => _invalidated.TryRemove(playerId, out _);

        // On startup — restore pending from DB
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider
                .GetRequiredService<IUnitOfWork>();

            var pendingIds = await unitOfWork.Repository<PlayerCard>()
                .GetQueryableAsNoTracking()
                .Where(pc => pc.NeedsRecalculation)
                .Select(pc => pc.PlayerId)
                .ToListAsync(cancellationToken);

            foreach (var playerId in pendingIds)
            {
                _invalidated.TryAdd(playerId, true);
                
            }
            

            _logger.LogInformation(
                $"CardInvalidationList restored {pendingIds.Count} pending players from DB");
        }

        // On shutdown — persist pending to DB in one batch
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (!_invalidated.Any()) return;

            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider
                .GetRequiredService<IUnitOfWork>();

            var pendingIds = _invalidated.Keys.ToList();

            var cards = await unitOfWork.Repository<PlayerCard>()
                .GetQueryable()
                .Where(pc => pendingIds.Contains(pc.PlayerId))
                .ToListAsync();

            foreach (var card in cards)
                card.NeedsRecalculation = true;

            await unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "CardInvalidationList persisted {Count} pending players to DB on shutdown",
                pendingIds.Count);
        }
    }
}
