using FastEndpoints;

namespace AiTodo.Api.Endpoints.Health;

public sealed record HealthResponse(string Status);

public sealed class HealthEndpoint : EndpointWithoutRequest<HealthResponse>
{
    public override void Configure()
    {
        Get("/health");
        AllowAnonymous();
        Summary(s => s.Summary = "Verifica se a API está em execução (não consulta o banco).");
    }

    public override Task HandleAsync(CancellationToken ct)
        => Send.OkAsync(new HealthResponse("Healthy"), ct);
}
