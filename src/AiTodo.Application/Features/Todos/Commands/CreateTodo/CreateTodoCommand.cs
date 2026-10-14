using FastEndpoints;

namespace AiTodo.Application.Features.Todos.Commands.CreateTodo;

public sealed record CreateTodoCommand(string? Title, string? Description) : ICommand<TodoResponse>;
