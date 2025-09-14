using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Todos.Api.Data;
using Todos.Api.Repositories;
using Todos.Api.Observability;

namespace Todos.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TodosController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public TodosController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoItem>>> GetAll(CancellationToken cancellationToken)
    {
        var todos = await _uow.Todos.GetAllAsync(cancellationToken);
        return Ok(todos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TodoItem>> GetById(int id, CancellationToken cancellationToken)
    {
        var todo = await _uow.Todos.GetByIdAsync(id, cancellationToken);
        if (todo is null) return NotFound();
        return Ok(todo);
    }

    public record CreateTodoRequest(string Title, string? Description);

    [HttpPost]
    public async Task<ActionResult<TodoItem>> Create([FromBody] CreateTodoRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return ValidationProblem("Title is required");

        var item = new TodoItem
        {
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
        };
        await _uow.Todos.AddAsync(item, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        TodoMetrics.TodosCreated.Add(1);

        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    public record UpdateTodoRequest(string Title, string? Description, bool IsCompleted);

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TodoItem>> Update(int id, [FromBody] UpdateTodoRequest request, CancellationToken cancellationToken)
    {
        var existing = await _uow.Todos.GetByIdAsync(id, cancellationToken);
        if (existing is null) return NotFound();

        if (string.IsNullOrWhiteSpace(request.Title))
            return ValidationProblem("Title is required");

        existing.Title = request.Title.Trim();
        existing.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        if (!existing.IsCompleted && request.IsCompleted)
        {
            existing.IsCompleted = true;
            existing.CompletedAtUtc = DateTime.UtcNow;
        }
        else if (existing.IsCompleted && !request.IsCompleted)
        {
            existing.IsCompleted = false;
            existing.CompletedAtUtc = null;
        }

        _uow.Todos.Update(existing);
        await _uow.SaveChangesAsync(cancellationToken);
        TodoMetrics.TodosUpdated.Add(1);

        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var existing = await _uow.Todos.GetByIdAsync(id, cancellationToken);
        if (existing is null) return NotFound();

        _uow.Todos.Remove(existing);
        await _uow.SaveChangesAsync(cancellationToken);
        TodoMetrics.TodosDeleted.Add(1);
        return NoContent();
    }
}
