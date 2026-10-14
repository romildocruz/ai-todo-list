using AiTodo.Application.Common;
using AiTodo.Application.Features.Todos;
using AiTodo.Domain;
using AiTodo.Domain.Todos;
using AiTodo.Application.Features.Todos.Commands.ChangeTodoStatus;
using AiTodo.Application.Features.Todos.Commands.CreateTodo;
using AiTodo.Application.Features.Todos.Commands.DeleteTodo;
using AiTodo.Application.Features.Todos.Commands.UpdateTodo;
using AiTodo.Tests.Common;

namespace AiTodo.Application.UnitTests;

public class CommandHandlerTests
{
    private static readonly string UnknownId = "0123456789abcdef01234567";

    private readonly InMemoryTodoRepository _repository = new();
    private readonly FakeClock _clock = new();

    private async Task<TodoResponse> SeedAsync(string title = "Estudar", string? description = "desc")
        => await new CreateTodoHandler(_repository, _clock).ExecuteAsync(new CreateTodoCommand(title, description), default);

    // ---- Create ----------------------------------------------------------------------------

    [Fact]
    public async Task Create_persists_a_ToDo_with_trimmed_title_and_clock_dates()
    {
        var created = await SeedAsync("  Estudar CQRS  ", "handlers");

        Assert.Equal("Estudar CQRS", created.Title);
        Assert.Equal("handlers", created.Description);
        Assert.Equal(TodoStatus.ToDo, created.Status);
        Assert.Equal(_clock.UtcNow, created.CreatedAt);
        Assert.Equal(created.CreatedAt, created.UpdatedAt);
        Assert.Equal(1, _repository.Count);
        Assert.Equal(created, _repository.Find(created.Id)!.ToResponse());
    }

    [Fact]
    public async Task Create_with_an_invalid_title_throws_and_stores_nothing()
    {
        await Assert.ThrowsAsync<DomainException>(() => SeedAsync("   "));

        Assert.Equal(0, _repository.Count);
    }

    [Fact]
    public async Task Create_propagates_repository_failures()
    {
        _repository.FailWith = new TimeoutException();

        await Assert.ThrowsAsync<TimeoutException>(() => SeedAsync());
    }

    // ---- Update ----------------------------------------------------------------------------

