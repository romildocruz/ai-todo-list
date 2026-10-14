using AiTodo.Domain.Todos;

namespace AiTodo.Application.Features.Todos;

/// <summary>Todo representation returned to callers. Dates are UTC.</summary>
/// <param name="Id">Identifier as a 24-character hexadecimal string.</param>
public sealed record TodoResponse(
    string Id,
    string Title,
    string? Description,
    TodoStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public static class TodoMapping
{
    public static TodoResponse ToResponse(this Todo todo)
        => new(todo.Id.Value, todo.Title, todo.Description, todo.Status, todo.CreatedAt, todo.UpdatedAt);
}
