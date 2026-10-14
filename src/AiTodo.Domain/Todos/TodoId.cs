namespace AiTodo.Domain.Todos;

/// <summary>
/// Typed identity of a <see cref="Todo"/>: 24 hexadecimal characters (the textual form of a MongoDB
/// ObjectId, as required by the specs), always normalized to lower case. Generation is the
/// repository's job (<see cref="ITodoRepository.NextId"/>), so the Domain stays free of driver types.
/// </summary>
public readonly record struct TodoId
{
    public const int Length = 24;

    private TodoId(string value) => Value = value;

    public string Value { get; } = string.Empty;

    /// <summary>Parses the textual form. Anything that is not exactly 24 hexadecimal characters is rejected.</summary>
    public static bool TryParse(string? text, out TodoId id)
    {
        if (text is { Length: Length } && text.All(char.IsAsciiHexDigit))
        {
            id = new TodoId(text.ToLowerInvariant());
            return true;
        }

        id = default;
        return false;
    }

    public static TodoId Parse(string text)
        => TryParse(text, out var id)
            ? id
            : throw new DomainException("O identificador informado é inválido.", "id");

    public override string ToString() => Value;
}
