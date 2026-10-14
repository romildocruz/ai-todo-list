using System.Reflection;
using AiTodo.Application.Features.Todos;
using AiTodo.Domain.Todos;

namespace AiTodo.Tests.Common;

/// <summary>
/// Test double for both persistence ports (<see cref="ITodoRepository"/> and <see cref="ITodoReadRepository"/>).
/// Aggregates cross the boundary as copies, like a database: changes made to an instance are not
/// persisted unless the handler calls Update.
/// </summary>
public sealed class InMemoryTodoRepository : ITodoRepository, ITodoReadRepository
{
    // Todo only holds immutable values, so a shallow copy is a faithful snapshot (test-only reflection
    // keeps any cloning API out of the Domain).
    private static readonly MethodInfo ShallowCopy =
        typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private readonly List<Todo> _store = [];

    /// <summary>When set, every port call throws it (simulates a database failure).</summary>
    public Exception? FailWith { get; set; }

    /// <summary>Makes <see cref="UpdateAsync"/> report that nothing matched (record deleted concurrently).</summary>
    public bool UpdateMatchesNothing { get; set; }

    public int UpdateCalls { get; private set; }

    public int Count => _store.Count;

    public void Clear() => _store.Clear();

    /// <summary>Direct, failure-free access to what is stored (for assertions).</summary>
    public Todo? Find(string id) => _store.FirstOrDefault(t => t.Id.Value == id) is { } todo ? Copy(todo) : null;

    // ---- ITodoRepository ---------------------------------------------------------------------

    public TodoId NextId() => TodoId.Parse(Guid.NewGuid().ToString("N")[..TodoId.Length]);

    public Task AddAsync(Todo todo, CancellationToken cancellationToken)
    {
        ThrowIfFailing();
        _store.Add(Copy(todo));
        return Task.CompletedTask;
    }

    Task<Todo?> ITodoRepository.GetByIdAsync(TodoId id, CancellationToken cancellationToken)
    {
        ThrowIfFailing();
        return Task.FromResult(Find(id.Value));
    }

    public Task<bool> UpdateAsync(Todo todo, CancellationToken cancellationToken)
    {
        ThrowIfFailing();
        UpdateCalls++;

        var index = _store.FindIndex(t => t.Id == todo.Id);
        if (index < 0 || UpdateMatchesNothing)
        {
            return Task.FromResult(false);
        }

        _store[index] = Copy(todo);
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(TodoId id, CancellationToken cancellationToken)
    {
        ThrowIfFailing();
        return Task.FromResult(_store.RemoveAll(t => t.Id == id) > 0);
    }

    // ---- ITodoReadRepository -----------------------------------------------------------------

    Task<TodoResponse?> ITodoReadRepository.GetByIdAsync(TodoId id, CancellationToken cancellationToken)
    {
        ThrowIfFailing();
        return Task.FromResult(Find(id.Value)?.ToResponse());
    }

    public Task<IReadOnlyList<TodoResponse>> ListAsync(TodoStatus? status, string? search, CancellationToken cancellationToken)
    {
        ThrowIfFailing();

        var term = search?.Trim();
        IReadOnlyList<TodoResponse> result = _store
            .Where(t => status is null || t.Status == status)
            .Where(t => string.IsNullOrEmpty(term) || t.Title.Contains(term, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id.Value, StringComparer.Ordinal)
            .Select(t => t.ToResponse())
            .ToList();

        return Task.FromResult(result);
    }

    private static Todo Copy(Todo todo) => (Todo)ShallowCopy.Invoke(todo, null)!;

    private void ThrowIfFailing()
    {
        if (FailWith is not null)
        {
            throw FailWith;
        }
    }
}
