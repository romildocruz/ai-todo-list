using AiTodo.Application.Common;
using FastEndpoints;

namespace AiTodo.Application.Features.Todos.Queries.GetTodoById;

public sealed record GetTodoByIdQuery(string Id) : ICommand<Result<TodoResponse>>;
