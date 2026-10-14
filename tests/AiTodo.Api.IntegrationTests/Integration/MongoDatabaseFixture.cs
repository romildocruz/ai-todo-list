using AiTodo.Application.Features.Todos;
using AiTodo.Domain.Todos;
using AiTodo.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AiTodo.Api.IntegrationTests.Integration;

/// <summary>
/// Creates a throwaway database for a test class, wires the real Infrastructure on top of it through DI,
/// and drops the database afterwards. When no test MongoDB is configured it does nothing (the tests are
/// skipped by <see cref="MongoFactAttribute"/>).
/// </summary>
public sealed class MongoDatabaseFixture : IAsyncLifetime
{
    private IMongoClient? _client;
    private ServiceProvider? _provider;

    public string DatabaseName { get; } = MongoTestSettings.NewDatabaseName();

    /// <summary>Raw access, used to assert on what is physically stored.</summary>
    public IMongoDatabase Database => _client?.GetDatabase(DatabaseName)
        ?? throw new InvalidOperationException(MongoTestSettings.SkipReason);

    public IMongoCollection<BsonDocument> RawTodos => Database.GetCollection<BsonDocument>("Todos");

    public ITodoRepository Repository => Provider.GetRequiredService<ITodoRepository>();

    public ITodoReadRepository ReadRepository => Provider.GetRequiredService<ITodoReadRepository>();

    private ServiceProvider Provider => _provider
        ?? throw new InvalidOperationException(MongoTestSettings.SkipReason);

    public async Task InitializeAsync()
    {
        if (MongoTestSettings.ConnectionString is not { } connectionString)
        {
            return;
        }

        MongoTestSettings.EnsureIsTestDatabase(DatabaseName);

        var settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
        var client = new MongoClient(settings);

        try
        {
            await client.GetDatabase("admin").RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
        }
        catch (Exception ex) when (ex is TimeoutException or MongoException)
        {
            // Fail loudly and explicitly: never fall back to another (e.g. development) database.
            throw new InvalidOperationException(
                $"A variável {MongoTestSettings.EnvironmentVariable} está definida, mas o MongoDB de testes não respondeu. " +
                "Inicie o serviço (Start-Service MongoDB) ou corrija a connection string.", ex);
        }

        _client = client;

        // The same wiring the API uses, pointed at the throwaway database (the hosted index
        // initializer is registered but never started because there is no host).
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Mongo:ConnectionString"] = connectionString,
                ["Mongo:DatabaseName"] = DatabaseName
            }).Build());
        services.AddInfrastructure();
        _provider = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        if (_provider is not null)
        {
            await _provider.DisposeAsync();
        }

        if (_client is not null)
        {
            MongoTestSettings.EnsureIsTestDatabase(DatabaseName);
            await _client.DropDatabaseAsync(DatabaseName);
        }
    }
}
