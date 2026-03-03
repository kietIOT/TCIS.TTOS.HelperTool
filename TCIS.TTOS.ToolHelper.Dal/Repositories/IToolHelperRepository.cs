using System.Linq.Expressions;

namespace TCIS.TTOS.ToolHelper.DAL.Repositories
{
    public interface IToolHelperRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetByIdAsync(string id);
        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entity);
        Task UpdateAsync(T entity);
        Task UpdateFieldsByIdAsync(string id, Dictionary<string, object> updatedValues);
        Task DeleteAsync(T entity);
        Task DeleteByIdAsync(string id);
        Task DeleteRangeAsync(IEnumerable<T> entities);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy);
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> FindWithPaginationAsync(Expression<Func<T, bool>> predicate,
            int? skip, int? take, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy);
        Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate);

        Task<int> SaveChangesAsync();
    }
}
