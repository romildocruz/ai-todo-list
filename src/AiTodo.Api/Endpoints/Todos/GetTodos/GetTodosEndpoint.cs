using AiTodo.Application.Features.Todos;
using AiTodo.Application.Features.Todos.Queries.GetTodos;
using AiTodo.Domain.Todos;
using FastEndpoints;

namespace AiTodo.Api.Endpoints.Todos.GetTodos;

public sealed class GetTodosEndpoint : Endpoint<GetTodosRequest, IReadOnlyList<TodoResponse>>
{
    public override void Configure()
    {
        Get("/todos");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Lista atividades, da mais recente para a mais antiga.";
            s.Description = "Filtros opcionais e combináveis: status (ToDo, Doing, Done) e search (trecho do título). Sem resultados retorna lista vazia.";
        });
        Description(b => b
            .Produces<IReadOnlyList<TodoResponse>>(StatusCodes.Status200OK)
            .ProducesProblemFE(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status503ServiceUnavailable));
    }

    public override async Task HandleAsync(GetTodosRequest req, CancellationToken ct)
    {
        TodoStatus? status = TodoRules.TryParseStatus(req.Status, out var parsed) ? parsed : null;

        var todos = await new GetTodosQuery(status, req.Search).ExecuteAsync(ct);

        await Send.OkAsync(todos, ct);
    }
}
