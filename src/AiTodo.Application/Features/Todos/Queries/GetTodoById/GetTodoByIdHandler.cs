using AiTodo.Application.Common;
using AiTodo.Domain.Todos;
using FastEndpoints;

namespace AiTodo.Application.Features.Todos.Queries.GetTodoById;

public sealed class GetTodoByIdHandler(ITodoReadRepository readRepository)
    : ICommandHandler<GetTodoByIdQuery, Result<TodoResponse>>
{
    public async Task<Result<TodoResponse>> ExecuteAsync(GetTodoByIdQuery query, CancellationToken ct)
    {
        if (!TodoId.TryParse(query.Id, out var id))
        {
            return Result<TodoResponse>.NotFound();
        }

        var todo = await readRepository.GetByIdAsync(id, ct);

        return todo is null
            ? Result<TodoResponse>.NotFound()
            : Result<TodoResponse>.Success(todo);
    }
}
