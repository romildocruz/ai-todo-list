using AiTodo.Application.Common;
using FastEndpoints;

namespace AiTodo.Api.Common;

/// <summary>Builds RFC 9457 problem responses with the same shape used for every non-validation error.</summary>
public static class ProblemResults
{
    public const string TodoNotFoundDetail = "Atividade não encontrada.";

    public static IResult NotFound(string detail)
        => Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Recurso não encontrado.",
            detail: detail);

    /// <summary>
    /// Maps a handler <see cref="Result{T}"/> to the HTTP response: 200 with the value on success,
    /// otherwise the standard 404 problem. Keeps endpoints free of outcome-to-status plumbing.
    /// </summary>
    public static Task SendResultAsync<TRequest, TResponse>(
        this ResponseSender<TRequest, TResponse> send,
        Result<TResponse> result,
        CancellationToken ct,
        string notFoundDetail = TodoNotFoundDetail)
        where TRequest : notnull
        => result.IsSuccess
            ? send.OkAsync(result.Value!, ct)
            : send.ResultAsync(NotFound(notFoundDetail));

    /// <summary>Maps a value-less <see cref="Result"/>: 204 on success, otherwise the standard 404 problem.</summary>
    public static Task SendResultAsync<TRequest, TResponse>(
        this ResponseSender<TRequest, TResponse> send,
        Result result,
        CancellationToken ct,
        string notFoundDetail = TodoNotFoundDetail)
        where TRequest : notnull
        => result.IsSuccess
            ? send.NoContentAsync(ct)
            : send.ResultAsync(NotFound(notFoundDetail));
}
