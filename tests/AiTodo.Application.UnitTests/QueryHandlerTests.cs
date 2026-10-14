using AiTodo.Application.Common;
using AiTodo.Application.Features.Todos;
using AiTodo.Domain;
using AiTodo.Domain.Todos;
using AiTodo.Application.Features.Todos.Commands.ChangeTodoStatus;
using AiTodo.Application.Features.Todos.Commands.CreateTodo;
using AiTodo.Application.Features.Todos.Queries.GetTodoById;
using AiTodo.Application.Features.Todos.Queries.GetTodos;
using AiTodo.Tests.Common;

namespace AiTodo.Application.UnitTests;

public class QueryHandlerTests
{
    private readonly InMemoryTodoRepository _repository = new();
    private readonly FakeClock _clock = new();

    private async Task<TodoResponse> SeedAsync(string title, TodoStatus status = TodoStatus.ToDo)
    {
        _clock.Advance(TimeSpan.FromMinutes(1)); // distinct creation times -> deterministic ordering
        var created = await new CreateTodoHandler(_repository, _clock).ExecuteAsync(new CreateTodoCommand(title, null), default);

        if (status != TodoStatus.ToDo)
        {
            await new ChangeTodoStatusHandler(_repository, _clock)
                .ExecuteAsync(new ChangeTodoStatusCommand(created.Id, status), default);
        }

        return created;
    }

    private Task<IReadOnlyList<TodoResponse>> ListAsync(TodoStatus? status = null, string? search = null)
        => new GetTodosHandler(_repository).ExecuteAsync(new GetTodosQuery(status, search), default);

    // ---- GetById ---------------------------------------------------------------------------

    [Fact]
    public async Task GetById_returns_the_todo()
    {
        var created = await SeedAsync("Estudar");

        var result = await new GetTodoByIdHandler(_repository).ExecuteAsync(new GetTodoByIdQuery(created.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(created, result.Value);
    }

    [Theory]
    [InlineData("000000000000000000000000")]
    [InlineData("abc")]
    public async Task GetById_of_a_missing_or_malformed_id_returns_NotFound(string id)
    {
        var result = await new GetTodoByIdHandler(_repository).ExecuteAsync(new GetTodoByIdQuery(id), default);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task GetById_does_not_modify_anything()
    {
        var created = await SeedAsync("Estudar");

        await new GetTodoByIdHandler(_repository).ExecuteAsync(new GetTodoByIdQuery(created.Id), default);

        Assert.Equal(0, _repository.UpdateCalls);
        Assert.Equal(1, _repository.Count);
    }

    // ---- GetList ---------------------------------------------------------------------------

    [Fact]
    public async Task List_without_filters_returns_everything_newest_first()
    {
        var a = await SeedAsync("a");
        var b = await SeedAsync("b");
        var c = await SeedAsync("c");

        var list = await ListAsync();

        Assert.Equal([c.Id, b.Id, a.Id], list.Select(t => t.Id));
    }

    [Fact]
    public async Task List_with_nothing_stored_returns_an_empty_list()
    {
        Assert.Empty(await ListAsync());
    }

    [Fact]
    public async Task List_filters_by_status()
    {
        await SeedAsync("a");
        var doing = await SeedAsync("b", TodoStatus.Doing);
        await SeedAsync("c", TodoStatus.Done);

        var list = await ListAsync(status: TodoStatus.Doing);

        Assert.Equal([doing.Id], list.Select(t => t.Id));
    }

    [Fact]
    public async Task List_searches_the_title_ignoring_case()
    {
        var match = await SeedAsync("Estudar CQRS");
        await SeedAsync("Comprar pão");

        var list = await ListAsync(search: "cqrs");

        Assert.Equal([match.Id], list.Select(t => t.Id));
    }

    [Fact]
    public async Task List_combines_status_and_search_with_AND()
    {
        await SeedAsync("estudar api", TodoStatus.Doing);
        var both = await SeedAsync("estudar api 2", TodoStatus.Done);
        await SeedAsync("comprar", TodoStatus.Done);

        var list = await ListAsync(TodoStatus.Done, "estudar");

        Assert.Equal([both.Id], list.Select(t => t.Id));
    }

    [Fact]
    public async Task List_with_no_match_returns_an_empty_list_not_an_error()
    {
        await SeedAsync("a");

        Assert.Empty(await ListAsync(search: "zzz"));
    }

    [Fact]
    public async Task List_is_a_read_only_operation()
    {
        await SeedAsync("a");
        var before = await ListAsync();

        await ListAsync(TodoStatus.Done, "x");

        Assert.Equal(before, await ListAsync());
        Assert.Equal(0, _repository.UpdateCalls);
    }

    [Fact]
    public async Task Queries_propagate_repository_failures()
    {
        _repository.FailWith = new TimeoutException();

        await Assert.ThrowsAsync<TimeoutException>(() => ListAsync());
        await Assert.ThrowsAsync<TimeoutException>(
            () => new GetTodoByIdHandler(_repository).ExecuteAsync(new GetTodoByIdQuery("0123456789abcdef01234567"), default));
    }
}
