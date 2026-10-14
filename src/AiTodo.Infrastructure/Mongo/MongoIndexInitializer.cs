using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AiTodo.Infrastructure.Mongo;

/// <summary>
/// Ensures the indexes of the todos collection exist. Creation is idempotent (same name and keys
/// is a no-op). It runs in the background and retries while MongoDB is unreachable, so the API
/// starts normally and recovers on its own when the database comes back.
/// </summary>
public sealed class MongoIndexInitializer(
    IMongoDatabase database,
    IOptions<MongoOptions> options,
    ILogger<MongoIndexInitializer> logger) : BackgroundService
{
    public const string CreatedAtIndexName = "ix_createdAt_desc";
    public const string StatusCreatedAtIndexName = "ix_status_createdAt_desc";

    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EnsureIndexesAsync(stoppingToken);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex) when (ex is MongoException or TimeoutException)
            {
                // Log only the exception type: driver messages can include server endpoints.
                logger.LogWarning(
                    "MongoDB indisponível ({ErrorType}); índices não criados. Nova tentativa em {Delay}s.",
                    ex.GetType().Name, RetryDelay.TotalSeconds);
            }

            try
            {
                await Task.Delay(RetryDelay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    internal async Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        var collection = database.GetCollection<BsonDocument>(options.Value.CollectionName);

        var createdAtDesc = new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Descending("createdAt"),
            new CreateIndexOptions { Name = CreatedAtIndexName });

        var statusCreatedAtDesc = new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("status").Descending("createdAt"),
            new CreateIndexOptions { Name = StatusCreatedAtIndexName });

        var created = await collection.Indexes.CreateManyAsync(
            [createdAtDesc, statusCreatedAtDesc], cancellationToken);

        logger.LogInformation("Índices garantidos em {Database}.{Collection}: {Indexes}",
            database.DatabaseNamespace.DatabaseName, options.Value.CollectionName, string.Join(", ", created));
    }
}
