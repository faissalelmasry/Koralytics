using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();

        Task<IEnumerable<T>> FindAsync(
            Expression<Func<T, bool>> predicate,
            string[]? includes = null);

        Task<T?> FindSingleAsync(
            Expression<Func<T, bool>> predicate,
            string[]? includes = null);

        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);

        void Delete(T entity);

        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);
    }
}
