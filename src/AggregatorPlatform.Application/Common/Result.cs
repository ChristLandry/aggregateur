namespace AggregatorPlatform.Application.Common;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    protected Result(bool isSuccess, string? errorCode, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public static Result Success() => new(true, null, null);
    public static Result Failure(string errorCode, string errorMessage) => new(false, errorCode, errorMessage);

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(string errorCode, string errorMessage) => Result<T>.Failure(errorCode, errorMessage);
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(T? value, bool isSuccess, string? errorCode, string? errorMessage)
        : base(isSuccess, errorCode, errorMessage)
    {
        Value = value;
    }

    public static new Result<T> Success(T value) => new(value, true, null, null);
    public static new Result<T> Failure(string errorCode, string errorMessage) => new(default, false, errorCode, errorMessage);
}
