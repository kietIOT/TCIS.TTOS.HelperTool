using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace TCIS.TTOS.ToolHelper.DAL.UnitOfWork
{
    public abstract class GenericUnitOfWork<TContext>(IDbContextFactory<TContext> dbContextFactory) : IGenericUnitOfWork where TContext : DbContext
    {
        protected readonly TContext _dbContext = dbContextFactory.CreateDbContext();
        private IDbContextTransaction? _transaction;
        private bool _disposed;

        public async Task BeginTransactionAsync()
        {
            _transaction ??= await _dbContext.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task<int> CompleteAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }    

        #region Implement IDisposable & IAsyncDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _transaction?.Dispose();
                    _dbContext.Dispose();
                }

                _disposed = true;
            }
        }

        ~GenericUnitOfWork()
        {
            Dispose(false);
        }

        public async ValueTask DisposeAsync()
        {
            Dispose(true);
            GC.SuppressFinalize(this);

            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
            }

            await _dbContext.DisposeAsync();
        }
        #endregion
    }
}
