namespace AiTodo.Domain.Todos;

/// <summary>
/// Todo aggregate root. All business rules live here (private constructor, static factory, private
/// setters). Callers supply the current UTC time (taken from a <see cref="TimeProvider"/>) so the
/// behavior is deterministic in tests.
/// </summary>
public sealed class Todo
{
    // Used by the persistence layer when it materializes stored documents.
    private Todo()
    {
    }

    public TodoId Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public TodoStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    /// <summary>Creates a new todo with status <see cref="TodoStatus.ToDo"/>.</summary>
    /// <exception cref="DomainException">The title or description violates the rules.</exception>
    public static Todo Create(TodoId id, string? title, string? description, DateTime utcNow)
    {
        var (normalizedTitle, normalizedDescription) = Normalize(title, description);
        var now = TodoRules.ToStorageTime(utcNow);

        return new Todo
        {
            Id = id,
            Title = normalizedTitle,
            Description = normalizedDescription,
            Status = TodoStatus.ToDo,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>Replaces title and description. Returns <c>false</c> (and keeps UpdatedAt) when nothing changes.</summary>
    /// <exception cref="DomainException">The title or description violates the rules.</exception>
    public bool Update(string? title, string? description, DateTime utcNow)
    {
        var (normalizedTitle, normalizedDescription) = Normalize(title, description);

        if (normalizedTitle == Title && normalizedDescription == Description)
        {
            return false;
        }

        Title = normalizedTitle;
        Description = normalizedDescription;
        UpdatedAt = TodoRules.ToStorageTime(utcNow);
        return true;
    }

    /// <summary>Changes the status (any transition is allowed). Returns <c>false</c> when it is already the same.</summary>
    /// <exception cref="DomainException">The status is not a defined value.</exception>
    public bool ChangeStatus(TodoStatus status, DateTime utcNow)
    {
        if (!Enum.IsDefined(status))
        {
            throw new DomainException("O status deve ser ToDo, Doing ou Done.", "status");
        }

        if (status == Status)
        {
            return false;
        }

        Status = status;
        UpdatedAt = TodoRules.ToStorageTime(utcNow);
        return true;
    }

    private static (string Title, string? Description) Normalize(string? title, string? description)
    {
        var normalizedTitle = TodoRules.NormalizeTitle(title);
        if (!TodoRules.IsValidTitle(normalizedTitle))
        {
            throw new DomainException(
                $"O título é obrigatório e deve ter no máximo {TodoRules.MaxTitleLength} caracteres.", "title");
        }

        var normalizedDescription = TodoRules.NormalizeDescription(description);
        if (!TodoRules.IsValidDescription(normalizedDescription))
        {
            throw new DomainException(
                $"A descrição deve ter no máximo {TodoRules.MaxDescriptionLength} caracteres.", "description");
        }

        return (normalizedTitle!, normalizedDescription);
    }
}
