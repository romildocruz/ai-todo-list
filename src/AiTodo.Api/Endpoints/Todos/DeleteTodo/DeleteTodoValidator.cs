using AiTodo.Api.Validation;
using FastEndpoints;

namespace AiTodo.Api.Endpoints.Todos.DeleteTodo;

public sealed class DeleteTodoValidator : Validator<DeleteTodoRequest>
{
    public DeleteTodoValidator() => RuleFor(x => x.Id).ValidTodoId();
}
