using AiTodo.Application.Common;
using AiTodo.Domain.Todos;
using FastEndpoints;

namespace AiTodo.Application.Features.Todos.Commands.ChangeTodoStatus;

public sealed class ChangeTodoStatusHandler(ITodoRepository repository, TimeProvider timeProvider)
    : ICommandHandler<ChangeTodoStatusCommand, Result<TodoResponse>>
{
    public async Task<Result<TodoResponse>> ExecuteAsync(ChangeTodoStatusCommand command, CancellationToken ct)
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

        var changed = todo.ChangeStatus(command.Status, timeProvider.GetUtcNow().UtcDateTime);

        // Same status: nothing to persist and UpdatedAt stays as it was.
        if (changed && !await repository.UpdateAsync(todo, ct))
        {
            return Result<TodoResponse>.NotFound();
        }

        return Result<TodoResponse>.Success(todo.ToResponse());
    }
}
