using AiTodo.Application.Common;
using AiTodo.Domain.Todos;
using FastEndpoints;

namespace AiTodo.Application.Features.Todos.Commands.UpdateTodo;

public sealed class UpdateTodoHandler(ITodoRepository repository, TimeProvider timeProvider)
    : ICommandHandler<UpdateTodoCommand, Result<TodoResponse>>
{
    public async Task<Result<TodoResponse>> ExecuteAsync(UpdateTodoCommand command, CancellationToken ct)
    {
        if (!TodoId.TryParse(command.Id, out var id))
        {
            return Result<TodoResponse>.NotFound();
        }

        var todo = await repository.GetByIdAsync(id, ct);
        if (todo is null)
        {
            return Result<TodoResponse>.NotFound();
        }

        var changed = todo.Update(command.Title, command.Description, timeProvider.GetUtcNow().UtcDateTime);

        // Nothing to persist: UpdatedAt stays as it was.
        if (changed && !await repository.UpdateAsync(todo, ct))
        {
            // Deleted by someone else between the read and the write.
            return Result<TodoResponse>.NotFound();
        }

        return Result<TodoResponse>.Success(todo.ToResponse());
    }
}
