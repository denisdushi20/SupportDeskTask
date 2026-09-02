namespace SupportDesk.Api.Application.Common;

public class AppResult
{
    protected AppResult(bool isSuccess, AppError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public AppError? Error { get; }

    public static AppResult Success() => new(true, null);

    public static AppResult Failure(AppError error) => new(false, error);

    public static AppResult Failure(
        string code,
        string message,
        IReadOnlyDictionary<string, object?>? context = null) =>
        Failure(AppError.Create(code, message, context));
}

public sealed class AppResult<T> : AppResult
{
    private AppResult(bool isSuccess, T? value, AppError? error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    public T? Value { get; }

    public static AppResult<T> Success(T value) => new(true, value, null);

    public new static AppResult<T> Failure(AppError error) => new(false, default, error);

    public new static AppResult<T> Failure(
        string code,
        string message,
        IReadOnlyDictionary<string, object?>? context = null) =>
        Failure(AppError.Create(code, message, context));

    public static AppResult<T> FromError(AppResult failed) =>
        Failure(failed.Error ?? AppError.Create(AppErrorCodes.Conflict, "Operation failed."));
}
