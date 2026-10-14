using FluentValidation.Results;

namespace AiTodo.Api.Common;

public static class ApiProblemDetails
{
    /// <summary>
    /// Builds the 400 response. Failures produced by our validators already carry pt-BR messages (and an
    /// error code). Failures without a code come from request binding/deserialization (malformed JSON,
    /// unknown enum value...): their original text is English and exposes internal type names, so it is
    /// replaced by a generic message.
    /// </summary>
    public static FastEndpoints.ProblemDetails Build(IEnumerable<ValidationFailure> failures, HttpContext context, int statusCode)
    {
        var sanitized = failures.Select(failure => string.IsNullOrEmpty(failure.ErrorCode)
            ? new ValidationFailure(failure.PropertyName, "O valor informado é inválido ou o corpo da requisição está malformado.")
            : failure).ToList();

        return new FastEndpoints.ProblemDetails(sanitized, context.Request.Path.Value ?? string.Empty, context.TraceIdentifier, statusCode);
    }
}
