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
    }
}