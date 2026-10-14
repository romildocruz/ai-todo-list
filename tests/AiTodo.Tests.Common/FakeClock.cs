namespace AiTodo.Tests.Common;

/// <summary>Controllable clock: tests decide what "now" is and when it moves.</summary>
public sealed class FakeClock(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _now = start;

    public FakeClock() : this(new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero))
    {
    }

    public DateTime UtcNow => _now.UtcDateTime;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}
