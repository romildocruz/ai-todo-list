using AiTodo.Domain.Todos;
using FastEndpoints;

namespace AiTodo.Application.Features.Todos.Queries.GetTodos;

public sealed record GetTodosQuery(TodoStatus? Status, string? Search) : ICommand<IReadOnlyList<TodoResponse>>;
