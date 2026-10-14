namespace AiTodo.Api.Common;

public static class RequestBody
{
    /// <summary>
    /// True when the request carries no body at all. When the length is unknown (no Content-Length,
    /// e.g. GET/DELETE sent by generic HTTP wrappers) the body is probed without consuming it.
    /// </summary>
    public static async ValueTask<bool> IsEmptyAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (request.ContentLength is { } length)
        {
            return length == 0;
        }

        var result = await request.BodyReader.ReadAsync(cancellationToken);
        var isEmpty = result.IsCompleted && result.Buffer.IsEmpty;

        // Nothing consumed or examined: the next reader still sees every byte.
        request.BodyReader.AdvanceTo(result.Buffer.Start);

        return isEmpty;
    }
}
