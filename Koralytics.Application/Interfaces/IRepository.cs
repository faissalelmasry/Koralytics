using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Interfaces;

namespace Koralytics.Application.Interfaces
{
    public interface IRepository<T> where T : class, ISoftDelete
    {
        Task<T?> GetByIdAsync(int id);
        Task<T?> GetByIdAsNoTrackingAsync(int id);
        Task<T?> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T?> FindAsNoTrackingAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> FindAllAsNoTrackingAsync(Expression<Func<T, bool>> predicate);
        IQueryable<T> GetQueryable();
        IQueryable<T> GetQueryableAsNoTracking();
        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        void SoftDelete(T entity);
        void SoftDeleteRange(IEnumerable<T> entities);
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
    }
}
