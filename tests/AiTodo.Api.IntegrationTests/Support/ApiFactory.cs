using AiTodo.Application.Features.Todos;
using AiTodo.Domain.Todos;
using AiTodo.Tests.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace AiTodo.Api.IntegrationTests.Support;

/// <summary>
/// Hosts the real API pipeline (routing, FastEndpoints, command bus, validation, error handling, JSON)
/// with both persistence ports replaced by an in-memory double and a fake clock. It never connects to
/// MongoDB: the index initializer is removed and the connection string only satisfies options validation.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public InMemoryTodoRepository Repository { get; } = new();

    public FakeClock Clock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Mongo:ConnectionString"] = "mongodb://never-contacted.invalid:27017",
                ["Mongo:DatabaseName"] = "AiTodoDb_Tests_NeverUsed"
            }));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ITodoRepository>();
            services.RemoveAll<ITodoReadRepository>();
            services.AddSingleton<ITodoRepository>(Repository);
            services.AddSingleton<ITodoReadRepository>(Repository);

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);

            services.RemoveAll<IHostedService>(); // MongoIndexInitializer is the only hosted service registered
        });
    }
}
