using AiTodo.Domain.Todos;

namespace AiTodo.Application.Features.Todos;

/// <summary>
/// Read-side port: queries read straight from the store and return DTOs, without rebuilding the aggregate.
/// </summary>
public interface ITodoReadRepository
{
    /// <returns>The todo, or <c>null</c> when it does not exist.</returns>
    Task<TodoResponse?> GetByIdAsync(TodoId id, CancellationToken cancellationToken);

    /// <summary>
    /// Lists todos newest first (ties broken by id, descending). <paramref name="search"/> is a literal,
    /// case-insensitive "title contains" match combined (AND) with the status filter.
    /// </summary>
    Task<IReadOnlyList<TodoResponse>> ListAsync(TodoStatus? status, string? search, CancellationToken cancellationToken);
}
