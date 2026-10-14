using AiTodo.Domain.Todos;
using FastEndpoints;
using FluentValidation;

namespace AiTodo.Api.Endpoints.Todos.GetTodos;

public sealed class GetTodosValidator : Validator<GetTodosRequest>
{
    public GetTodosValidator()
    {
        // A blank value means "no filter" (the front-end may send an empty parameter).
        RuleFor(x => x.Status)
            .Must(status => string.IsNullOrWhiteSpace(status) || TodoRules.TryParseStatus(status, out _))
            .WithMessage("O status deve ser ToDo, Doing ou Done.");
    }
}
