using Microsoft.EntityFrameworkCore;
using Todos.Api.Data;

namespace Todos.Api.Repositories;

public class TodoRepository : ITodoRepository
{
    private readonly AppDbContext _db;

    public TodoRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<TodoItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _db.TodoItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<List<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _db.TodoItems.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken);

    public async Task AddAsync(TodoItem item, CancellationToken cancellationToken = default)
        => await _db.TodoItems.AddAsync(item, cancellationToken);

    public void Update(TodoItem item)
        => _db.TodoItems.Update(item);

    public void Remove(TodoItem item)
        => _db.TodoItems.Remove(item);
}
