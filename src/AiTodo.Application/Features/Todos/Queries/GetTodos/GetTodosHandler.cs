using FastEndpoints;

namespace AiTodo.Application.Features.Todos.Queries.GetTodos;

public sealed class GetTodosHandler(ITodoReadRepository readRepository)
    : ICommandHandler<GetTodosQuery, IReadOnlyList<TodoResponse>>
{
    public Task<IReadOnlyList<TodoResponse>> ExecuteAsync(GetTodosQuery query, CancellationToken ct)
        => readRepository.ListAsync(query.Status, query.Search, ct);
}
