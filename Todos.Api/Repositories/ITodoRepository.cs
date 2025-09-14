using Todos.Api.Data;

namespace Todos.Api.Repositories;

public interface ITodoRepository
{
    Task<TodoItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(TodoItem item, CancellationToken cancellationToken = default);
    void Update(TodoItem item);
    void Remove(TodoItem item);
}
