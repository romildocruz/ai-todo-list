using System.Text.Json;
using System.Text.Json.Serialization;
using AiTodo.Domain.Todos;

namespace AiTodo.Api.Serialization;

/// <summary>
/// Strict JSON reader/writer for <see cref="TodoStatus"/>: only the exact names ToDo/Doing/Done.
/// The stock <see cref="JsonStringEnumConverter"/> would also accept flag-style lists such as
/// "ToDo,Doing" (parsed as a bit combination), which the API contract does not allow.
/// </summary>
public sealed class TodoStatusJsonConverter : JsonConverter<TodoStatus>
{
    public override TodoStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && TodoRules.TryParseStatus(reader.GetString(), out var status))
        {
            return status;
        }

        throw new JsonException("Status inválido.");
    }

    public override void Write(Utf8JsonWriter writer, TodoStatus value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
