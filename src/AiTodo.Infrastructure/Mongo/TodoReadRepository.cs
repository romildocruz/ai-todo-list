using System.Text.RegularExpressions;
using AiTodo.Application.Features.Todos;
using AiTodo.Domain.Todos;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AiTodo.Infrastructure.Mongo;

/// <summary>Read-side repository: reads documents into DTOs without going through the aggregate.</summary>
internal sealed class TodoReadRepository(IMongoCollection<TodoReadModel> collection) : ITodoReadRepository
{
    public Task<TodoResponse?> GetByIdAsync(TodoId id, CancellationToken cancellationToken)
        => MongoCall.RunAsync(async () =>
        {
            var objectId = ObjectId.Parse(id.Value);
            var document = await collection.Find(t => t.Id == objectId).FirstOrDefaultAsync(cancellationToken);

            return document is null ? null : ToResponse(document);
        });

    public Task<IReadOnlyList<TodoResponse>> ListAsync(
        TodoStatus? status, string? search, CancellationToken cancellationToken)
        => MongoCall.RunAsync<IReadOnlyList<TodoResponse>>(async () =>
        {
            var builder = Builders<TodoReadModel>.Filter;
            var filter = builder.Empty;

            if (status is { } value)
            {
                filter &= builder.Eq(t => t.Status, value);
            }

            var term = search?.Trim();
            if (!string.IsNullOrEmpty(term))
            {
                // Regex.Escape makes the term literal: user input is never interpreted as a pattern.
                filter &= builder.Regex(t => t.Title, new BsonRegularExpression(Regex.Escape(term), "i"));
            }

            var documents = await collection.Find(filter)
                .SortByDescending(t => t.CreatedAt)
                .ThenByDescending(t => t.Id)
                .ToListAsync(cancellationToken);

            return documents.Select(ToResponse).ToList();
        });

    private static TodoResponse ToResponse(TodoReadModel document)
        => new(document.Id.ToString(), document.Title, document.Description, document.Status, document.CreatedAt, document.UpdatedAt);
}
