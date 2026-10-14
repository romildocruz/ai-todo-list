using System.Net;
using System.Text.Json;
using AiTodo.Api.IntegrationTests.Support;
using AiTodo.Application.Common;
using AiTodo.Domain;
using AiTodo.Tests.Common;

namespace AiTodo.Api.IntegrationTests.Endpoints;

/// <summary>Cross-cutting behavior: unexpected failures, database outages, Swagger, CORS, health.</summary>
public sealed class ErrorHandlingAndPlatformTests : IClassFixture<ApiFactory>
{
    private const string SecretMarker = "super-secret-host-and-password";

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public ErrorHandlingAndPlatformTests(ApiFactory factory)
    {
        _factory = factory;
        _factory.Repository.Clear();
        _factory.Repository.FailWith = null;
        _client = factory.CreateClient();
    }

    private static readonly (string Method, string Url, string? Body)[] AllDataEndpoints =
    [
        ("GET", "/api/todos", null),
        ("GET", "/api/todos/6acf964fff4285ed0dc88831", null),
        ("POST", "/api/todos", """{"title":"t"}"""),
        ("PUT", "/api/todos/6acf964fff4285ed0dc88831", """{"title":"t"}"""),
        ("PATCH", "/api/todos/6acf964fff4285ed0dc88831/status", """{"status":"Done"}"""),
        ("DELETE", "/api/todos/6acf964fff4285ed0dc88831", null)
    ];

    public static TheoryData<string, string, string?> DataEndpoints
    {
        get
        {
            var data = new TheoryData<string, string, string?>();
            foreach (var (method, url, body) in AllDataEndpoints)
            {
                data.Add(method, url, body);
            }

            return data;
        }
    }

    private Task<HttpResponseMessage> SendAsync(string method, string url, string? body)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (body is not null)
        {
            request.Content = HttpJson.Json(body);
        }

