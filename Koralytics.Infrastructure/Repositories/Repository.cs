using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Application.Interfaces;
using Koralytics.Domain.Interfaces;
using Koralytics.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Koralytics.Infrastructure.Repositories
{
    public class Repository<T> : IRepository<T> where T : class,ISoftDelete
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }
        public async Task<T?> GetByIdAsync(int id)
        => await _dbSet.FindAsync(id);
        public async Task<T?> GetByIdAsNoTrackingAsync(int id)
            => await _dbSet.AsNoTracking().FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
        public IQueryable<T> GetQueryable()
        => _dbSet.AsQueryable();

        public IQueryable<T> GetQueryableAsNoTracking()
            => _dbSet.AsNoTracking().AsQueryable();
        public async Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> predicate)
            => await _dbSet.Where(predicate).ToListAsync();

        public async Task<IEnumerable<T>> FindAllAsNoTrackingAsync(Expression<Func<T, bool>> predicate)
            => await _dbSet.AsNoTracking().Where(predicate).ToListAsync();
        public async Task AddAsync(T entity)
        => await _dbSet.AddAsync(entity);

        public async Task AddRangeAsync(IEnumerable<T> entities)
            => await _dbSet.AddRangeAsync(entities);
        public void SoftDelete(T entity)
            => entity.IsDeleted = true;

        public void SoftDeleteRange(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
                entity.IsDeleted = true;
        }
        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        => await _dbSet.AnyAsync(predicate);

        public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
            => predicate == null
                ? await _dbSet.CountAsync()
                : await _dbSet.CountAsync(predicate);

        public async Task<T?> FindAsync(Expression<Func<T, bool>> predicate)
            => await _dbSet.FirstOrDefaultAsync(predicate);

        public async Task<T?> FindAsNoTrackingAsync(Expression<Func<T, bool>> predicate)
            => await _dbSet.AsNoTracking().FirstOrDefaultAsync(predicate);
    }
}
