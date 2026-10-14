namespace AiTodo.Application.Common;

/// <summary>Expected outcomes of a handler. Unexpected failures are exceptions, not results.</summary>
public enum ResultStatus
{
    Success,
    NotFound
}

public readonly record struct Result(ResultStatus Status)
{
    public bool IsSuccess => Status == ResultStatus.Success;

    public static Result Success() => new(ResultStatus.Success);

    public static Result NotFound() => new(ResultStatus.NotFound);
}

public readonly record struct Result<T>(ResultStatus Status, T? Value)
{
    public bool IsSuccess => Status == ResultStatus.Success;

    public static Result<T> Success(T value) => new(ResultStatus.Success, value);

    public static Result<T> NotFound() => new(ResultStatus.NotFound, default);
}
