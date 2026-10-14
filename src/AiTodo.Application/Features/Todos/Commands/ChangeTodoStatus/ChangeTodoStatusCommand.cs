using AiTodo.Application.Common;
using AiTodo.Domain.Todos;
using FastEndpoints;

namespace AiTodo.Application.Features.Todos.Commands.ChangeTodoStatus;

public sealed record ChangeTodoStatusCommand(string Id, TodoStatus Status) : ICommand<Result<TodoResponse>>;
