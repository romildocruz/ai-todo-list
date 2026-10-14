using System.Diagnostics;
using FastEndpoints;

namespace AiTodo.Api.Processors;

/// <summary>Global pre-processor: marks the start of every endpoint execution and logs the incoming request.</summary>
public sealed class RequestLoggingPreProcessor : IGlobalPreProcessor
{
    internal const string StartedAtKey = "AiTodo.RequestStartedAt";

    public Task PreProcessAsync(IPreProcessorContext context, CancellationToken ct)
    {
        var http = context.HttpContext;
        http.Items[StartedAtKey] = Stopwatch.GetTimestamp();

        http.Resolve<ILogger<RequestLoggingPreProcessor>>()
            .LogInformation("Requisição recebida: {Method} {Path}", http.Request.Method, http.Request.Path);

        return Task.CompletedTask;
    }
}

/// <summary>Global post-processor: logs the outcome (status code and elapsed time) of every endpoint execution.</summary>
public sealed class RequestLoggingPostProcessor : IGlobalPostProcessor
{
    public Task PostProcessAsync(IPostProcessorContext context, CancellationToken ct)
    {
        var http = context.HttpContext;
        var elapsed = http.Items.TryGetValue(RequestLoggingPreProcessor.StartedAtKey, out var started) && started is long timestamp
            ? Stopwatch.GetElapsedTime(timestamp)
            : TimeSpan.Zero;

        http.Resolve<ILogger<RequestLoggingPostProcessor>>().LogInformation(
            "Requisição concluída: {Method} {Path} -> {StatusCode} em {ElapsedMs:0.0} ms",
            http.Request.Method, http.Request.Path, http.Response.StatusCode, elapsed.TotalMilliseconds);

        return Task.CompletedTask;
    }
}
