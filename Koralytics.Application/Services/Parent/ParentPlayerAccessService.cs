using Koralytics.Application.Interfaces;
using Koralytics.Domain.Entities.Parents;
using Microsoft.EntityFrameworkCore;
using System;
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
            // 🟢 OPTIMIZATION: Returns Array.Empty to allocate zero memory on parse failure
            if (!int.TryParse(parentUserId, out int parsedParentId))
            {
                return Array.Empty<int>();
            }

            // 🟢 OPTIMIZATION: GetQueryableAsNoTracking for maximum read performance
            var authorizedPlayerIds = await _unitOfWork.Repository<ParentPlayer>()
                .GetQueryableAsNoTracking()
                .Where(pp => pp.ParentId == parsedParentId && !pp.IsDeleted)
                .Select(pp => pp.PlayerId)
                .ToListAsync();

            return authorizedPlayerIds;
        }
    }
}