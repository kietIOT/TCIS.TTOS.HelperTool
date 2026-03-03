namespace TCIS.TTOS.ToolHelper.DAL.UnitOfWork
{
    public interface IGenericUnitOfWork : IDisposable, IAsyncDisposable
    {
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        Task<int> CompleteAsync();
    }
}
