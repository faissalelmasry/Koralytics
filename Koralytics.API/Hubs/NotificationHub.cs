using Koralytics.Application.Interfaces;
using Koralytics.Domain.Entities.Player;
using Koralytics.Domain.Entities.Parents;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Koralytics.API.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly IUnitOfWork _unitOfWork;

        public NotificationHub(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public override async Task OnConnectedAsync()
        {
            var userIdString = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            var roles = Context.User?.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new System.Collections.Generic.List<string>();

            if (!string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out var userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");

                foreach (var role in roles)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Role_{role.ToLowerInvariant()}");
                }

                if (roles.Any(r => string.Equals(r, "Player", StringComparison.OrdinalIgnoreCase)))
                {
                    var player = await _unitOfWork.Repository<Player>()
                        .GetQueryableAsNoTracking()
                        .FirstOrDefaultAsync(p => p.Id == userId);

                    if (player != null)
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, $"Player_{player.Id}");

                        var teamIds = await _unitOfWork.Repository<PlayerTeam>()
                            .GetQueryableAsNoTracking()
                            .Where(pt => pt.PlayerId == player.Id)
                            .Select(pt => pt.TeamId)
                            .ToListAsync();

                        foreach (var teamId in teamIds)
                        {
                            await Groups.AddToGroupAsync(Context.ConnectionId, $"Team_{teamId}");
                        }
                    }
                }

                if (roles.Any(r => string.Equals(r, "Parent", StringComparison.OrdinalIgnoreCase)))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Parent_{userId}");
                }

                if (roles.Any(r => string.Equals(r, "Scouter", StringComparison.OrdinalIgnoreCase)))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Scouter_{userId}");
                }
            }

            await base.OnConnectedAsync();
        }
    }
}