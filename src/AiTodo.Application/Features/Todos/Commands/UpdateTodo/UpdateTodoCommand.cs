using AiTodo.Application.Common;
using FastEndpoints;

namespace AiTodo.Application.Features.Todos.Commands.UpdateTodo;

public sealed record UpdateTodoCommand(string Id, string? Title, string? Description) : ICommand<Result<TodoResponse>>;
