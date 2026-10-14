namespace AiTodo.Api.IntegrationTests.Integration;

/// <summary>
/// Settings for the integration tests that need a real MongoDB. They only run when
/// <c>MongoTests__ConnectionString</c> is defined, always use a throwaway database named
/// <c>AiTodoDb_Tests_&lt;guid&gt;</c>, and can never touch the development database <c>AiTodoDb</c>.
/// </summary>
public static class MongoTestSettings
{
    public const string EnvironmentVariable = "MongoTests__ConnectionString";
    public const string DatabasePrefix = "AiTodoDb_Tests_";
    public const string DevelopmentDatabaseName = "AiTodoDb";

    public const string SkipReason =
        $"Instância MongoDB de testes não configurada. Defina a variável de ambiente {EnvironmentVariable} " +
        "(ex.: mongodb://localhost:27017) para executar os testes de integração. " +
        $"Eles usam apenas bancos temporários '{DatabasePrefix}<guid>' e nunca o banco '{DevelopmentDatabaseName}'.";

    public static string? ConnectionString => Normalize(Environment.GetEnvironmentVariable(EnvironmentVariable));

    /// <summary>Blank or whitespace-only values count as "not configured".</summary>
    public static string? Normalize(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    public static bool IsConfigured => ConnectionString is not null;

    public static string NewDatabaseName()
    {
        var name = DatabasePrefix + Guid.NewGuid().ToString("N");
        EnsureIsTestDatabase(name);
        return name;
    }

    /// <summary>Last line of defense: refuses to work with any database that is not a throwaway test one.</summary>
    public static void EnsureIsTestDatabase(string databaseName)
    {
        if (!databaseName.StartsWith(DatabasePrefix, StringComparison.Ordinal)
            || string.Equals(databaseName, DevelopmentDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"'{databaseName}' não é um banco de testes (esperado prefixo '{DatabasePrefix}'). Operação abortada.");
        }
    }
}

/// <summary>[Fact] that is explicitly skipped, with an explanatory message, when no test MongoDB is configured.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class MongoFactAttribute : FactAttribute
{
    public MongoFactAttribute()
    {
        if (!MongoTestSettings.IsConfigured)
        {
            Skip = MongoTestSettings.SkipReason;
        }
    }
}

/// <summary>[Theory] that is explicitly skipped, with an explanatory message, when no test MongoDB is configured.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class MongoTheoryAttribute : TheoryAttribute
{
    public MongoTheoryAttribute()
    {
        if (!MongoTestSettings.IsConfigured)
        {
            Skip = MongoTestSettings.SkipReason;
        }
    }
}
