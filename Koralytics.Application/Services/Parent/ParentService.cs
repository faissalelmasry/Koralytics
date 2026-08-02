using Koralytics.Application.DTOs.Parent;
using Koralytics.Application.Interfaces;
using Koralytics.Application.Services.Parent;
using Koralytics.Domain.Entities.Parents;
using Microsoft.EntityFrameworkCore;

namespace Koralytics.Infrastructure.Services.Parents
{
    public class ParentService : IParentService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ParentService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<ParentChildDto>> GetMyChildrenAsync(int parentUserId)
        {
            var parentPlayerRepo = _unitOfWork.Repository<ParentPlayer>();

            return await parentPlayerRepo.GetQueryable()
                .Where(pp => pp.ParentId == parentUserId)
                .Select(pp => new ParentChildDto
                {
                    PlayerId = pp.Player.Id,
                    FullName = (pp.Player.FirstName + " " + pp.Player.LastName).Trim(),
                    PhotoUrl = pp.Player.ProfileImageUrl ?? pp.Player.ProfileImageUrl,
                    Position = pp.Player.PlayerPositions
                        .Where(pos => pos.IsPrimary)
                        .Select(pos => pos.Position.ToString())
                        .FirstOrDefault()
                        ?? pp.Player.PlayerPositions
                            .Select(pos => pos.Position.ToString())
                            .FirstOrDefault()
                        ?? "Squad Player",
                    TeamName = pp.Player.PlayerTeams
                        .Where(pt => pt.LeftAt == null)
                        .Select(pt => pt.Team.Name)
                        .FirstOrDefault()
                        ?? pp.Player.PlayerTeams
                            .Select(pt => pt.Team.Name)
                            .FirstOrDefault()
                        ?? "Unassigned Team"
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<ParentPlayerSearchResponseDto>> SearchAvailablePlayersAsync(string? name, int parentUserId)
        {
            var playersQuery = _unitOfWork.Repository<Domain.Entities.Player.Player>().GetQueryable();

            if (!string.IsNullOrWhiteSpace(name))
            {
                var lowerName = name.ToLower();
                playersQuery = playersQuery.Where(p => (p.FirstName + " " + p.LastName).ToLower().Contains(lowerName));
            }

            var parentPlayers = await _unitOfWork.Repository<ParentPlayer>()
                .FindAllAsync(pp => pp.ParentId == parentUserId);
            var linkedPlayerIds = new HashSet<int>(parentPlayers.Select(pp => pp.PlayerId));

            var pendingRequests = await _unitOfWork.Repository<ParentPlayerJoinRequest>()
                .FindAllAsync(r => r.ParentId == parentUserId && r.Status == Koralytics.Domain.Enums.JoinRequestStatus.Pending);
            var pendingPlayerIds = new HashSet<int>(pendingRequests.Select(r => r.PlayerId));

            var results = await playersQuery
                .Take(30)
                .Select(p => new
                {
                    p.Id,
                    FullName = (p.FirstName + " " + p.LastName).Trim(),
                    PhotoUrl = p.ProfileImageUrl,
                    Position = p.PlayerPositions
                        .Where(pos => pos.IsPrimary)
                        .Select(pos => pos.Position.ToString())
                        .FirstOrDefault()
                        ?? "Squad Player",
                    TeamName = p.PlayerTeams
                        .Where(pt => pt.LeftAt == null)
                        .Select(pt => pt.Team.Name)
                        .FirstOrDefault()
                        ?? "Unassigned Team"
                })
                .ToListAsync();

            return results.Select(r => new ParentPlayerSearchResponseDto
            {
                PlayerId = r.Id,
                FullName = r.FullName,
                PhotoUrl = r.PhotoUrl,
                Position = r.Position,
                TeamName = r.TeamName,
                IsAlreadyLinked = linkedPlayerIds.Contains(r.Id),
                HasPendingRequest = pendingPlayerIds.Contains(r.Id)
            });
        }

        public async Task SendChildJoinRequestAsync(int parentUserId, int playerId)
        {
            var player = await _unitOfWork.Repository<Domain.Entities.Player.Player>().GetByIdAsync(playerId);
            if (player is null)
                throw new Koralytics.Domain.Exceptions.NotFoundException($"Player with ID {playerId} not found.");

            var existingLink = await _unitOfWork.Repository<ParentPlayer>()
                .FindAsync(pp => pp.ParentId == parentUserId && pp.PlayerId == playerId);
            if (existingLink != null)
                throw new Koralytics.Domain.Exceptions.BadRequestException("Player is already linked to your account.");

            var existingPending = await _unitOfWork.Repository<ParentPlayerJoinRequest>()
                .FindAsync(r => r.ParentId == parentUserId && r.PlayerId == playerId && r.Status == Koralytics.Domain.Enums.JoinRequestStatus.Pending);
            if (existingPending != null)
                throw new Koralytics.Domain.Exceptions.BadRequestException("A join request is already pending for this player.");

            var request = new ParentPlayerJoinRequest
            {
                ParentId = parentUserId,
                PlayerId = playerId,
                Status = Koralytics.Domain.Enums.JoinRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                CreatedById = parentUserId
            };

            await _unitOfWork.Repository<ParentPlayerJoinRequest>().AddAsync(request);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<ParentPlayerJoinRequestResponseDto>> GetPendingRequestsForParentAsync(int parentUserId)
        {
            var requests = await _unitOfWork.Repository<ParentPlayerJoinRequest>().GetQueryable()
                .Include(r => r.Player)
                .Include(r => r.Parent)
                .Where(r => r.ParentId == parentUserId && r.Status == Koralytics.Domain.Enums.JoinRequestStatus.Pending)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            return requests.Select(r => new ParentPlayerJoinRequestResponseDto
            {
                Id = r.Id,
                ParentId = r.ParentId,
                PlayerId = r.PlayerId,
                PlayerName = (r.Player.FirstName + " " + r.Player.LastName).Trim(),
                PlayerPhotoUrl = r.Player.ProfileImageUrl,
                ParentName = (r.Parent.FirstName + " " + r.Parent.LastName).Trim(),
                ParentEmail = r.Parent.Email ?? string.Empty,
                Status = r.Status,
                RequestedAt = r.RequestedAt,
                RespondedAt = r.RespondedAt
            });
        }

        public async Task CancelChildJoinRequestAsync(int requestId, int parentUserId)
        {
            var request = await _unitOfWork.Repository<ParentPlayerJoinRequest>().FindAsync(r => r.Id == requestId);
            if (request is null)
                throw new Koralytics.Domain.Exceptions.NotFoundException($"Join request {requestId} not found.");

            if (request.ParentId != parentUserId)
                throw new UnauthorizedAccessException("You can only cancel your own join requests.");

            if (request.Status != Koralytics.Domain.Enums.JoinRequestStatus.Pending)
                throw new Koralytics.Domain.Exceptions.BadRequestException("This request has already been processed.");

            request.Status = Koralytics.Domain.Enums.JoinRequestStatus.Cancelled;
            request.UpdatedById = parentUserId;
            request.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<ParentPlayerJoinRequestResponseDto>> GetPendingRequestsForPlayerAsync(int playerUserId)
        {
            var requests = await _unitOfWork.Repository<ParentPlayerJoinRequest>().GetQueryable()
                .Include(r => r.Player)
                .Include(r => r.Parent)
                .Where(r => r.PlayerId == playerUserId && r.Status == Koralytics.Domain.Enums.JoinRequestStatus.Pending)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            return requests.Select(r => new ParentPlayerJoinRequestResponseDto
            {
                Id = r.Id,
                ParentId = r.ParentId,
                PlayerId = r.PlayerId,
                PlayerName = (r.Player.FirstName + " " + r.Player.LastName).Trim(),
                PlayerPhotoUrl = r.Player.ProfileImageUrl,
                ParentName = (r.Parent.FirstName + " " + r.Parent.LastName).Trim(),
                ParentEmail = r.Parent.Email ?? string.Empty,
                Status = r.Status,
                RequestedAt = r.RequestedAt,
                RespondedAt = r.RespondedAt
            });
        }

        public async Task RespondToChildJoinRequestAsync(int requestId, Koralytics.Domain.Enums.JoinRequestStatus status, int playerUserId)
        {
            var request = await _unitOfWork.Repository<ParentPlayerJoinRequest>().FindAsync(r => r.Id == requestId);
            if (request is null)
                throw new Koralytics.Domain.Exceptions.NotFoundException($"Join request {requestId} not found.");

            if (request.PlayerId != playerUserId)
                throw new UnauthorizedAccessException("You can only respond to your own join requests.");

            if (request.Status != Koralytics.Domain.Enums.JoinRequestStatus.Pending)
                throw new Koralytics.Domain.Exceptions.BadRequestException("This request has already been processed.");

            if (status != Koralytics.Domain.Enums.JoinRequestStatus.Accepted && status != Koralytics.Domain.Enums.JoinRequestStatus.Rejected)
                throw new Koralytics.Domain.Exceptions.BadRequestException("Invalid status for response.");

            request.Status = status;
            request.RespondedAt = DateTime.UtcNow;
            request.UpdatedById = playerUserId;
            request.UpdatedAt = DateTime.UtcNow;

            if (status == Koralytics.Domain.Enums.JoinRequestStatus.Accepted)
            {
                var existingLink = await _unitOfWork.Repository<ParentPlayer>()
                    .GetQueryable()
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(pp => pp.ParentId == request.ParentId && pp.PlayerId == playerUserId);

                if (existingLink == null)
                {
                    var parentPlayer = new ParentPlayer
                    {
                        ParentId = request.ParentId,
                        PlayerId = playerUserId,
                        IsDeleted = false
                    };
                    await _unitOfWork.Repository<ParentPlayer>().AddAsync(parentPlayer);
                }
                else if (existingLink.IsDeleted)
                {
                    existingLink.IsDeleted = false;
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UnlinkChildAsync(int parentUserId, int playerId)
        {
            var link = await _unitOfWork.Repository<ParentPlayer>()
                .FindAsync(pp => pp.ParentId == parentUserId && pp.PlayerId == playerId);

            if (link == null)
                throw new Koralytics.Domain.Exceptions.NotFoundException("Linked player not found.");

            _unitOfWork.Repository<ParentPlayer>().SoftDelete(link);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<PlayerParentDto>> GetMyParentsAsync(int playerUserId)
        {
            var parentPlayerRepo = _unitOfWork.Repository<ParentPlayer>();

            return await parentPlayerRepo.GetQueryable()
                .Where(pp => pp.PlayerId == playerUserId)
                .Select(pp => new PlayerParentDto
                {
                    ParentId = pp.Parent.Id,
                    FullName = (pp.Parent.FirstName + " " + pp.Parent.LastName).Trim(),
                    Email = pp.Parent.Email ?? string.Empty,
                    PhoneNumber = pp.Parent.PhoneNumber,
                    PhotoUrl = pp.Parent.ProfileImageUrl
                })
                .ToListAsync();
        }

        public async Task UnlinkParentAsync(int playerUserId, int parentId)
        {
            var link = await _unitOfWork.Repository<ParentPlayer>()
                .FindAsync(pp => pp.ParentId == parentId && pp.PlayerId == playerUserId);

            if (link == null)
                throw new Koralytics.Domain.Exceptions.NotFoundException("Linked parent not found.");

            _unitOfWork.Repository<ParentPlayer>().SoftDelete(link);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}