using AiTodo.Application.Common;
using AiTodo.Application.Features.Todos;
using AiTodo.Domain.Todos;
using AiTodo.Infrastructure;
using AiTodo.Infrastructure.Mongo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AiTodo.Api.IntegrationTests.Integration;

/// <summary>
/// Configuration and failure-handling behavior of the MongoDB infrastructure. These tests need
/// no running MongoDB: they use missing or unreachable servers on purpose.
/// </summary>
public class MongoInfrastructureTests
{
    private static ServiceProvider BuildProvider(Dictionary<string, string?> settings)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
        services.AddInfrastructure();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Missing_connection_string_fails_with_a_message_naming_the_key_and_no_value()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>());

        var ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<MongoOptions>>().Value);

        Assert.Contains("Mongo:ConnectionString", ex.Message);
        Assert.Contains("Mongo__ConnectionString", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_connection_string_is_rejected(string connectionString)
    {
        using var provider = BuildProvider(new Dictionary<string, string?> { ["Mongo:ConnectionString"] = connectionString });

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<MongoOptions>>().Value);
    }

    [Fact]
    public void Defaults_point_to_AiTodoDb_Todos_with_short_timeouts()
    {
        using var provider = BuildProvider(new Dictionary<string, string?> { ["Mongo:ConnectionString"] = "mongodb://localhost:27017" });

        var options = provider.GetRequiredService<IOptions<MongoOptions>>().Value;

        Assert.Equal("AiTodoDb", options.DatabaseName);
        Assert.Equal("Todos", options.CollectionName);
        Assert.Equal(3, options.ServerSelectionTimeoutSeconds);
        Assert.Equal(3, options.ConnectTimeoutSeconds);
    }

    [Fact]
    public void Configured_values_override_the_defaults()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Mongo:ConnectionString"] = "mongodb://localhost:27017",
            ["Mongo:DatabaseName"] = "Outro",
            ["Mongo:CollectionName"] = "Tarefas",
            ["Mongo:ServerSelectionTimeoutSeconds"] = "7"
        });

        var options = provider.GetRequiredService<IOptions<MongoOptions>>().Value;

        Assert.Equal("Outro", options.DatabaseName);
        Assert.Equal("Tarefas", options.CollectionName);
        Assert.Equal(7, options.ServerSelectionTimeoutSeconds);
    }

    [Theory]
    [InlineData("Mongo:ServerSelectionTimeoutSeconds", "0")]
    [InlineData("Mongo:ServerSelectionTimeoutSeconds", "121")]
    [InlineData("Mongo:ConnectTimeoutSeconds", "0")]
    [InlineData("Mongo:DatabaseName", "")]
    [InlineData("Mongo:CollectionName", "")]
    public void Invalid_settings_are_rejected_on_validation(string key, string value)
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Mongo:ConnectionString"] = "mongodb://localhost:27017",
            [key] = value
        });

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<MongoOptions>>().Value);
    }

    [Fact]
    public void The_client_applies_the_configured_timeouts_and_is_a_singleton()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Mongo:ConnectionString"] = "mongodb://127.0.0.1:1",
            ["Mongo:ServerSelectionTimeoutSeconds"] = "2",
            ["Mongo:ConnectTimeoutSeconds"] = "4"
        });

        var client = provider.GetRequiredService<IMongoClient>();

        Assert.Same(client, provider.GetRequiredService<IMongoClient>());
        Assert.Equal(TimeSpan.FromSeconds(2), client.Settings.ServerSelectionTimeout);
        Assert.Equal(TimeSpan.FromSeconds(4), client.Settings.ConnectTimeout);
    }

    [Fact]
    public async Task Every_repository_operation_translates_an_unreachable_server_into_DataStoreUnavailableException()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Mongo:ConnectionString"] = "mongodb://127.0.0.1:1/?secret=do-not-leak",
            ["Mongo:ServerSelectionTimeoutSeconds"] = "1"
        });
        var repository = provider.GetRequiredService<ITodoRepository>();
        var readRepository = provider.GetRequiredService<ITodoReadRepository>();
        var id = repository.NextId();
        var todo = Todo.Create(id, "t", null, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var failures = await Task.WhenAll(
            Assert.ThrowsAsync<DataStoreUnavailableException>(() => repository.AddAsync(todo, default)),
            Assert.ThrowsAsync<DataStoreUnavailableException>(() => repository.GetByIdAsync(id, default)),
            Assert.ThrowsAsync<DataStoreUnavailableException>(() => repository.UpdateAsync(todo, default)),
            Assert.ThrowsAsync<DataStoreUnavailableException>(() => repository.DeleteAsync(id, default)),
            Assert.ThrowsAsync<DataStoreUnavailableException>(() => readRepository.GetByIdAsync(id, default)),
            Assert.ThrowsAsync<DataStoreUnavailableException>(() => readRepository.ListAsync(null, null, default)));

        Assert.All(failures, ex =>
        {
            Assert.IsType<TimeoutException>(ex.InnerException);
            Assert.DoesNotContain("do-not-leak", ex.Message);
        });
    }

    [Fact]
    public async Task The_index_initializer_does_not_crash_or_block_startup_when_mongodb_is_unreachable()
    {
        var settings = MongoClientSettings.FromConnectionString("mongodb://127.0.0.1:1");
        settings.ServerSelectionTimeout = TimeSpan.FromMilliseconds(300);
        var database = new MongoClient(settings).GetDatabase("AiTodoDb_Tests_Unreachable");
        var options = Options.Create(new MongoOptions { ConnectionString = "mongodb://127.0.0.1:1" });
        var initializer = new MongoIndexInitializer(database, options, NullLogger<MongoIndexInitializer>.Instance);

        var started = initializer.StartAsync(default);
        await started.WaitAsync(TimeSpan.FromSeconds(2)); // StartAsync returns immediately: the API is not held back
        await Task.Delay(1000);                           // enough for at least one failed attempt

        Assert.False(initializer.ExecuteTask!.IsFaulted, "a database outage must not fault the background initializer");
        Assert.False(initializer.ExecuteTask.IsCompleted, "it must keep retrying until MongoDB is back");

        var stopping = initializer.StopAsync(default);
        await stopping.WaitAsync(TimeSpan.FromSeconds(2)); // and shutting down is not blocked by the retry delay
        Assert.True(initializer.ExecuteTask.IsCompletedSuccessfully);
    }
}
