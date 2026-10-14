using AiTodo.Domain.Todos;

namespace AiTodo.Domain.UnitTests;

public class TodoRulesTests
{
    [Theory]
    [InlineData("ToDo", TodoStatus.ToDo)]
    [InlineData("Doing", TodoStatus.Doing)]
    [InlineData("Done", TodoStatus.Done)]
    [InlineData("todo", TodoStatus.ToDo)]
    [InlineData("DOING", TodoStatus.Doing)]
    [InlineData("  done ", TodoStatus.Done)]
    public void TryParseStatus_accepts_the_three_names_ignoring_case_and_outer_spaces(string text, TodoStatus expected)
    {
        Assert.True(TodoRules.TryParseStatus(text, out var status));
        Assert.Equal(expected, status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("Pending")]
    [InlineData("Blocked")]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("-1")]
    [InlineData("ToDo,Doing")]
    [InlineData("ToDo, Done")]
    [InlineData("Do ne")]
    public void TryParseStatus_rejects_everything_else_including_numbers_and_flag_style_lists(string? text)
    {
        Assert.False(TodoRules.TryParseStatus(text, out _));
    }

    [Fact]
    public void NormalizeDescription_returns_null_for_blank_and_trimmed_text_otherwise()
    {
        Assert.Null(TodoRules.NormalizeDescription(null));
        Assert.Null(TodoRules.NormalizeDescription("   "));
        Assert.Equal("a", TodoRules.NormalizeDescription(" a "));
    }
}
