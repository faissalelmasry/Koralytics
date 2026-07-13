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
            var role = Context.User?.FindFirstValue(ClaimTypes.Role);

            if (!string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out var userId))
            {
                // 1. Always map to the universal User ID channel
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");

                if (!string.IsNullOrEmpty(role))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Role_{role}");
                }

                // 2. If the user is a Player, subscribe them to their Player and Team channels
                if (role == "Player")
                {
                    var player = await _unitOfWork.Repository<Player>()
                        .GetQueryableAsNoTracking()
                        .FirstOrDefaultAsync(p => p.Id == userId);

                    if (player != null)
                    {
                        // Add them to the specific Player channel expected by PlayerNotificationService
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

                // 3. If the user is a Parent, subscribe them to the Parent channels
                if (role == "Parent")
                {
                    
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Parent_{userId}");
                }

                if (role == "Scouter")
                {
                   
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Scouter_{userId}");
                }
            }

            await base.OnConnectedAsync();
        }
    }
}