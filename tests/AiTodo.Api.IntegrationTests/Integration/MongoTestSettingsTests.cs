namespace AiTodo.Api.IntegrationTests.Integration;

/// <summary>The guard that keeps integration tests away from the development database.</summary>
public class MongoTestSettingsTests
{
    [Theory]
    [InlineData("AiTodoDb")]
    [InlineData("aitodoDB")]
    [InlineData("admin")]
    [InlineData("local")]
    [InlineData("Todos")]
    [InlineData("")]
    [InlineData("aitodoDb_Tests_abc")]      // prefix is case-sensitive
    [InlineData("Prod_AiTodoDb_Tests_abc")] // must START with the prefix
    public void The_guard_refuses_any_database_that_is_not_a_throwaway_test_one(string databaseName)
    {
        Assert.Throws<InvalidOperationException>(() => MongoTestSettings.EnsureIsTestDatabase(databaseName));
    }

    [Fact]
    public void Generated_database_names_are_unique_prefixed_and_accepted_by_the_guard()
    {
        var names = Enumerable.Range(0, 50).Select(_ => MongoTestSettings.NewDatabaseName()).ToList();

        Assert.Equal(names.Count, names.Distinct().Count());
        Assert.All(names, name =>
        {
            Assert.StartsWith("AiTodoDb_Tests_", name);
            MongoTestSettings.EnsureIsTestDatabase(name);
            Assert.True(name.Length <= 63, "MongoDB database names must stay below 64 characters");
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_missing_or_blank_value_counts_as_not_configured(string? value)
    {
        Assert.Null(MongoTestSettings.Normalize(value));
    }

    [Fact]
    public void A_connection_string_is_trimmed()
    {
        Assert.Equal("mongodb://localhost:27017", MongoTestSettings.Normalize("  mongodb://localhost:27017 "));
    }
}
