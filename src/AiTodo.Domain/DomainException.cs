namespace AiTodo.Domain;

/// <summary>
/// A business invariant was violated. Thrown by aggregates when they are asked to enter an invalid
/// state; the API validates requests first, so reaching this means a validation gap or a direct caller.
/// </summary>
public sealed class DomainException(string message, string? field = null) : Exception(message)
{
    /// <summary>The offending field in camelCase (e.g. "title"), when the rule concerns a single field.</summary>
    public string? Field { get; } = field;
}
