using AiTodo.Application.Common;
using FastEndpoints;

namespace AiTodo.Application.Features.Todos.Commands.DeleteTodo;

public sealed record DeleteTodoCommand(string Id) : ICommand<Result>;
