using Todos.Api.Data;

namespace Todos.Api.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    public ITodoRepository Todos { get; }

    public UnitOfWork(AppDbContext db, ITodoRepository todoRepository)
    {
        _db = db;
        Todos = todoRepository;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);
}
