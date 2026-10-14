namespace AiTodo.Domain.Todos;

/// <summary>
/// Single source of truth for the todo business rules (limits and normalization),
/// shared by the aggregate and the request validators.
/// </summary>
public static class TodoRules
{
    public const int MaxTitleLength = 100;
    public const int MaxDescriptionLength = 500;

    public static string? NormalizeTitle(string? title) => title?.Trim();

    /// <summary>A description that is blank after trimming is treated as absent.</summary>
    public static string? NormalizeDescription(string? description)
    {
        var trimmed = description?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    /// <summary>
    /// Strict status parsing for text inputs: only the exact names ToDo/Doing/Done (case-insensitive).
    /// Numbers and flag-style lists such as "ToDo,Doing", which <see cref="Enum.TryParse{TEnum}(string?, out TEnum)"/> accepts, are rejected.
    /// </summary>
    public static bool TryParseStatus(string? value, out TodoStatus status)
    {
        var candidate = value?.Trim();
        foreach (var name in Enum.GetNames<TodoStatus>())
        {
            if (string.Equals(name, candidate, StringComparison.OrdinalIgnoreCase))
            {
                status = Enum.Parse<TodoStatus>(name);
                return true;
            }
        }

        status = default;
        return false;
    }

    public static bool IsValidTitle(string? normalizedTitle)
        => !string.IsNullOrEmpty(normalizedTitle) && normalizedTitle.Length <= MaxTitleLength;

    public static bool IsValidDescription(string? normalizedDescription)
        => normalizedDescription is null || normalizedDescription.Length <= MaxDescriptionLength;

    /// <summary>
    /// MongoDB stores dates with millisecond precision: truncating here keeps the in-memory
    /// entity identical to what is read back from the database.
    /// </summary>
    internal static DateTime ToStorageTime(DateTime utc)
    {
        if (utc.Kind != DateTimeKind.Utc)
        {
            throw new DomainException("A data deve estar em UTC.");
        }

        return new DateTime(utc.Ticks - utc.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
    }
}
