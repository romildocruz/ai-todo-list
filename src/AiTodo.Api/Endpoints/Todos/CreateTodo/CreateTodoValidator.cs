using AiTodo.Api.Validation;
using FastEndpoints;

namespace AiTodo.Api.Endpoints.Todos.CreateTodo;

public sealed class CreateTodoValidator : Validator<CreateTodoRequest>
{
    public CreateTodoValidator()
    {
        RuleFor(x => x.Title).ValidTitle();
        RuleFor(x => x.Description).ValidDescription();
    }
}
