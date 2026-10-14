using AiTodo.Application.Common;
using AiTodo.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AiTodo.Api.Common;

/// <summary>
/// Converts unhandled exceptions into RFC 9457 problem responses without exposing internal details:
/// 400 for a violated domain rule, 503 when the data store is unreachable, 500 for anything else.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // The client gave up: nobody is listening for a response and it is not a server error.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return true;
        }

        ProblemDetails problem;

        switch (exception)
        {
            case DomainException domainException:
                // Request validators normally stop invalid input first; this is the safety net.
                logger.LogWarning("Regra de domínio violada em {Method} {Path} (campo: {Field})",
                    httpContext.Request.Method, httpContext.Request.Path, domainException.Field);

                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Requisição inválida.",
                    Detail = domainException.Message
                };
                break;

            case DataStoreUnavailableException:
                // Log only the exception type: driver messages can include server endpoints.
                logger.LogError("Armazenamento de dados indisponível ({ErrorType}) em {Method} {Path}",
                    exception.InnerException?.GetType().Name, httpContext.Request.Method, httpContext.Request.Path);

                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Title = "Serviço temporariamente indisponível.",
                    Detail = "Não foi possível acessar o banco de dados. Tente novamente em instantes."
                };
                break;

            default:
                logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
                    httpContext.Request.Method, httpContext.Request.Path);

                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Erro interno do servidor.",
                    Detail = "Ocorreu um erro inesperado. Tente novamente mais tarde."
                };
                break;
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(
            problem, options: null, contentType: "application/problem+json", cancellationToken);

        return true;
    }
}
