using AiTodo.Domain.Todos;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace AiTodo.Infrastructure.Mongo;

/// <summary>
/// BSON mapping through <see cref="BsonClassMap"/> (never attributes, so the Domain stays clean):
/// TodoId stored as ObjectId, status stored as string, dates stored as UTC, camelCase field names.
/// The aggregate (writes) and the read model (queries) map the same documents.
/// </summary>
public static class TodoBsonMapping
{
    private static readonly object Gate = new();

    public static void Register()
    {
        lock (Gate)
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(Todo)))
            {
                BsonClassMap.RegisterClassMap<Todo>(map =>
                {
                    map.SetIgnoreExtraElements(true);
                    map.MapIdMember(t => t.Id).SetSerializer(new TodoIdSerializer());
                    map.MapMember(t => t.Title).SetElementName("title");
                    map.MapMember(t => t.Description).SetElementName("description").SetIgnoreIfNull(true);
                    map.MapMember(t => t.Status).SetElementName("status")
                        .SetSerializer(new EnumSerializer<TodoStatus>(BsonType.String));
                    map.MapMember(t => t.CreatedAt).SetElementName("createdAt")
                        .SetSerializer(new DateTimeSerializer(DateTimeKind.Utc));
                    map.MapMember(t => t.UpdatedAt).SetElementName("updatedAt")
                        .SetSerializer(new DateTimeSerializer(DateTimeKind.Utc));
                });
            }

            if (!BsonClassMap.IsClassMapRegistered(typeof(TodoReadModel)))
            {
                BsonClassMap.RegisterClassMap<TodoReadModel>(map =>
                {
                    map.SetIgnoreExtraElements(true);
                    map.MapIdMember(t => t.Id);
                    map.MapMember(t => t.Title).SetElementName("title");
                    map.MapMember(t => t.Description).SetElementName("description");
                    map.MapMember(t => t.Status).SetElementName("status")
                        .SetSerializer(new EnumSerializer<TodoStatus>(BsonType.String));
                    map.MapMember(t => t.CreatedAt).SetElementName("createdAt")
                        .SetSerializer(new DateTimeSerializer(DateTimeKind.Utc));
                    map.MapMember(t => t.UpdatedAt).SetElementName("updatedAt")
                        .SetSerializer(new DateTimeSerializer(DateTimeKind.Utc));
                });
            }
        }
    }
}

/// <summary>Stores the typed <see cref="TodoId"/> as a native MongoDB ObjectId.</summary>
internal sealed class TodoIdSerializer : SerializerBase<TodoId>
{
    public override TodoId Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        => TodoId.Parse(context.Reader.ReadObjectId().ToString());

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TodoId value)
        => context.Writer.WriteObjectId(ObjectId.Parse(value.Value));
}
