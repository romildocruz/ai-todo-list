using AiTodo.Domain.Todos;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AiTodo.Infrastructure.Mongo;

/// <summary>Write-side repository of the Todo aggregate. Each operation is its own commit (no unit of work).</summary>
internal sealed class TodoRepository(IMongoCollection<Todo> collection) : ITodoRepository
{
    public TodoId NextId() => TodoId.Parse(ObjectId.GenerateNewId().ToString());

    public Task AddAsync(Todo todo, CancellationToken cancellationToken)
        => MongoCall.RunAsync(() => collection.InsertOneAsync(todo, cancellationToken: cancellationToken));

    public Task<Todo?> GetByIdAsync(TodoId id, CancellationToken cancellationToken)
        => MongoCall.RunAsync<Todo?>(() => collection.Find(t => t.Id == id).FirstOrDefaultAsync(cancellationToken)!);

    public Task<bool> UpdateAsync(Todo todo, CancellationToken cancellationToken)
        => MongoCall.RunAsync(async () =>
        {
            var result = await collection.ReplaceOneAsync(t => t.Id == todo.Id, todo, cancellationToken: cancellationToken);
            return result.MatchedCount > 0;
        });

    public Task<bool> DeleteAsync(TodoId id, CancellationToken cancellationToken)
        => MongoCall.RunAsync(async () =>
        {
            var result = await collection.DeleteOneAsync(t => t.Id == id, cancellationToken);
            return result.DeletedCount > 0;
        });
}
