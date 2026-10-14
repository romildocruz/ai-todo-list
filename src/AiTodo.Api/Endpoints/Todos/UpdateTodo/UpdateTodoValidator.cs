using AiTodo.Api.Validation;
using FastEndpoints;

namespace AiTodo.Api.Endpoints.Todos.UpdateTodo;

public sealed class UpdateTodoValidator : Validator<UpdateTodoRequest>
{
    public UpdateTodoValidator()
    {
        RuleFor(x => x.Id).ValidTodoId();
        RuleFor(x => x.Title).ValidTitle();
        RuleFor(x => x.Description).ValidDescription();
    }
}