    [Fact]
    public async Task Update_persists_new_values_and_moves_UpdatedAt_only()
    {
        var created = await SeedAsync("antigo", "desc");
        _clock.Advance(TimeSpan.FromMinutes(10));
        var handler = new UpdateTodoHandler(_repository, _clock);

        var result = await handler.ExecuteAsync(new UpdateTodoCommand(created.Id, "novo", null), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("novo", result.Value!.Title);
        Assert.Null(result.Value.Description);
        Assert.Equal(_clock.UtcNow, result.Value.UpdatedAt);
        Assert.Equal(created.CreatedAt, result.Value.CreatedAt);
        Assert.Equal(result.Value, _repository.Find(created.Id)!.ToResponse());
    }

    [Fact]
    public async Task Update_with_the_same_values_succeeds_without_writing_or_touching_UpdatedAt()
    {
        var created = await SeedAsync("igual", "desc");
        _clock.Advance(TimeSpan.FromHours(1));
        var handler = new UpdateTodoHandler(_repository, _clock);

        var result = await handler.ExecuteAsync(new UpdateTodoCommand(created.Id, "igual", "desc"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(created.UpdatedAt, result.Value!.UpdatedAt);
        Assert.Equal(0, _repository.UpdateCalls);
    }

    [Theory]
    [InlineData("000000000000000000000000")]
    [InlineData("abc")]
    public async Task Update_of_a_missing_or_malformed_id_returns_NotFound_and_creates_nothing(string id)
    {
        var handler = new UpdateTodoHandler(_repository, _clock);

        var result = await handler.ExecuteAsync(new UpdateTodoCommand(id, "x", null), default);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Equal(0, _repository.Count);
    }

    [Fact]
    public async Task Update_with_an_invalid_title_throws_and_leaves_the_stored_todo_unchanged()
    {
        var created = await SeedAsync("original", "desc");
        var handler = new UpdateTodoHandler(_repository, _clock);

        await Assert.ThrowsAsync<DomainException>(
            () => handler.ExecuteAsync(new UpdateTodoCommand(created.Id, "", "nova"), default));

        Assert.Equal(created, _repository.Find(created.Id)!.ToResponse());
    }

    [Fact]
    public async Task Update_returns_NotFound_when_the_todo_disappears_between_read_and_write()
    {
        var created = await SeedAsync();
        _repository.UpdateMatchesNothing = true;
        var handler = new UpdateTodoHandler(_repository, _clock);

        var result = await handler.ExecuteAsync(new UpdateTodoCommand(created.Id, "outro", null), default);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    // ---- ChangeStatus ----------------------------------------------------------------------

    [Fact]
    public async Task ChangeStatus_persists_the_new_status_and_moves_UpdatedAt()
    {
        var created = await SeedAsync();
        _clock.Advance(TimeSpan.FromMinutes(5));
        var handler = new ChangeTodoStatusHandler(_repository, _clock);

        var result = await handler.ExecuteAsync(new ChangeTodoStatusCommand(created.Id, TodoStatus.Done), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(TodoStatus.Done, result.Value!.Status);
        Assert.Equal(_clock.UtcNow, result.Value.UpdatedAt);
        Assert.Equal(created.CreatedAt, result.Value.CreatedAt);
        Assert.Equal(result.Value, _repository.Find(created.Id)!.ToResponse());
    }

    [Fact]
    public async Task ChangeStatus_to_the_current_status_succeeds_without_writing_or_touching_UpdatedAt()
    {
        var created = await SeedAsync();
        _clock.Advance(TimeSpan.FromMinutes(5));
        var handler = new ChangeTodoStatusHandler(_repository, _clock);

        var result = await handler.ExecuteAsync(new ChangeTodoStatusCommand(created.Id, TodoStatus.ToDo), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(created.UpdatedAt, result.Value!.UpdatedAt);
        Assert.Equal(0, _repository.UpdateCalls);
    }

    [Fact]
    public async Task ChangeStatus_of_a_missing_todo_returns_NotFound()
    {
        var handler = new ChangeTodoStatusHandler(_repository, _clock);

        var result = await handler.ExecuteAsync(new ChangeTodoStatusCommand(UnknownId, TodoStatus.Done), default);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task ChangeStatus_with_an_undefined_status_throws_and_leaves_the_stored_todo_unchanged()
    {
        var created = await SeedAsync();
        var handler = new ChangeTodoStatusHandler(_repository, _clock);

        await Assert.ThrowsAsync<DomainException>(
            () => handler.ExecuteAsync(new ChangeTodoStatusCommand(created.Id, (TodoStatus)99), default));

        Assert.Equal(created, _repository.Find(created.Id)!.ToResponse());
    }

    [Fact]
    public async Task ChangeStatus_returns_NotFound_when_the_todo_disappears_between_read_and_write()
    {
        var created = await SeedAsync();
        _repository.UpdateMatchesNothing = true;
        var handler = new ChangeTodoStatusHandler(_repository, _clock);

        var result = await handler.ExecuteAsync(new ChangeTodoStatusCommand(created.Id, TodoStatus.Doing), default);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    // ---- Delete ----------------------------------------------------------------------------

    [Fact]
    public async Task Delete_removes_an_existing_todo_and_only_that_one()
    {
        var first = await SeedAsync("um");
        var second = await SeedAsync("dois");
        var handler = new DeleteTodoHandler(_repository);

        var result = await handler.ExecuteAsync(new DeleteTodoCommand(first.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Null(_repository.Find(first.Id));
        Assert.NotNull(_repository.Find(second.Id));
    }

    [Theory]
    [InlineData("000000000000000000000000")]
    [InlineData("xyz")]
    public async Task Delete_of_a_missing_or_malformed_id_returns_NotFound(string id)
    {
        await SeedAsync();
        var handler = new DeleteTodoHandler(_repository);

        var result = await handler.ExecuteAsync(new DeleteTodoCommand(id), default);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Equal(1, _repository.Count);
    }

    [Fact]
    public async Task Delete_twice_returns_NotFound_the_second_time()
    {
        var created = await SeedAsync();
        var handler = new DeleteTodoHandler(_repository);

        await handler.ExecuteAsync(new DeleteTodoCommand(created.Id), default);
        var second = await handler.ExecuteAsync(new DeleteTodoCommand(created.Id), default);

        Assert.Equal(ResultStatus.NotFound, second.Status);
    }
}
