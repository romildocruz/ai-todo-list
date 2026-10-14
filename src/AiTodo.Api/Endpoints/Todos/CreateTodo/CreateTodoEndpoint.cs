using AiTodo.Api.Endpoints.Todos.GetTodoById;
using AiTodo.Application.Features.Todos;
using AiTodo.Application.Features.Todos.Commands.CreateTodo;
using FastEndpoints;

namespace AiTodo.Api.Endpoints.Todos.CreateTodo;

public sealed class CreateTodoEndpoint : Endpoint<CreateTodoRequest, TodoResponse>
{
    public override void Configure()
    {
        Post("/todos");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Cria uma atividade.";
            s.Description = "A atividade nasce com status ToDo; id e datas são definidos pelo servidor.";
        });
        Description(b => b
            .Produces<TodoResponse>(StatusCodes.Status201Created)
            .ProducesProblemFE(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status503ServiceUnavailable)
            .ClearDefaultProduces(StatusCodes.Status200OK));
    }

    public override async Task HandleAsync(CreateTodoRequest req, CancellationToken ct)
    {
        var todo = await new CreateTodoCommand(req.Title, req.Description).ExecuteAsync(ct);

        await Send.CreatedAtAsync<GetTodoByIdEndpoint>(new { id = todo.Id }, todo, cancellation: ct);
    }
}
