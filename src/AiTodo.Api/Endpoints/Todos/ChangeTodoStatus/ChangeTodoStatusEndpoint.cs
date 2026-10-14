using AiTodo.Api.Common;
using AiTodo.Application.Features.Todos;
using AiTodo.Application.Features.Todos.Commands.ChangeTodoStatus;
using FastEndpoints;

namespace AiTodo.Api.Endpoints.Todos.ChangeTodoStatus;

public sealed class ChangeTodoStatusEndpoint : Endpoint<ChangeTodoStatusRequest, TodoResponse>
{
    public override void Configure()
    {
        Patch("/todos/{id}/status");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Altera o status de uma atividade.";
            s.Description = "Qualquer transição é permitida. updatedAt só muda se o status for diferente do atual.";
        });
        Description(b => b
            .Produces<TodoResponse>(StatusCodes.Status200OK)
            .ProducesProblemFE(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status503ServiceUnavailable));
    }

    public override async Task HandleAsync(ChangeTodoStatusRequest req, CancellationToken ct)
    {
        var result = await new ChangeTodoStatusCommand(req.Id!, req.Status!.Value).ExecuteAsync(ct);

        await Send.SendResultAsync(result, ct);
    }
}
