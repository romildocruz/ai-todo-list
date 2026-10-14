using AiTodo.Application.Common;
using MongoDB.Driver;

namespace AiTodo.Infrastructure.Mongo;

/// <summary>
/// Wraps repository operations so that "database unreachable" driver failures surface as the
/// technology-neutral <see cref="DataStoreUnavailableException"/>. Anything else flows through unchanged.
/// </summary>
internal static class MongoCall
{
    public static async Task<T> RunAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex) when (IsUnavailable(ex))
        {
            throw new DataStoreUnavailableException(ex);
        }
    }

    public static async Task RunAsync(Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (Exception ex) when (IsUnavailable(ex))
        {
            throw new DataStoreUnavailableException(ex);
        }
    }

    public static bool IsUnavailable(Exception exception)
        => exception is TimeoutException
            or MongoConnectionException
            or MongoAuthenticationException
            or MongoWaitQueueFullException;
}
