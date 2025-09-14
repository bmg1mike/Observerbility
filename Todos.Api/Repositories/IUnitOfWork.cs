namespace Todos.Api.Repositories;

public interface IUnitOfWork
{
    ITodoRepository Todos { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
