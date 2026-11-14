using NoAzul.Domain.Entities;

namespace NoAzul.Application.Abstractions.Repository.UnitOfWork
{
    public interface IUnitOfWork
    {
        IRepository<T> Repository<T>() where T : EntityBase;
        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
    }
}
