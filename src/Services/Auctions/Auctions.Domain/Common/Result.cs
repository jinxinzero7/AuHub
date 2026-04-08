namespace Auctions.Domain.Common;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }
    public int StatusCode { get; }

    protected Result(bool isSuccess, string error, int statusCode)
    {
        IsSuccess = isSuccess;
        Error = error;
        StatusCode = statusCode;
    }

    public static Result Success() => new(true, string.Empty, 200);
    public static Result Failure(string error, int statusCode = 400) => new(false, error, statusCode);
    
    public static Result<T> Success<T>(T value) => new(value, true, string.Empty, 200);
    public static Result<T> Failure<T>(string error, int statusCode = 400) => new(default!, false, error, statusCode);
}

public class Result<T> : Result
{
    public T Value { get; }

    public Result(T value, bool isSuccess, string error, int statusCode) 
        : base(isSuccess, error, statusCode)
    {
        Value = value;
    }
}
