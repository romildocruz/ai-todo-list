using AiTodo.Domain.Todos;
using MongoDB.Bson;

namespace AiTodo.Infrastructure.Mongo;

/// <summary>
/// Flat, read-only view of a stored todo used by queries: documents are read straight into DTOs
/// without rebuilding the aggregate.
/// </summary>
internal sealed class TodoReadModel
{
    public ObjectId Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TodoStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
