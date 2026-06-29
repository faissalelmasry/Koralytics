using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;

namespace Koralytics.Application.Interfaces
{
    public interface IUnitOfWork:IDisposable
    {
        Task<int> SaveChanges();

        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}
