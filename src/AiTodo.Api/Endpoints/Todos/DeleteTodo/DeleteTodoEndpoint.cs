using AiTodo.Api.Common;
using AiTodo.Application.Features.Todos.Commands.DeleteTodo;
using FastEndpoints;

namespace AiTodo.Api.Endpoints.Todos.DeleteTodo;

public sealed class DeleteTodoEndpoint : Endpoint<DeleteTodoRequest>
{
    public override void Configure()
    {
        Delete("/todos/{id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Exclui uma atividade.";
            s.Description = "A exclusão é definitiva.";
        });
        Description(b => b
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemFE(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status503ServiceUnavailable));
    }

    public override async Task HandleAsync(DeleteTodoRequest req, CancellationToken ct)
    {
        var result = await new DeleteTodoCommand(req.Id!).ExecuteAsync(ct);

        await Send.SendResultAsync(result, ct);
    }
}
