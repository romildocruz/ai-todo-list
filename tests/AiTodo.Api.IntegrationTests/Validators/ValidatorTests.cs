using AiTodo.Application.Features.Todos;
using AiTodo.Domain.Todos;
using AiTodo.Api.Endpoints.Todos.ChangeTodoStatus;
using AiTodo.Api.Endpoints.Todos.CreateTodo;
using AiTodo.Api.Endpoints.Todos.DeleteTodo;
using AiTodo.Api.Endpoints.Todos.GetTodoById;
using AiTodo.Api.Endpoints.Todos.GetTodos;
using AiTodo.Api.Endpoints.Todos.UpdateTodo;
using FluentValidation.Results;

namespace AiTodo.Api.IntegrationTests.Validators;

public class ValidatorTests
{
    private const string ValidId = "6acf964fff4285ed0dc88831";

    private static string Chars(int count) => new('x', count);

    private static void AssertInvalid(ValidationResult result, string property, string message)
    {
        // FastEndpoints switches FluentValidation to camelCase property names process-wide once an app starts,
        // so the casing depends on which test class ran first: compare ignoring case.
        var error = Assert.Single(result.Errors, e => string.Equals(e.PropertyName, property, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(message, error.ErrorMessage);
    }

    // ---- Create / Update: title ------------------------------------------------------------

    public static TheoryData<string?> BlankTitles => new() { null, "", "   ", "\t \n" };

    [Theory]
    [MemberData(nameof(BlankTitles))]
    public void Create_rejects_a_missing_or_blank_title_with_one_clear_message(string? title)
    {
        var result = new CreateTodoValidator().Validate(new CreateTodoRequest { Title = title });

        AssertInvalid(result, "Title", "O título é obrigatório.");
    }

    [Fact]
    public void Create_rejects_a_title_with_101_characters()
    {
        var result = new CreateTodoValidator().Validate(new CreateTodoRequest { Title = Chars(101) });

        AssertInvalid(result, "Title", "O título deve ter no máximo 100 caracteres.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Create_accepts_titles_between_1_and_100_characters(int length)
    {
        Assert.True(new CreateTodoValidator().Validate(new CreateTodoRequest { Title = Chars(length) }).IsValid);
    }

    [Fact]
    public void Create_measures_the_title_after_trimming()
    {
        var request = new CreateTodoRequest { Title = "  " + Chars(100) + "  " };

        Assert.True(new CreateTodoValidator().Validate(request).IsValid);
    }

    // ---- Create / Update: description ------------------------------------------------------

    [Fact]
    public void Create_rejects_a_description_with_501_characters()
    {
        var request = new CreateTodoRequest { Title = "t", Description = Chars(501) };

        var result = new CreateTodoValidator().Validate(request);

        AssertInvalid(result, "Description", "A descrição deve ter no máximo 500 caracteres.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_accepts_a_missing_or_blank_description(string? description)
    {
        var request = new CreateTodoRequest { Title = "t", Description = description };

        Assert.True(new CreateTodoValidator().Validate(request).IsValid);
    }

    [Fact]
    public void Create_accepts_a_description_of_exactly_500_characters()
    {
        var request = new CreateTodoRequest { Title = "t", Description = Chars(500) };

        Assert.True(new CreateTodoValidator().Validate(request).IsValid);
    }

    [Fact]
    public void Create_reports_title_and_description_errors_together()
    {
        var request = new CreateTodoRequest { Title = "", Description = Chars(501) };

        var result = new CreateTodoValidator().Validate(request);

        Assert.Contains(result.Errors, e => string.Equals(e.PropertyName, "Title", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, e => string.Equals(e.PropertyName, "Description", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Update_applies_the_same_title_and_description_rules()
    {
        var validator = new UpdateTodoValidator();

        Assert.True(validator.Validate(new UpdateTodoRequest { Id = ValidId, Title = "ok" }).IsValid);
        AssertInvalid(validator.Validate(new UpdateTodoRequest { Id = ValidId, Title = " " }), "Title", "O título é obrigatório.");
        AssertInvalid(
            validator.Validate(new UpdateTodoRequest { Id = ValidId, Title = Chars(101) }),
            "Title", "O título deve ter no máximo 100 caracteres.");
        AssertInvalid(
            validator.Validate(new UpdateTodoRequest { Id = ValidId, Title = "ok", Description = Chars(501) }),
            "Description", "A descrição deve ter no máximo 500 caracteres.");
    }

    // ---- Id --------------------------------------------------------------------------------

    public static TheoryData<string?> InvalidIds => new()
    {
        null,
        "",
        "   ",
        "abc",
        "6acf964fff4285ed0dc8883",       // 23 chars
        "6acf964fff4285ed0dc888311",     // 25 chars
        "6acf964fff4285ed0dc8883g",      // not hexadecimal
        "../etc/passwd",
        "{\"$ne\":null}"                  // injection-looking input
    };

    [Theory]
    [MemberData(nameof(InvalidIds))]
    public void Every_endpoint_with_an_id_rejects_a_malformed_id(string? id)
    {
        const string message = "O identificador informado é inválido.";

        AssertInvalid(new GetTodoByIdValidator().Validate(new GetTodoByIdRequest { Id = id }), "Id", message);
        AssertInvalid(new DeleteTodoValidator().Validate(new DeleteTodoRequest { Id = id }), "Id", message);
        AssertInvalid(new UpdateTodoValidator().Validate(new UpdateTodoRequest { Id = id, Title = "ok" }), "Id", message);
        AssertInvalid(
            new ChangeTodoStatusValidator().Validate(new ChangeTodoStatusRequest { Id = id, Status = TodoStatus.Done }),
            "Id", message);
    }

    [Theory]
    [InlineData("6acf964fff4285ed0dc88831")]
    [InlineData("000000000000000000000000")]
    [InlineData("6ACF964FFF4285ED0DC88831")]
    public void Well_formed_ids_are_accepted(string id)
    {
        Assert.True(new GetTodoByIdValidator().Validate(new GetTodoByIdRequest { Id = id }).IsValid);
        Assert.True(new DeleteTodoValidator().Validate(new DeleteTodoRequest { Id = id }).IsValid);
    }

    // ---- Status (list filter) --------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ToDo")]
    [InlineData("doing")]
    [InlineData("DONE")]
    public void List_accepts_no_filter_or_a_valid_status_name(string? status)
    {
        Assert.True(new GetTodosValidator().Validate(new GetTodosRequest { Status = status }).IsValid);
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Blocked")]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("3")]
    [InlineData("-1")]
    [InlineData("ToDo,Doing")]
    public void List_rejects_an_invalid_status_filter(string status)
    {
        var result = new GetTodosValidator().Validate(new GetTodosRequest { Status = status });

        AssertInvalid(result, "Status", "O status deve ser ToDo, Doing ou Done.");
    }

    [Fact]
    public void List_does_not_restrict_the_search_text()
    {
        var request = new GetTodosRequest { Search = "a.*[(" };

        Assert.True(new GetTodosValidator().Validate(request).IsValid);
    }

    // ---- Status (PATCH body) ---------------------------------------------------------------

    [Theory]
    [InlineData(TodoStatus.ToDo)]
    [InlineData(TodoStatus.Doing)]
    [InlineData(TodoStatus.Done)]
    public void ChangeStatus_accepts_the_three_valid_statuses(TodoStatus status)
    {
        var result = new ChangeTodoStatusValidator().Validate(
            new ChangeTodoStatusRequest { Id = ValidId, Status = status });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ChangeStatus_rejects_a_missing_status_instead_of_defaulting_to_ToDo()
    {
        var result = new ChangeTodoStatusValidator().Validate(
            new ChangeTodoStatusRequest { Id = ValidId, Status = null });

        AssertInvalid(result, "Status", "O status deve ser ToDo, Doing ou Done.");
    }

    [Fact]
    public void ChangeStatus_rejects_a_value_outside_the_enum()
    {
        var result = new ChangeTodoStatusValidator().Validate(
            new ChangeTodoStatusRequest { Id = ValidId, Status = (TodoStatus)7 });

        AssertInvalid(result, "Status", "O status deve ser ToDo, Doing ou Done.");
    }
}
