using AiTodo.Api.Validation;
using FastEndpoints;
using FluentValidation;

namespace AiTodo.Api.Endpoints.Todos.ChangeTodoStatus;

public sealed class ChangeTodoStatusValidator : Validator<ChangeTodoStatusRequest>
{
    public ChangeTodoStatusValidator()
    {
        RuleFor(x => x.Id).ValidTodoId();
        RuleFor(x => x.Status)
            .Must(status => status is { } value && Enum.IsDefined(value))
            .WithMessage("O status deve ser ToDo, Doing ou Done.");
    }
}
