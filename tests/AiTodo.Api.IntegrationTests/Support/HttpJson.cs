using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiTodo.Application.Features.Todos;
using AiTodo.Domain.Todos;

namespace AiTodo.Api.IntegrationTests.Support;

/// <summary>Helpers shared by the endpoint and integration tests.</summary>
public static class HttpJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");

    public static Task<HttpResponseMessage> PostJsonAsync(this HttpClient client, string url, string json)
        => client.PostAsync(url, Json(json));

    public static Task<HttpResponseMessage> PutJsonAsync(this HttpClient client, string url, string json)
        => client.PutAsync(url, Json(json));

    public static Task<HttpResponseMessage> PatchJsonAsync(this HttpClient client, string url, string json)
        => client.PatchAsync(url, Json(json));

    public static async Task<TodoResponse> ReadTodoAsync(this HttpResponseMessage response)
        => (await response.Content.ReadFromJsonAsync<TodoResponse>(Options))!;

    public static async Task<List<TodoResponse>> ReadTodosAsync(this HttpResponseMessage response)
        => (await response.Content.ReadFromJsonAsync<List<TodoResponse>>(Options))!;

    public static async Task<JsonDocument> ReadJsonAsync(this HttpResponseMessage response)
        => JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    /// <summary>Names of the fields listed in the "errors" array of a problem response.</summary>
    public static async Task<string[]> ReadErrorFieldsAsync(this HttpResponseMessage response)
    {
        using var json = await response.ReadJsonAsync();
        return json.RootElement.GetProperty("errors").EnumerateArray()
            .Select(e => e.GetProperty("name").GetString()!)
            .ToArray();
    }

    public static async Task<string[]> ReadErrorReasonsAsync(this HttpResponseMessage response)
    {
        using var json = await response.ReadJsonAsync();
        return json.RootElement.GetProperty("errors").EnumerateArray()
            .Select(e => e.GetProperty("reason").GetString()!)
            .ToArray();
    }
}
