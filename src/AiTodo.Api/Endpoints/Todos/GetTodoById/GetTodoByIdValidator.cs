using AiTodo.Api.Validation;
using FastEndpoints;

namespace AiTodo.Api.Endpoints.Todos.GetTodoById;

public sealed class GetTodoByIdValidator : Validator<GetTodoByIdRequest>
{
    public GetTodoByIdValidator() => RuleFor(x => x.Id).ValidTodoId();
}
