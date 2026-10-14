using AiTodo.Application.Features.Todos;
using AiTodo.Domain.Todos;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AiTodo.Api.IntegrationTests.Integration;

/// <summary>
/// Persistence behavior of both repositories (write and read ports) against a real MongoDB
/// (specs: todo-persistence, todo-management).
/// </summary>
public sealed class TodoRepositoriesIntegrationTests(MongoDatabaseFixture mongo)
    : IClassFixture<MongoDatabaseFixture>, IAsyncLifetime
{
    private static readonly DateTime T0 = new(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);

    public async Task InitializeAsync()
    {
        if (MongoTestSettings.IsConfigured)
        {
            await mongo.RawTodos.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Todo> InsertAsync(string title, string? description = null, TodoStatus status = TodoStatus.ToDo, int minutes = 0)
    {
        var todo = Todo.Create(mongo.Repository.NextId(), title, description, T0.AddMinutes(minutes));
        if (status != TodoStatus.ToDo)
        {
            todo.ChangeStatus(status, T0.AddMinutes(minutes).AddSeconds(1));
        }

        await mongo.Repository.AddAsync(todo, default);
        return todo;
    }

    private BsonDocument Raw(TodoId id)
        => mongo.RawTodos.Find(new BsonDocument("_id", ObjectId.Parse(id.Value))).Single();

    private async Task<IReadOnlyList<TodoResponse>> ListAsync(TodoStatus? status = null, string? search = null)
        => await mongo.ReadRepository.ListAsync(status, search, default);

    private async Task<long> CountAsync() => await mongo.RawTodos.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);

    // ---- Identity ---------------------------------------------------------------------------

    [MongoFact]
    public void NextId_returns_unique_valid_identifiers()
    {
        var ids = Enumerable.Range(0, 100).Select(_ => mongo.Repository.NextId()).ToList();

        Assert.Equal(100, ids.Distinct().Count());
        Assert.All(ids, id =>
        {
            Assert.Equal(TodoId.Length, id.Value.Length);
            Assert.True(ObjectId.TryParse(id.Value, out _));
        });
    }

    // ---- Document shape ---------------------------------------------------------------------

    [MongoFact]
    public async Task Documents_are_stored_with_ObjectId_string_status_utc_dates_and_camelCase_names()
    {
        var todo = await InsertAsync("Estudar", "desc");

        var raw = Raw(todo.Id);

        Assert.Equal(BsonType.ObjectId, raw["_id"].BsonType);
        Assert.Equal(todo.Id.Value, raw["_id"].AsObjectId.ToString());
        Assert.Equal(BsonType.String, raw["status"].BsonType);
        Assert.Equal("ToDo", raw["status"].AsString);
        Assert.Equal(BsonType.DateTime, raw["createdAt"].BsonType);
        Assert.Equal(BsonType.DateTime, raw["updatedAt"].BsonType);
        Assert.Equal(T0, raw["createdAt"].ToUniversalTime());
        Assert.Equal(
            ["_id", "createdAt", "description", "status", "title", "updatedAt"],
            raw.Names.OrderBy(n => n, StringComparer.Ordinal));
    }

    [MongoFact]
    public async Task A_missing_description_is_not_stored()
    {
        var todo = await InsertAsync("sem descrição");

        Assert.False(Raw(todo.Id).Contains("description"));
    }

    [MongoFact]
    public async Task Status_is_stored_by_name_for_every_value()
    {
        foreach (var status in Enum.GetValues<TodoStatus>())
        {
            var todo = await InsertAsync($"t-{status}", status: status);

            Assert.Equal(status.ToString(), Raw(todo.Id)["status"].AsString);
        }
    }

    // ---- Round trip (write side rebuilds the aggregate; read side builds the DTO) -----------

    [MongoFact]
    public async Task The_aggregate_read_back_equals_the_one_stored_with_utc_dates()
    {
        var todo = await InsertAsync("Round trip", "desc", TodoStatus.Doing);

        var back = await mongo.Repository.GetByIdAsync(todo.Id, default);

        Assert.NotNull(back);
        Assert.Equal(todo.ToResponse(), back.ToResponse());
        Assert.Equal(todo.Id, back.Id);
        Assert.Equal(DateTimeKind.Utc, back.CreatedAt.Kind);
        Assert.Equal(DateTimeKind.Utc, back.UpdatedAt.Kind);
    }

    [MongoFact]
    public async Task The_read_model_returns_the_same_data_as_the_aggregate_without_rebuilding_it()
    {
        var todo = await InsertAsync("Leitura", "direta", TodoStatus.Done);

        var dto = await mongo.ReadRepository.GetByIdAsync(todo.Id, default);

        Assert.Equal(todo.ToResponse(), dto);
        Assert.Equal(DateTimeKind.Utc, dto!.CreatedAt.Kind);
        Assert.Equal(todo.Id.Value, dto.Id);
    }

    [MongoFact]
    public async Task Accents_and_emoji_survive_the_round_trip()
    {
        var todo = await InsertAsync("Açaí com pão 🍞", "coração");

        var back = await mongo.ReadRepository.GetByIdAsync(todo.Id, default);

        Assert.Equal("Açaí com pão 🍞", back!.Title);
        Assert.Equal("coração", back.Description);
    }

    [MongoFact]
    public async Task Get_returns_null_for_an_unknown_id_on_both_sides()
    {
        await InsertAsync("x");
        var unknown = mongo.Repository.NextId();

        Assert.Null(await mongo.Repository.GetByIdAsync(unknown, default));
        Assert.Null(await mongo.ReadRepository.GetByIdAsync(unknown, default));
    }

    // ---- Update / Delete --------------------------------------------------------------------

    [MongoFact]
    public async Task Update_persists_the_changes_and_touches_only_that_document()
    {
        var target = await InsertAsync("alvo", "desc");
        var other = await InsertAsync("outro");
        target.Update("alvo editado", null, T0.AddHours(1));

        Assert.True(await mongo.Repository.UpdateAsync(target, default));

        var back = await mongo.Repository.GetByIdAsync(target.Id, default);
        Assert.Equal("alvo editado", back!.Title);
        Assert.Null(back.Description);
        Assert.False(Raw(target.Id).Contains("description"));
        Assert.Equal("outro", (await mongo.Repository.GetByIdAsync(other.Id, default))!.Title);
    }

    [MongoFact]
    public async Task Update_of_a_missing_todo_returns_false_and_does_not_insert()
    {
        var ghost = Todo.Create(mongo.Repository.NextId(), "fantasma", null, T0);

        Assert.False(await mongo.Repository.UpdateAsync(ghost, default));
        Assert.Equal(0, await CountAsync());
    }

    [MongoFact]
    public async Task Delete_removes_one_document_and_reports_whether_it_existed()
    {
        var first = await InsertAsync("um");
        await InsertAsync("dois");

        Assert.True(await mongo.Repository.DeleteAsync(first.Id, default));
        Assert.False(await mongo.Repository.DeleteAsync(first.Id, default));
        Assert.Equal(1, await CountAsync());
    }

    // ---- List -------------------------------------------------------------------------------

    [MongoFact]
    public async Task List_orders_newest_first()
    {
        var a = await InsertAsync("a", minutes: 1);
        var c = await InsertAsync("c", minutes: 3);
        var b = await InsertAsync("b", minutes: 2);

        var list = await ListAsync();

        Assert.Equal([c.Id.Value, b.Id.Value, a.Id.Value], list.Select(t => t.Id));
    }

    [MongoFact]
    public async Task List_breaks_ties_between_equal_creation_times_by_id_descending()
    {
        var first = await InsertAsync("x", minutes: 5);
        var second = await InsertAsync("y", minutes: 5);
        var third = await InsertAsync("z", minutes: 5);

        var list = await ListAsync();

        var expected = new[] { first.Id.Value, second.Id.Value, third.Id.Value }.OrderByDescending(id => id, StringComparer.Ordinal);
        Assert.Equal(expected, list.Select(t => t.Id));
    }

    [MongoFact]
    public async Task List_filters_by_status()
    {
        await InsertAsync("a");
        var doing = await InsertAsync("b", status: TodoStatus.Doing);
        await InsertAsync("c", status: TodoStatus.Done);

        var list = await ListAsync(TodoStatus.Doing);

        Assert.Equal([doing.Id.Value], list.Select(t => t.Id));
    }

    [MongoFact]
    public async Task List_searches_the_title_ignoring_case_and_matching_in_the_middle()
    {
        var match = await InsertAsync("Revisar a API de Tarefas");
        await InsertAsync("Comprar pão");

        Assert.Equal([match.Id.Value], (await ListAsync(search: "api")).Select(t => t.Id));
        Assert.Equal([match.Id.Value], (await ListAsync(search: "TAREFAS")).Select(t => t.Id));
        Assert.Equal([match.Id.Value], (await ListAsync(search: "  de  ")).Select(t => t.Id));
    }

    [MongoFact]
    public async Task List_searches_accented_text()
    {
        var match = await InsertAsync("Comprar pão");
        await InsertAsync("Comprar leite");

        var list = await ListAsync(search: "pão");

        Assert.Equal([match.Id.Value], list.Select(t => t.Id));
    }

    [MongoTheory]
    [InlineData(".*")]
    [InlineData("a.*")]
    [InlineData("^")]
    [InlineData("$")]
    [InlineData("(")]
    [InlineData("[a-z]")]
    [InlineData("\\")]
    [InlineData("a|b")]
    [InlineData("(?i)")]
    public async Task List_treats_regex_metacharacters_in_the_search_as_literal_text(string term)
    {
        await InsertAsync("alpha");
        await InsertAsync("bravo");

        var list = await ListAsync(search: term);

        Assert.Empty(list); // none of the titles literally contains the metacharacters
    }

    [MongoFact]
    public async Task List_matches_a_search_term_that_literally_contains_metacharacters()
    {
        var match = await InsertAsync("Revisar API.* (v2)");
        await InsertAsync("Revisar APIs");

        var list = await ListAsync(search: "API.* (v2)");

        Assert.Equal([match.Id.Value], list.Select(t => t.Id));
    }

    [MongoFact]
    public async Task List_combines_status_and_search_with_AND()
    {
        await InsertAsync("estudar api", status: TodoStatus.Doing);
        var both = await InsertAsync("estudar api 2", status: TodoStatus.Done, minutes: 1);
        await InsertAsync("comprar", status: TodoStatus.Done, minutes: 2);

        var list = await ListAsync(TodoStatus.Done, "estudar");

        Assert.Equal([both.Id.Value], list.Select(t => t.Id));
    }

    [MongoFact]
    public async Task List_with_a_blank_search_applies_no_filter_and_with_no_match_returns_empty()
    {
        await InsertAsync("a");

        Assert.Single(await ListAsync(search: "   "));
        Assert.Empty(await ListAsync(search: "zzz"));
    }
}
