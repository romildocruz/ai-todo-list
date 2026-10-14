using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace AiTodo.Api.IntegrationTests.Integration;

/// <summary>
/// The complete API (real handlers, real repository, real index initializer) running against the test MongoDB,
/// in a throwaway database that is dropped on dispose. Nothing starts unless a test asks for a client.
/// </summary>
public sealed class MongoApiFactory : WebApplicationFactory<Program>
{
    private bool _started;

    public string DatabaseName { get; } = MongoTestSettings.NewDatabaseName();

    public IMongoDatabase Database
    {
        get
        {
            MongoTestSettings.EnsureIsTestDatabase(DatabaseName);
            return new MongoClient(MongoTestSettings.ConnectionString).GetDatabase(DatabaseName);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connectionString = MongoTestSettings.ConnectionString
            ?? throw new InvalidOperationException(MongoTestSettings.SkipReason);
        MongoTestSettings.EnsureIsTestDatabase(DatabaseName);
        _started = true;

        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Mongo:ConnectionString"] = connectionString,
                ["Mongo:DatabaseName"] = DatabaseName
            }));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && _started)
        {
            MongoTestSettings.EnsureIsTestDatabase(DatabaseName);
            new MongoClient(MongoTestSettings.ConnectionString).DropDatabase(DatabaseName);
        }
    }
}
