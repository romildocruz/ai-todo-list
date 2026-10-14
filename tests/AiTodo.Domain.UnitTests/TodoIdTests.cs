using AiTodo.Domain.Todos;

namespace AiTodo.Domain.UnitTests;

public class TodoIdTests
{
    [Theory]
    [InlineData("6acf964fff4285ed0dc88831")]
    [InlineData("000000000000000000000000")]
    [InlineData("ffffffffffffffffffffffff")]
    public void Valid_identifiers_are_accepted(string text)
    {
        Assert.True(TodoId.TryParse(text, out var id));
        Assert.Equal(text, id.Value);
        Assert.Equal(text, id.ToString());
    }

    [Fact]
    public void Identifiers_are_normalized_to_lower_case_so_that_both_spellings_are_the_same_id()
    {
        Assert.True(TodoId.TryParse("6ACF964FFF4285ED0DC88831", out var upper));
        Assert.True(TodoId.TryParse("6acf964fff4285ed0dc88831", out var lower));

        Assert.Equal("6acf964fff4285ed0dc88831", upper.Value);
        Assert.Equal(lower, upper);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("6acf964fff4285ed0dc8883")]      // 23 characters
    [InlineData("6acf964fff4285ed0dc888311")]    // 25 characters
    [InlineData("6acf964fff4285ed0dc8883g")]     // not hexadecimal
    [InlineData(" 6acf964fff4285ed0dc88831")]    // no implicit trimming
    [InlineData("../etc/passwd")]
    [InlineData("{\"$ne\":null}")]
    public void Anything_else_is_rejected(string? text)
    {
        Assert.False(TodoId.TryParse(text, out var id));
        Assert.Equal(default, id);
    }

    [Fact]
    public void Parse_throws_a_domain_exception_naming_the_field_when_the_text_is_invalid()
    {
        var ex = Assert.Throws<DomainException>(() => TodoId.Parse("abc"));

        Assert.Equal("id", ex.Field);
    }
}
