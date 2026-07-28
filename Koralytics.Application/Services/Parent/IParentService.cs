using System.Collections.Generic;
using System.Threading.Tasks;
using Koralytics.Application.DTOs.Parent;

namespace Koralytics.Application.Services.Parent
{
    public interface IParentService
    {
        Task<IEnumerable<ParentChildDto>> GetMyChildrenAsync(int parentUserId);
    }
}