namespace AiTodo.Domain.Todos;

/// <summary>Write-side persistence port for the <see cref="Todo"/> aggregate (one repository per aggregate root).</summary>
public interface ITodoRepository
{
    /// <summary>Generates the identity of a new todo (the persistence technology decides the format).</summary>
    TodoId NextId();

    Task AddAsync(Todo todo, CancellationToken cancellationToken);

    /// <returns>The todo, or <c>null</c> when it does not exist.</returns>
    Task<Todo?> GetByIdAsync(TodoId id, CancellationToken cancellationToken);

    /// <returns><c>true</c> when the todo existed and was replaced; <c>false</c> (nothing is created) otherwise.</returns>
    Task<bool> UpdateAsync(Todo todo, CancellationToken cancellationToken);

    /// <returns><c>true</c> when a todo was deleted.</returns>
    Task<bool> DeleteAsync(TodoId id, CancellationToken cancellationToken);
}
