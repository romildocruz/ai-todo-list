using AiTodo.Api.Common;
using AiTodo.Application.Features.Todos;
using AiTodo.Application.Features.Todos.Commands.UpdateTodo;
using FastEndpoints;

namespace AiTodo.Api.Endpoints.Todos.UpdateTodo;

public sealed class UpdateTodoEndpoint : Endpoint<UpdateTodoRequest, TodoResponse>
{
    public override void Configure()
    {
        Put("/todos/{id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Atualiza título e descrição de uma atividade.";
            s.Description = "Substitui os dois campos. Status e datas de criação não são alterados; updatedAt só muda se houver alteração.";
        });
        Description(b => b
            .Produces<TodoResponse>(StatusCodes.Status200OK)
            .ProducesProblemFE(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status503ServiceUnavailable));
    }

    public override async Task HandleAsync(UpdateTodoRequest req, CancellationToken ct)
    {
        var result = await new UpdateTodoCommand(req.Id!, req.Title, req.Description).ExecuteAsync(ct);

        await Send.SendResultAsync(result, ct);
    }
}
