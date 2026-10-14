using AiTodo.Application.Common;
using AiTodo.Domain.Todos;
using FastEndpoints;

namespace AiTodo.Application.Features.Todos.Commands.DeleteTodo;

public sealed class DeleteTodoHandler(ITodoRepository repository) : ICommandHandler<DeleteTodoCommand, Result>
{
    public async Task<Result> ExecuteAsync(DeleteTodoCommand command, CancellationToken ct)
        => TodoId.TryParse(command.Id, out var id) && await repository.DeleteAsync(id, ct)
            ? Result.Success()
            : Result.NotFound();
}
