using System.Net;
using AiTodo.Application.Features.Todos;
using AiTodo.Domain.Todos;
using AiTodo.Infrastructure;
using AiTodo.Infrastructure.Mongo;
using AiTodo.Api.IntegrationTests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AiTodo.Api.IntegrationTests.Integration;

/// <summary>End-to-end: HTTP -> endpoints -> handlers -> MongoDB (and back), in a throwaway database.</summary>
public sealed class MongoApiIntegrationTests(MongoApiFactory factory) : IClassFixture<MongoApiFactory>
{
    [MongoFact]
    public async Task The_api_works_against_the_test_database_and_never_the_development_one()
    {
        using var client = factory.CreateClient();
        var options = factory.Services.GetRequiredService<IOptions<MongoOptions>>().Value;

        Assert.Equal(factory.DatabaseName, options.DatabaseName);
        Assert.StartsWith(MongoTestSettings.DatabasePrefix, options.DatabaseName);
        Assert.NotEqual(MongoTestSettings.DevelopmentDatabaseName, options.DatabaseName);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/todos")).StatusCode);
    }

    [MongoFact]
    public async Task Full_lifecycle_create_get_list_update_change_status_delete()
    {
        using var client = factory.CreateClient();

        // create
        var createResponse = await client.PostJsonAsync("/api/todos", """{"title":"  Ciclo completo ","description":"fim a fim"}""");
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.ReadTodoAsync();
        Assert.Equal($"/api/todos/{created.Id}", createResponse.Headers.Location?.ToString());
        Assert.Equal("Ciclo completo", created.Title);
        Assert.Equal(TodoStatus.ToDo, created.Status);

        // the document really is in the test database
        var raw = await factory.Database.GetCollection<BsonDocument>("Todos")
            .Find(new BsonDocument("_id", ObjectId.Parse(created.Id))).SingleAsync();
        Assert.Equal("Ciclo completo", raw["title"].AsString);
        Assert.Equal("ToDo", raw["status"].AsString);

        // get
        Assert.Equal(created, await (await client.GetAsync($"/api/todos/{created.Id}")).ReadTodoAsync());

        // update
        await Task.Delay(20); // guarantee a later UpdatedAt than CreatedAt (millisecond resolution)
        var updated = await (await client.PutJsonAsync($"/api/todos/{created.Id}", """{"title":"Ciclo editado"}""")).ReadTodoAsync();
        Assert.Equal("Ciclo editado", updated.Title);
        Assert.Null(updated.Description);
        Assert.Equal(created.CreatedAt, updated.CreatedAt);
        Assert.True(updated.UpdatedAt > created.UpdatedAt);

        // same values again: nothing changes
        var unchanged = await (await client.PutJsonAsync($"/api/todos/{created.Id}", """{"title":"Ciclo editado"}""")).ReadTodoAsync();
        Assert.Equal(updated.UpdatedAt, unchanged.UpdatedAt);

        // status
        await Task.Delay(20);
        var done = await (await client.PatchJsonAsync($"/api/todos/{created.Id}/status", """{"status":"Done"}""")).ReadTodoAsync();
        Assert.Equal(TodoStatus.Done, done.Status);
        Assert.True(done.UpdatedAt > updated.UpdatedAt);

        // list + filters
        var all = await (await client.GetAsync("/api/todos")).ReadTodosAsync();
        Assert.Contains(all, t => t.Id == created.Id && t.Status == TodoStatus.Done);
        var byStatus = await (await client.GetAsync("/api/todos?status=Done&search=EDITADO")).ReadTodosAsync();
        Assert.Contains(byStatus, t => t.Id == created.Id);
        Assert.Empty(await (await client.GetAsync("/api/todos?status=ToDo&search=editado")).ReadTodosAsync());

        // delete
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/todos/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/todos/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/todos/{created.Id}")).StatusCode);
    }

    [MongoFact]
    public async Task Operations_on_an_unknown_id_return_404_without_creating_documents()
    {
        using var client = factory.CreateClient();
        var before = await factory.Database.GetCollection<BsonDocument>("Todos").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        var unknown = ObjectId.GenerateNewId();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/todos/{unknown}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutJsonAsync($"/api/todos/{unknown}", """{"title":"x"}""")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PatchJsonAsync($"/api/todos/{unknown}/status", """{"status":"Done"}""")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/todos/{unknown}")).StatusCode);

        var after = await factory.Database.GetCollection<BsonDocument>("Todos").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        Assert.Equal(before, after);
    }

    [MongoFact]
    public async Task Invalid_requests_do_not_modify_stored_data()
    {
        using var client = factory.CreateClient();
        var created = await (await client.PostJsonAsync("/api/todos", """{"title":"intacto","description":"d"}""")).ReadTodoAsync();

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutJsonAsync($"/api/todos/{created.Id}", """{"title":" "}""")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PatchJsonAsync($"/api/todos/{created.Id}/status", """{"status":"Blocked"}""")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostJsonAsync("/api/todos", """{"title":""}""")).StatusCode);

        Assert.Equal(created, await (await client.GetAsync($"/api/todos/{created.Id}")).ReadTodoAsync());
    }

    [MongoFact]
    public async Task Starting_the_api_creates_the_indexes_and_restarting_is_idempotent()
    {
        using var client = factory.CreateClient(); // starts the host, which starts the index initializer
        var indexes = factory.Database.GetCollection<BsonDocument>("Todos").Indexes;

        var names = await WaitForIndexesAsync(indexes, expected: 3);

        Assert.Contains(MongoIndexInitializer.CreatedAtIndexName, names);
        Assert.Contains(MongoIndexInitializer.StatusCreatedAtIndexName, names);

        // a second initializer run (e.g. another API start) must neither fail nor duplicate indexes
        var options = Options.Create(new MongoOptions { ConnectionString = MongoTestSettings.ConnectionString!, DatabaseName = factory.DatabaseName });
        var initializer = new MongoIndexInitializer(factory.Database, options, Microsoft.Extensions.Logging.Abstractions.NullLogger<MongoIndexInitializer>.Instance);
        await initializer.StartAsync(default);
        await initializer.ExecuteTask!;
        await initializer.StopAsync(default);

        Assert.Equal(3, (await (await indexes.ListAsync()).ToListAsync()).Count);
    }

    [MongoFact]
    public async Task The_index_keys_match_the_listing_query_shape()
    {
        using var client = factory.CreateClient();
        var indexes = factory.Database.GetCollection<BsonDocument>("Todos").Indexes;
        await WaitForIndexesAsync(indexes, expected: 3);

        var all = await (await indexes.ListAsync()).ToListAsync();

        var createdAt = all.Single(i => i["name"] == MongoIndexInitializer.CreatedAtIndexName)["key"].AsBsonDocument;
        Assert.Equal(new BsonDocument("createdAt", -1), createdAt);
        var statusCreatedAt = all.Single(i => i["name"] == MongoIndexInitializer.StatusCreatedAtIndexName)["key"].AsBsonDocument;
        Assert.Equal(new BsonDocument { { "status", 1 }, { "createdAt", -1 } }, statusCreatedAt);
    }

    private static async Task<List<string>> WaitForIndexesAsync(IMongoIndexManager<BsonDocument> indexes, int expected)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (true)
        {
            List<string> names;
            try
            {
                names = (await (await indexes.ListAsync()).ToListAsync()).Select(i => i["name"].AsString).ToList();
            }
            catch (MongoCommandException)
            {
                names = []; // collection does not exist yet
            }

            if (names.Count >= expected || DateTime.UtcNow > deadline)
            {
                return names;
            }

            await Task.Delay(100);
        }
    }
}
