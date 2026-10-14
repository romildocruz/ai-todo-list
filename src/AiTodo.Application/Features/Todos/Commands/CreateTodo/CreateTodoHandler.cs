using AiTodo.Domain.Todos;
using FastEndpoints;

namespace AiTodo.Application.Features.Todos.Commands.CreateTodo;

public sealed class CreateTodoHandler(ITodoRepository repository, TimeProvider timeProvider)
    : ICommandHandler<CreateTodoCommand, TodoResponse>
{
    public async Task<TodoResponse> ExecuteAsync(CreateTodoCommand command, CancellationToken ct)
    {
        var todo = Todo.Create(repository.NextId(), command.Title, command.Description, timeProvider.GetUtcNow().UtcDateTime);

        await repository.AddAsync(todo, ct);

        return todo.ToResponse();
    }
}
