using AiTodo.Domain;
using AiTodo.Domain.Todos;

namespace AiTodo.Domain.UnitTests;

/// <summary>
/// Business rules 1-6 and 9 (specs: todo-management).
/// </summary>
public class TodoTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    private static Todo NewTodo(string? title, string? description, DateTime utcNow)
        => Todo.Create(TodoId.Parse(Guid.NewGuid().ToString("N")[..24]), title, description, utcNow);

    // Rule 1: title is required and cannot be only spaces
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n ")]
    public void Create_rejects_missing_or_blank_title(string? title)
    {
        var ex = Assert.Throws<DomainException>(() => NewTodo(title, null, T0));

        Assert.Equal("title", ex.Field);
    }

    [Fact]
    public void Create_trims_the_title()
    {
        var todo = NewTodo("  Comprar pão  ", null, T0);

        Assert.Equal("Comprar pão", todo.Title);
    }

    [Fact]
    public void Create_accepts_a_title_of_exactly_100_characters_and_rejects_101()
    {
        Assert.Equal(100, NewTodo(new string('x', 100), null, T0).Title.Length);
        Assert.Throws<DomainException>(() => NewTodo(new string('x', 101), null, T0));
    }

    [Fact]
    public void Title_limit_is_applied_after_trimming()
    {
        var todo = NewTodo("  " + new string('x', 100) + "  ", null, T0);

        Assert.Equal(100, todo.Title.Length);
    }

    // Rule 2: description is optional
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void Create_treats_a_missing_or_blank_description_as_absent(string? description)
    {
        var todo = NewTodo("t", description, T0);

        Assert.Null(todo.Description);
    }

    [Fact]
    public void Create_keeps_and_trims_the_description()
    {
        var todo = NewTodo("t", "  detalhes  ", T0);

        Assert.Equal("detalhes", todo.Description);
    }

    [Fact]
    public void Create_accepts_a_description_of_exactly_500_characters_and_rejects_501()
    {
        Assert.Equal(500, NewTodo("t", new string('x', 500), T0).Description!.Length);

        var ex = Assert.Throws<DomainException>(() => NewTodo("t", new string('x', 501), T0));
        Assert.Equal("description", ex.Field);
    }

    // Rule 4: new todos start as ToDo
    [Fact]
    public void Create_starts_as_ToDo_keeping_the_given_identity()
    {
        var id = TodoId.Parse("6acf964fff4285ed0dc88831");

        var todo = Todo.Create(id, "t", null, T0);

        Assert.Equal(TodoStatus.ToDo, todo.Status);
        Assert.Equal(id, todo.Id);
    }

    [Fact]
    public void Create_sets_both_dates_to_the_given_time_truncated_to_milliseconds()
    {
        var todo = NewTodo("t", null, T0.AddTicks(1234567));

        Assert.Equal(todo.CreatedAt, todo.UpdatedAt);
        Assert.Equal(T0.AddTicks(1230000), todo.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, todo.CreatedAt.Kind);
    }

    [Fact]
    public void Dates_must_be_utc()
    {
        Assert.Throws<DomainException>(() => NewTodo("t", null, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local)));
        Assert.Throws<DomainException>(() => NewTodo("t", null, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)));
    }

    // Rule 5 + 6: CreatedAt is immutable, UpdatedAt changes only on a real change
    [Fact]
    public void Update_with_new_values_changes_them_moves_UpdatedAt_and_keeps_CreatedAt_and_Status()
    {
        var todo = NewTodo("antigo", "desc", T0);
        todo.ChangeStatus(TodoStatus.Doing, T0.AddMinutes(1));
        var later = T0.AddMinutes(10);

        var changed = todo.Update("novo", "outra", later);

        Assert.True(changed);
        Assert.Equal("novo", todo.Title);
        Assert.Equal("outra", todo.Description);
        Assert.Equal(later, todo.UpdatedAt);
        Assert.Equal(T0, todo.CreatedAt);
        Assert.Equal(TodoStatus.Doing, todo.Status);
    }

    [Fact]
    public void Update_with_the_same_values_reports_no_change_and_keeps_UpdatedAt()
    {
        var todo = NewTodo("igual", "desc", T0);

        var changed = todo.Update("  igual ", " desc ", T0.AddHours(1));

        Assert.False(changed);
        Assert.Equal(T0, todo.UpdatedAt);
    }

    [Fact]
    public void Update_without_description_clears_it()
    {
        var todo = NewTodo("t", "desc", T0);

        Assert.True(todo.Update("t", null, T0.AddMinutes(1)));
        Assert.Null(todo.Description);
    }

    // Rule 9: invalid operations do not modify data
    [Fact]
    public void Update_with_invalid_values_throws_and_leaves_the_todo_untouched()
    {
        var todo = NewTodo("original", "desc", T0);

        Assert.Throws<DomainException>(() => todo.Update("   ", "nova", T0.AddMinutes(1)));
        Assert.Throws<DomainException>(() => todo.Update("novo", new string('x', 501), T0.AddMinutes(1)));

        Assert.Equal("original", todo.Title);
        Assert.Equal("desc", todo.Description);
        Assert.Equal(T0, todo.UpdatedAt);
    }

    [Theory]
    [InlineData(TodoStatus.ToDo, TodoStatus.Doing)]
    [InlineData(TodoStatus.ToDo, TodoStatus.Done)]
    [InlineData(TodoStatus.Doing, TodoStatus.Done)]
    [InlineData(TodoStatus.Doing, TodoStatus.ToDo)]
    [InlineData(TodoStatus.Done, TodoStatus.ToDo)]
    [InlineData(TodoStatus.Done, TodoStatus.Doing)]
    public void ChangeStatus_allows_any_transition_and_moves_UpdatedAt(TodoStatus from, TodoStatus to)
    {
        var todo = NewTodo("t", null, T0);
        todo.ChangeStatus(from, T0.AddMinutes(1));
        var later = T0.AddMinutes(30);

        var changed = todo.ChangeStatus(to, later);

        Assert.True(changed);
        Assert.Equal(to, todo.Status);
        Assert.Equal(later, todo.UpdatedAt);
        Assert.Equal(T0, todo.CreatedAt);
    }

    [Fact]
    public void ChangeStatus_to_the_same_status_reports_no_change_and_keeps_UpdatedAt()
    {
        var todo = NewTodo("t", null, T0);

        var changed = todo.ChangeStatus(TodoStatus.ToDo, T0.AddHours(1));

        Assert.False(changed);
        Assert.Equal(T0, todo.UpdatedAt);
    }

    [Fact]
    public void ChangeStatus_rejects_a_value_outside_the_enum_and_leaves_the_todo_untouched()
    {
        var todo = NewTodo("t", null, T0);

        Assert.Throws<DomainException>(() => todo.ChangeStatus((TodoStatus)42, T0.AddMinutes(1)));

        Assert.Equal(TodoStatus.ToDo, todo.Status);
        Assert.Equal(T0, todo.UpdatedAt);
    }

    [Fact]
    public void Only_ToDo_Doing_and_Done_exist()
    {
        Assert.Equal(["ToDo", "Doing", "Done"], Enum.GetNames<TodoStatus>());
    }
}
