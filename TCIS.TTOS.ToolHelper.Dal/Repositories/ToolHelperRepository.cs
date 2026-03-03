using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace TCIS.TTOS.ToolHelper.DAL.Repositories
{
    public class ToolHelperRepository<T>(ToolHelperDbContext dbContext) : IToolHelperRepository<T> where T : class
    {
        private readonly ToolHelperDbContext _dbContext = dbContext;
        private readonly DbSet<T> _dbSet = dbContext.Set<T>();

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<T?> GetByIdAsync(string id)
        {
            if (!Guid.TryParse(id, out Guid newId))
            {
                throw new ArgumentException("Id must be a valid GUID.", nameof(id));
            }
            return await _dbSet.FindAsync(newId);
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public async Task AddRangeAsync(IEnumerable<T> entity)
        {
            await _dbSet.AddRangeAsync(entity);
        }

        public async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            await Task.CompletedTask;
        }

        public async Task UpdateFieldsByIdAsync(string id, Dictionary<string, object> updatedValues)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity != null && updatedValues.Count > 0)
            {
                var entry = _dbContext.Entry(entity);

                foreach (var kvp in updatedValues)
                {
                    entry.Property(kvp.Key).CurrentValue = kvp.Value;
                    entry.Property(kvp.Key).IsModified = true;
                }
            }

            await Task.CompletedTask;
        }

        public async Task DeleteAsync(T entity)
        {
            _dbSet.Remove(entity);
            await Task.CompletedTask;
        }

        public async Task DeleteByIdAsync(string id)
        {
            if (!Guid.TryParse(id, out Guid newId))
            {
                throw new ArgumentException("Id must be a valid GUID.", nameof(id));
            }

            T? entity = await _dbSet.FindAsync(newId);

            if (entity != null)
            {
                _dbSet.Remove(entity);
            }
        }

        public async Task DeleteRangeAsync(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy)
        {
            var query = _dbSet.Where(predicate);

            if (orderBy != null)
                query = orderBy(query);

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<T>> FindWithPaginationAsync(Expression<Func<T, bool>> predicate,
            int? skip, int? take, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy)
        {
            IQueryable<T> query = _dbSet.Where(predicate);

            if (orderBy != null)
            {
                query = orderBy(query);
            }

            query = query.Skip(skip ?? 0).Take(take ?? int.MaxValue);

            return await query.ToListAsync();
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.CountAsync(predicate);
        }

        public async Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }
    }
}
