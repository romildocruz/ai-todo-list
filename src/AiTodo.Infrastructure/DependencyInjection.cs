using AiTodo.Application.Features.Todos;
using AiTodo.Domain.Todos;
using AiTodo.Infrastructure.Mongo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AiTodo.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers MongoDB access: options, client, repositories, BSON mapping and index creation.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        TodoBsonMapping.Register();

        services.AddOptions<MongoOptions>()
            .BindConfiguration(MongoOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // MongoClient is thread-safe and manages its own connection pool: keep a single instance.
        // It connects lazily, so the API starts even when MongoDB is down.
        services.AddSingleton<IMongoClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
            var settings = MongoClientSettings.FromConnectionString(options.ConnectionString);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(options.ServerSelectionTimeoutSeconds);
            settings.ConnectTimeout = TimeSpan.FromSeconds(options.ConnectTimeoutSeconds);
            return new MongoClient(settings);
        });

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
            return sp.GetRequiredService<IMongoClient>().GetDatabase(options.DatabaseName);
        });

        services.AddSingleton(sp => CollectionOf<Todo>(sp));
        services.AddSingleton(sp => CollectionOf<TodoReadModel>(sp));

        services.AddSingleton<ITodoRepository, TodoRepository>();
        services.AddSingleton<ITodoReadRepository, TodoReadRepository>();

        services.AddHostedService<MongoIndexInitializer>();

        return services;
    }

    private static IMongoCollection<T> CollectionOf<T>(IServiceProvider sp)
    {
        var options = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
        return sp.GetRequiredService<IMongoDatabase>().GetCollection<T>(options.CollectionName);
    }
}
