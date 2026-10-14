using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AiTodo.Application.Features.Todos;
using AiTodo.Domain.Todos;
using AiTodo.Api.IntegrationTests.Support;
using AiTodo.Tests.Common;

namespace AiTodo.Api.IntegrationTests.Endpoints;

/// <summary>HTTP contract of the six endpoints (specs: todo-api, todo-management).</summary>
public sealed class TodoEndpointsTests : IClassFixture<ApiFactory>
{
    private const string UnknownId = "000000000000000000000000";

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public TodoEndpointsTests(ApiFactory factory)
    {
        _factory = factory;
        _factory.Repository.Clear();
        _client = factory.CreateClient();
    }

    private async Task<TodoResponse> CreateAsync(string title, string? description = null)
    {
        _factory.Clock.Advance(TimeSpan.FromMinutes(1));
        var body = description is null ? $$"""{"title":"{{title}}"}""" : $$"""{"title":"{{title}}","description":"{{description}}"}""";
        var response = await _client.PostJsonAsync("/api/todos", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadTodoAsync();
    }

    private static void AssertProblem(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    // ---- POST /api/todos -------------------------------------------------------------------

    [Fact]
    public async Task Post_creates_a_todo_returning_201_with_location_and_the_new_entity()
    {
        var response = await _client.PostJsonAsync("/api/todos", """{"title":"  Estudar CQRS ","description":"handlers"}""");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var todo = await response.ReadTodoAsync();
        Assert.Equal($"/api/todos/{todo.Id}", response.Headers.Location?.ToString());
        Assert.Equal("Estudar CQRS", todo.Title);
        Assert.Equal("handlers", todo.Description);
        Assert.Equal(TodoStatus.ToDo, todo.Status);
        Assert.Equal(_factory.Clock.UtcNow, todo.CreatedAt);
        Assert.Equal(todo.CreatedAt, todo.UpdatedAt);
        Assert.Equal(1, _factory.Repository.Count);
    }

    [Fact]
    public async Task Post_serializes_status_as_a_string_and_dates_in_utc_with_Z()
    {
        var response = await _client.PostJsonAsync("/api/todos", """{"title":"t"}""");

        using var json = await response.ReadJsonAsync();
        Assert.Equal("ToDo", json.RootElement.GetProperty("status").GetString());
        Assert.EndsWith("Z", json.RootElement.GetProperty("createdAt").GetString());
        Assert.EndsWith("Z", json.RootElement.GetProperty("updatedAt").GetString());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("description").ValueKind);
    }

    [Fact]
    public async Task Post_ignores_id_status_and_dates_sent_by_the_client()
    {
        var response = await _client.PostJsonAsync("/api/todos", """
            {"title":"t","id":"6acf964fff4285ed0dc88831","status":"Done","createdAt":"2000-01-01T00:00:00Z","updatedAt":"2000-01-01T00:00:00Z"}
            """);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var todo = await response.ReadTodoAsync();
        Assert.NotEqual("6acf964fff4285ed0dc88831", todo.Id);
        Assert.Equal(TodoStatus.ToDo, todo.Status);
        Assert.Equal(_factory.Clock.UtcNow, todo.CreatedAt);
    }

    [Theory]
    [InlineData("""{}""")]
    [InlineData("""{"title":null}""")]
    [InlineData("""{"title":""}""")]
    [InlineData("""{"title":"   "}""")]
    public async Task Post_without_a_usable_title_returns_400_problem_and_stores_nothing(string body)
    {
        var response = await _client.PostJsonAsync("/api/todos", body);

        AssertProblem(response, HttpStatusCode.BadRequest);
        Assert.Contains("title", await response.ReadErrorFieldsAsync());
        Assert.Equal(["O título é obrigatório."], await response.ReadErrorReasonsAsync());
        Assert.Equal(0, _factory.Repository.Count);
    }

    [Fact]
    public async Task Post_with_a_title_over_100_characters_returns_400()
    {
        var response = await _client.PostJsonAsync("/api/todos", $$"""{"title":"{{new string('x', 101)}}"}""");

        AssertProblem(response, HttpStatusCode.BadRequest);
        Assert.Contains("title", await response.ReadErrorFieldsAsync());
        Assert.Equal(0, _factory.Repository.Count);
    }

    [Fact]
    public async Task Post_with_a_description_over_500_characters_returns_400()
    {
        var response = await _client.PostJsonAsync("/api/todos", $$"""{"title":"t","description":"{{new string('x', 501)}}"}""");

        AssertProblem(response, HttpStatusCode.BadRequest);
        Assert.Equal(["description"], await response.ReadErrorFieldsAsync());
        Assert.Equal(0, _factory.Repository.Count);
    }

    [Fact]
    public async Task Post_reports_every_invalid_field_at_once()
    {
        var response = await _client.PostJsonAsync("/api/todos", $$"""{"title":"","description":"{{new string('x', 501)}}"}""");

        AssertProblem(response, HttpStatusCode.BadRequest);
        var fields = await response.ReadErrorFieldsAsync();
        Assert.Contains("title", fields);
        Assert.Contains("description", fields);
    }

    [Theory]
    [InlineData("""{"title": """)]
    [InlineData("not json")]
    [InlineData("""{"title":123}""")]
    [InlineData("""[1,2]""")]
    public async Task Post_with_malformed_json_returns_400_problem_without_leaking_internals(string body)
    {
        var response = await _client.PostJsonAsync("/api/todos", body);

        AssertProblem(response, HttpStatusCode.BadRequest);
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("System.", text);
        Assert.DoesNotContain("AiTodo", text);
        Assert.DoesNotContain("LineNumber", text);
        Assert.Equal(0, _factory.Repository.Count);
    }

    [Fact]
    public async Task Post_with_an_empty_json_body_returns_400_validation_error()
    {
        var response = await _client.PostJsonAsync("/api/todos", "");

        AssertProblem(response, HttpStatusCode.BadRequest);
        Assert.Contains("title", await response.ReadErrorFieldsAsync());
    }

    [Fact]
    public async Task Post_without_a_json_content_type_returns_415()
    {
        var response = await _client.PostAsync("/api/todos", new StringContent("""{"title":"t"}""", Encoding.UTF8, "text/plain"));

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        Assert.Equal(0, _factory.Repository.Count);
    }

    // ---- GET /api/todos/{id} ---------------------------------------------------------------

    [Fact]
    public async Task Get_by_id_returns_200_with_the_todo()
    {
        var created = await CreateAsync("Estudar", "desc");

        var response = await _client.GetAsync($"/api/todos/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created, await response.ReadTodoAsync());
    }

    [Fact]
    public async Task Get_by_id_of_an_unknown_id_returns_404_problem()
    {
        var response = await _client.GetAsync($"/api/todos/{UnknownId}");

        AssertProblem(response, HttpStatusCode.NotFound);
        using var json = await response.ReadJsonAsync();
        Assert.Equal(404, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Atividade não encontrada.", json.RootElement.GetProperty("detail").GetString());
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("6acf964fff4285ed0dc8883")]
    [InlineData("6acf964fff4285ed0dc8883g")]
    public async Task Get_by_id_with_a_malformed_id_returns_400_on_field_id_without_touching_the_repository(string id)
    {
        _factory.Repository.FailWith = new InvalidOperationException("repository must not be reached");

        try
        {
            var response = await _client.GetAsync($"/api/todos/{id}");

            AssertProblem(response, HttpStatusCode.BadRequest);
            Assert.Equal(["id"], await response.ReadErrorFieldsAsync());
        }
        finally
        {
            _factory.Repository.FailWith = null;
        }
    }

    // ---- GET /api/todos --------------------------------------------------------------------

    [Fact]
    public async Task List_returns_200_with_an_empty_array_when_there_is_nothing()
    {
        var response = await _client.GetAsync("/api/todos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(await response.ReadTodosAsync());
    }

    [Fact]
    public async Task List_returns_everything_newest_first()
    {
        var a = await CreateAsync("a");
        var b = await CreateAsync("b");

        var list = await (await _client.GetAsync("/api/todos")).ReadTodosAsync();

        Assert.Equal([b.Id, a.Id], list.Select(t => t.Id));
    }

    [Fact]
    public async Task List_filters_by_status_name_ignoring_case()
    {
        await CreateAsync("a");
        var doing = await CreateAsync("b");
        await _client.PatchJsonAsync($"/api/todos/{doing.Id}/status", """{"status":"Doing"}""");

        var list = await (await _client.GetAsync("/api/todos?status=doing")).ReadTodosAsync();

        Assert.Equal([doing.Id], list.Select(t => t.Id));
    }

    [Fact]
    public async Task List_searches_by_title_and_combines_with_status()
    {
        var match = await CreateAsync("Estudar API");
        await CreateAsync("Comprar pão");

        var bySearch = await (await _client.GetAsync("/api/todos?search=estudar")).ReadTodosAsync();
        var combined = await (await _client.GetAsync("/api/todos?search=estudar&status=Done")).ReadTodosAsync();

        Assert.Equal([match.Id], bySearch.Select(t => t.Id));
        Assert.Empty(combined);
    }

    [Fact]
    public async Task List_treats_a_blank_status_as_no_filter()
    {
        await CreateAsync("a");

        var list = await (await _client.GetAsync("/api/todos?status=")).ReadTodosAsync();

        Assert.Single(list);
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("1")]
    [InlineData("ToDo,Doing")]
    public async Task List_with_an_invalid_status_returns_400_on_field_status(string status)
    {
        var response = await _client.GetAsync($"/api/todos?status={Uri.EscapeDataString(status)}");

        AssertProblem(response, HttpStatusCode.BadRequest);
        Assert.Equal(["status"], await response.ReadErrorFieldsAsync());
    }

    // ---- PUT /api/todos/{id} ---------------------------------------------------------------

    [Fact]
    public async Task Put_updates_title_and_description_returning_200()
    {
        var created = await CreateAsync("antigo", "desc");
        _factory.Clock.Advance(TimeSpan.FromMinutes(30));

        var response = await _client.PutJsonAsync($"/api/todos/{created.Id}", """{"title":"novo","description":"outra"}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.ReadTodoAsync();
        Assert.Equal("novo", updated.Title);
        Assert.Equal("outra", updated.Description);
        Assert.Equal(_factory.Clock.UtcNow, updated.UpdatedAt);
        Assert.Equal(created.CreatedAt, updated.CreatedAt);
        Assert.Equal(created.Status, updated.Status);
        Assert.Equal(updated, await (await _client.GetAsync($"/api/todos/{created.Id}")).ReadTodoAsync());
    }

    [Fact]
    public async Task Put_without_description_clears_it()
    {
        var created = await CreateAsync("t", "desc");

        var updated = await (await _client.PutJsonAsync($"/api/todos/{created.Id}", """{"title":"t"}""")).ReadTodoAsync();

        Assert.Null(updated.Description);
    }

    [Fact]
    public async Task Put_with_unchanged_values_returns_200_and_keeps_UpdatedAt()
    {
        var created = await CreateAsync("igual", "desc");
        _factory.Clock.Advance(TimeSpan.FromHours(1));

        var response = await _client.PutJsonAsync($"/api/todos/{created.Id}", """{"title":"igual","description":"desc"}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created.UpdatedAt, (await response.ReadTodoAsync()).UpdatedAt);
    }

    [Fact]
    public async Task Put_with_an_invalid_title_returns_400_and_leaves_the_todo_unchanged()
    {
        var created = await CreateAsync("original", "desc");

        var response = await _client.PutJsonAsync($"/api/todos/{created.Id}", """{"title":"  "}""");

        AssertProblem(response, HttpStatusCode.BadRequest);
        Assert.Contains("title", await response.ReadErrorFieldsAsync());
        Assert.Equal(created, await (await _client.GetAsync($"/api/todos/{created.Id}")).ReadTodoAsync());
    }

    [Fact]
    public async Task Put_of_an_unknown_id_returns_404_and_creates_nothing()
    {
        var response = await _client.PutJsonAsync($"/api/todos/{UnknownId}", """{"title":"x"}""");

        AssertProblem(response, HttpStatusCode.NotFound);
        Assert.Equal(0, _factory.Repository.Count);
    }

    [Fact]
    public async Task Put_with_a_malformed_id_returns_400()
    {
        var response = await _client.PutJsonAsync("/api/todos/abc", """{"title":"x"}""");

        AssertProblem(response, HttpStatusCode.BadRequest);
        Assert.Contains("id", await response.ReadErrorFieldsAsync());
    }

    // ---- PATCH /api/todos/{id}/status ------------------------------------------------------

    [Theory]
    [InlineData("Doing", TodoStatus.Doing)]
    [InlineData("Done", TodoStatus.Done)]
    [InlineData("doing", TodoStatus.Doing)]
    public async Task Patch_changes_the_status_returning_200(string status, TodoStatus expected)
    {
        var created = await CreateAsync("t");
        _factory.Clock.Advance(TimeSpan.FromMinutes(5));

        var response = await _client.PatchJsonAsync($"/api/todos/{created.Id}/status", $$"""{"status":"{{status}}"}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.ReadTodoAsync();
        Assert.Equal(expected, updated.Status);
        Assert.Equal(_factory.Clock.UtcNow, updated.UpdatedAt);
        Assert.Equal(created.CreatedAt, updated.CreatedAt);
    }

    [Fact]
    public async Task Patch_with_the_current_status_returns_200_and_keeps_UpdatedAt()
    {
        var created = await CreateAsync("t");
        _factory.Clock.Advance(TimeSpan.FromHours(1));

        var response = await _client.PatchJsonAsync($"/api/todos/{created.Id}/status", """{"status":"ToDo"}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created.UpdatedAt, (await response.ReadTodoAsync()).UpdatedAt);
    }

    [Theory]
    [InlineData("""{"status":"Blocked"}""")]
    [InlineData("""{"status":1}""")]
    [InlineData("""{"status":0}""")]
    [InlineData("""{"status":"1"}""")]
    [InlineData("""{"status":"ToDo,Doing"}""")]
    [InlineData("""{"status":null}""")]
    [InlineData("""{}""")]
    public async Task Patch_with_an_invalid_or_missing_status_returns_400_and_leaves_the_todo_unchanged(string body)
    {
        var created = await CreateAsync("t");

        var response = await _client.PatchJsonAsync($"/api/todos/{created.Id}/status", body);

        AssertProblem(response, HttpStatusCode.BadRequest);
        Assert.Equal(created, await (await _client.GetAsync($"/api/todos/{created.Id}")).ReadTodoAsync());
    }

    [Fact]
    public async Task Patch_of_an_unknown_id_returns_404()
    {
        var response = await _client.PatchJsonAsync($"/api/todos/{UnknownId}/status", """{"status":"Done"}""");

        AssertProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Patch_with_a_malformed_id_returns_400()
    {
        var response = await _client.PatchJsonAsync("/api/todos/abc/status", """{"status":"Done"}""");

        AssertProblem(response, HttpStatusCode.BadRequest);
        Assert.Contains("id", await response.ReadErrorFieldsAsync());
    }

    // ---- DELETE /api/todos/{id} ------------------------------------------------------------

    [Fact]
    public async Task Delete_returns_204_without_a_body_and_removes_only_that_todo()
    {
        var first = await CreateAsync("um");
        var second = await CreateAsync("dois");

        var response = await _client.DeleteAsync($"/api/todos/{first.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/todos/{first.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/todos/{second.Id}")).StatusCode);
    }

    [Fact]
    public async Task Delete_twice_returns_404_the_second_time()
    {
        var created = await CreateAsync("t");

        await _client.DeleteAsync($"/api/todos/{created.Id}");
        var second = await _client.DeleteAsync($"/api/todos/{created.Id}");

        AssertProblem(second, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_with_a_malformed_id_returns_400()
    {
        var response = await _client.DeleteAsync("/api/todos/abc");

        AssertProblem(response, HttpStatusCode.BadRequest);
    }

    // ---- Bodiless requests sent with a JSON content type -----------------------------------

    [Fact]
    public async Task Get_and_delete_with_a_json_content_type_and_no_body_are_handled_normally()
    {
        var created = await CreateAsync("t");

        var list = new HttpRequestMessage(HttpMethod.Get, "/api/todos") { Content = new ByteArrayContent([]) };
        list.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var delete = new HttpRequestMessage(HttpMethod.Delete, $"/api/todos/{created.Id}") { Content = new ByteArrayContent([]) };
        delete.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(list)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(delete)).StatusCode);
    }
}
