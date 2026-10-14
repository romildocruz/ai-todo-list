using AiTodo.Api.Common;
using AiTodo.Application.Features.Todos;
using AiTodo.Application.Features.Todos.Queries.GetTodoById;
using FastEndpoints;

namespace AiTodo.Api.Endpoints.Todos.GetTodoById;

public sealed class GetTodoByIdEndpoint : Endpoint<GetTodoByIdRequest, TodoResponse>
{
    public override void Configure()
    {
        Get("/todos/{id}");
        AllowAnonymous();
        Summary(s => s.Summary = "Consulta uma atividade pelo identificador.");
        Description(b => b
            .Produces<TodoResponse>(StatusCodes.Status200OK)
            .ProducesProblemFE(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status503ServiceUnavailable));
    }

    public override async Task HandleAsync(GetTodoByIdRequest req, CancellationToken ct)
    {
        var result = await new GetTodoByIdQuery(req.Id!).ExecuteAsync(ct);

        await Send.SendResultAsync(result, ct);
    }
}
