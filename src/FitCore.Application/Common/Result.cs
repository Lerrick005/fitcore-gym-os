namespace FitCore.Application.Common;

/// <summary>Non-generic result for operations without a return value (PRS §4.2).</summary>
public sealed class Result
{
    public bool IsSuccess { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    private Result(bool success, string? code, string? message)
    {
        IsSuccess = success;
        ErrorCode = code;
        ErrorMessage = message;
    }

    public static Result Ok() => new(true, null, null);
    public static Result Fail(string code, string message) => new(false, code, message);
}

/// <summary>Result with a value (PRS §4.2).</summary>
public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    private Result(bool success, T? value, string? code, string? message)
    {
        IsSuccess = success;
        Value = value;
        ErrorCode = code;
        ErrorMessage = message;
    }

    public static Result<T> Ok(T value) => new(true, value, null, null);
    public static Result<T> Fail(string code, string message) => new(false, default, code, message);
}
