using Koralytics.Application.Interfaces;
using Koralytics.Domain.Entities.Parents; // Using the entity location from your ParentService
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Koralytics.Application.Services.Parent
{
    public interface IParentPlayerAccessService
    {
        Task<IReadOnlyList<int>> GetAuthorizedPlayerIdsAsync(string parentUserId);
    }
    public class ParentPlayerAccessService : IParentPlayerAccessService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ParentPlayerAccessService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<int>> GetAuthorizedPlayerIdsAsync(string parentUserId)
        {
            // The JWT Claim returns a string, but the DB ParentId is an int.
            if (!int.TryParse(parentUserId, out int parsedParentId))
            {
                return new List<int>();
            }

            var parentPlayerRepo = _unitOfWork.Repository<ParentPlayer>();

            // Fetch the linked children for this parent
            var authorizedPlayerIds = await parentPlayerRepo.GetQueryable()
                .Where(pp => pp.ParentId == parsedParentId && !pp.IsDeleted)
                .Select(pp => pp.PlayerId)
                .ToListAsync();

            return authorizedPlayerIds;
        }
    }
}