        return _client.SendAsync(request);
    }

    // ---- 500 -------------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(DataEndpoints))]
    public async Task An_unexpected_failure_returns_a_generic_500_problem_without_internal_details(
        string method, string url, string? body)
    {
        _factory.Repository.FailWith = new InvalidOperationException($"boom {SecretMarker} at C:\\src\\Secret.cs:line 42");

        var response = await SendAsync(method, url, body);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(SecretMarker, text);
        Assert.DoesNotContain("InvalidOperationException", text);
        Assert.DoesNotContain("Secret.cs", text);
        Assert.DoesNotContain("StackTrace", text, StringComparison.OrdinalIgnoreCase);

        using var json = JsonDocument.Parse(text);
        Assert.Equal(500, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Erro interno do servidor.", json.RootElement.GetProperty("title").GetString());
    }

    // ---- 400 from a violated domain rule (safety net behind the request validators) ------------

    [Fact]
    public async Task A_domain_rule_violation_reaching_the_handlers_returns_400_problem_not_500()
    {
        _factory.Repository.FailWith = new DomainException("O título é obrigatório e deve ter no máximo 100 caracteres.", "title");

        var response = await _client.PostJsonAsync("/api/todos", """{"title":"ok"}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = await response.ReadJsonAsync();
        Assert.Equal(400, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("O título é obrigatório e deve ter no máximo 100 caracteres.", json.RootElement.GetProperty("detail").GetString());
    }

    // ---- 503 -------------------------------------------------------------------------------

    public static TheoryData<Exception> DatabaseOutages => new()
    {
        new DataStoreUnavailableException(new TimeoutException($"A timeout occurred after 3000ms selecting a server ... {SecretMarker}:27017")),
        new DataStoreUnavailableException(new IOException($"connection refused by {SecretMarker}"))
    };

    [Theory]
    [MemberData(nameof(DatabaseOutages))]
    public async Task A_database_outage_returns_503_problem_and_the_api_stays_up(Exception outage)
    {
        _factory.Repository.FailWith = outage;

        var response = await _client.GetAsync("/api/todos");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(SecretMarker, text);
        Assert.DoesNotContain("Timeout", text);
        using var json = JsonDocument.Parse(text);
        Assert.Equal(503, json.RootElement.GetProperty("status").GetInt32());

        // recovery: next request works once the database is back
        _factory.Repository.FailWith = null;
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/todos")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/health")).StatusCode);
    }

    [Theory]
    [MemberData(nameof(DataEndpoints))]
    public async Task Every_data_endpoint_maps_a_database_outage_to_503(string method, string url, string? body)
    {
        _factory.Repository.FailWith = new DataStoreUnavailableException(new TimeoutException());

        var response = await SendAsync(method, url, body);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Validation_errors_do_not_reach_the_database_even_when_it_is_down()
    {
        _factory.Repository.FailWith = new DataStoreUnavailableException(new TimeoutException());

        var response = await _client.PostJsonAsync("/api/todos", """{"title":""}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- Health / Swagger / CORS -----------------------------------------------------------

    [Fact]
    public async Task Health_answers_200_without_using_the_database()
    {
        _factory.Repository.FailWith = new DataStoreUnavailableException(new TimeoutException());

        var response = await _client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = await response.ReadJsonAsync();
        Assert.Equal("Healthy", json.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Swagger_document_lists_the_six_todo_operations_and_their_responses()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = await response.ReadJsonAsync();
        var paths = json.RootElement.GetProperty("paths");

        string[] CodesOf(string path, string method)
            => paths.GetProperty(path).GetProperty(method).GetProperty("responses").EnumerateObject().Select(p => p.Name).ToArray();

        Assert.Contains("201", CodesOf("/api/todos", "post"));
        Assert.Contains("400", CodesOf("/api/todos", "post"));
        Assert.Contains("200", CodesOf("/api/todos", "get"));
        Assert.Contains("200", CodesOf("/api/todos/{id}", "get"));
        Assert.Contains("404", CodesOf("/api/todos/{id}", "get"));
        Assert.Contains("200", CodesOf("/api/todos/{id}", "put"));
        Assert.Contains("404", CodesOf("/api/todos/{id}", "put"));
        Assert.Contains("200", CodesOf("/api/todos/{id}/status", "patch"));
        Assert.Contains("404", CodesOf("/api/todos/{id}/status", "patch"));
        Assert.Contains("204", CodesOf("/api/todos/{id}", "delete"));
        Assert.Contains("404", CodesOf("/api/todos/{id}", "delete"));
    }

    [Fact]
    public async Task Swagger_documents_status_as_a_string_enum_with_exactly_the_three_allowed_names()
    {
        using var json = await (await _client.GetAsync("/swagger/v1/swagger.json")).ReadJsonAsync();
        var schemas = json.RootElement.GetProperty("components").GetProperty("schemas");

        var statusSchema = schemas.EnumerateObject().Single(s => s.Name.EndsWith("TodoStatus", StringComparison.Ordinal)).Value;

        Assert.Equal("string", statusSchema.GetProperty("type").GetString());
        Assert.Equal(
            ["ToDo", "Doing", "Done"],
            statusSchema.GetProperty("enum").EnumerateArray().Select(v => v.GetString()!).ToArray());
    }

    [Fact]
    public async Task Cors_allows_the_configured_front_end_origin()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/todos");
        request.Headers.Add("Origin", "http://localhost:5173");

        var response = await _client.SendAsync(request);

        Assert.Equal("http://localhost:5173", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task Cors_preflight_for_the_front_end_allows_the_needed_methods_and_headers()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/todos/6acf964fff4285ed0dc88831/status");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "PATCH");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await _client.SendAsync(request);

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("http://localhost:5173", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Contains("PATCH", string.Join(",", response.Headers.GetValues("Access-Control-Allow-Methods")));
    }

    [Fact]
    public async Task Cors_does_not_allow_other_origins()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/todos");
        request.Headers.Add("Origin", "http://evil.example");

        var response = await _client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
